namespace NativeInvoke.Generator;

[Generator]
public sealed class NativeImportGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Incremental source generator is awesome
    var properties = context.SyntaxProvider
      .CreateSyntaxProvider(
        static (node, _) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
        static (ctx, _) => (PropertyDeclarationSyntax)ctx.Node)
      .Where(static p => p.AttributeLists.Count > 0);

    var compilationAndProps = context.CompilationProvider.Combine(properties.Collect());

    context.RegisterSourceOutput(compilationAndProps, static (spc, source) =>
    {
      var (compilation, propDecls) = source;
      if (propDecls.IsDefaultOrEmpty) return;

      var nativeImportAttr = compilation.GetTypeByMetadataName(typeof(NativeInvoke.NativeImportAttribute).FullName!);
      var nativeImportMethodAttr = compilation.GetTypeByMetadataName(typeof(NativeInvoke.NativeImportMethodAttribute).FullName!);
      if (nativeImportAttr is null) return;

      foreach (var propDecl in propDecls)
      {
        var model = compilation.GetSemanticModel(propDecl.SyntaxTree);
        if (model.GetDeclaredSymbol(propDecl) is not { } propSymbol) continue;

        // Properly identify, ensure it is indeed our specific attribute (not an alias with the same name)
        var attr = propSymbol.GetAttributes()
          .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, nativeImportAttr));
        if (attr is null) continue;

        GenerateForProperty(spc, compilation, propSymbol, attr, nativeImportMethodAttr);
      }
    });
  }

  private static void GenerateForProperty(
    SourceProductionContext spc,
    Compilation compilation,
    IPropertySymbol prop,
    AttributeData attr,
    INamedTypeSymbol? methodAttrSymbol)
  {
    var containingType = prop.ContainingType;

    // Containing type must be partial
    if (/*!containingType.IsStatic ||*/ !IsPartial(containingType))
    {
      spc.ReportDiagnostic(Diagnostic.Create(
        Diagnostics.TypeMustBePartial,
        prop.Locations[0],
        containingType.Name));
      return;
    }

    // Property must be static partial definition
    if (!prop.IsStatic || !prop.IsPartialDefinition)
    {
      spc.ReportDiagnostic(Diagnostic.Create(
        Diagnostics.PropertyMustBeStaticPartial,
        prop.Locations[0],
        prop.Name));
      return;
    }

    // Property type must be interface
    if (prop.Type.TypeKind != TypeKind.Interface)
    {
      spc.ReportDiagnostic(Diagnostic.Create(
        Diagnostics.PropertyTypeMustBeInterface,
        prop.Locations[0],
        prop.Name));
      return;
    }

    var iface = (INamedTypeSymbol)prop.Type;

    // Attribute data
    var libraryName = (string?)attr.ConstructorArguments[0].Value ?? "__Internal"; // TODO/FIXME: report a diagnostic
    var defaultCc = (CallingConvention)(attr.NamedArguments // TODO: switch to typeof(CallConv*)
      .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.CallingConvention))
      .Value.Value ?? (int)CallingConvention.Winapi); // Fallback to platform-default
    var lazy = (bool)(attr.NamedArguments
      .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.Lazy))
      .Value.Value ?? false);
    var symbolPrefix = (string?)(attr.NamedArguments
      .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.SymbolPrefix))
      .Value.Value ?? null); // TODO

    // Methods
    var methods = iface.GetMembers()
      .OfType<IMethodSymbol>()
      .Where(m =>
      {
        if (m.MethodKind != MethodKind.Ordinary) return false;
        // Properly identify, ensure it is indeed our specific attribute (not an alias with the same name)
        var mAttr = m.GetAttributes()
          .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodAttrSymbol));
        if (mAttr is null) return true; // include if attribute is not present
        if (mAttr.ConstructorArguments.Length <= 0) return true; // include if attribute is present, but no arguments
        if (mAttr.ConstructorArguments[0].Value is not string methodName) return true; // include if attribute is present, but 1st arg is not a string
        return !string.IsNullOrWhiteSpace(methodName); // exclude only if the string is null or empty string
      })
      .ToArray();

    // Blittability check
    foreach (var m in methods)
    {
      if (!IsBlittable(m.ReturnType) || m.Parameters.Any(static p => !IsBlittable(p.Type)))
      {
        spc.ReportDiagnostic(Diagnostic.Create(
          Diagnostics.NonBlittableSignature,
          m.Locations[0],
          $"{iface.Name}.{m.Name}"));
        return;
      }
    }

    var source = GenerateSource(containingType, prop, iface, methods, libraryName, defaultCc, lazy, methodAttrSymbol);
    spc.AddSource($"{containingType.Name}.{prop.Name}-{Guid.NewGuid():N}.g.cs", source); // Append Guid to avoid collisions, just in case
  }

  private static string GenerateSource(
    INamedTypeSymbol type,
    IPropertySymbol prop,
    INamedTypeSymbol iface,
    IMethodSymbol[] methods,
    string libraryName,
    CallingConvention defaultCc,
    bool lazy,
    INamedTypeSymbol? methodAttrSymbol)
  {
    var sb = new StringBuilder(); // TODO: switch to IndentedTextWriter (to fix nested indentation in Emit*)

    sb.AppendLine("// <auto-generated />");
    sb.AppendLine();

    var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
    if (ns is not null)
    {
      sb.Append("namespace ").Append(ns).AppendLine(";"); // C# 10 (file-scoped namespace)
      sb.AppendLine();
    }

    // 1. Collect the hierarchy (from target type up to its outermost parent)
    var typeStack = new Stack<INamedTypeSymbol>();
    var current = type;
    while (current is not null)
    {
      typeStack.Push(current);
      current = current.ContainingType;
    }

    var indentLevel = 0;
    string Indent() => new(' ', indentLevel * 4);

    // 2. Open nested type wrappers
    while (typeStack.Count > 0)
    {
      var t = typeStack.Pop();
      var acc = GetAccessibilityString(t);
      var kind = GetTypeKind(t);
      // NOTE: Using MinimallyQualifiedFormat captures generic parameters correctly
      var name = t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
      sb.Append(Indent()).AppendLine($"{acc}partial {kind} {name}");
      sb.Append(Indent()).AppendLine("{");
      indentLevel++;
    }

    // 3. The property
    var propAcc = GetAccessibilityString(prop);
    sb.Append(Indent()).AppendLine($"{propAcc}static partial {iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {prop.Name} => field ??= new __Impl();"); // C# 14 (compiler synthesized backing field)
    sb.AppendLine();

    // 4. Generate inner implementation
    sb.Append(Indent()).AppendLine($"private sealed unsafe class __Impl : {iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
    sb.Append(Indent()).AppendLine("{");

    var innerIndent = Indent() + "    ";
    sb.AppendLine($"{innerIndent}private static readonly nint __lib;"); // C# 9 (enhanced IntPtr)
    sb.AppendLine($"{innerIndent}static __Impl() {{ if (!global::System.Runtime.InteropServices.NativeLibrary.TryLoad({libraryName.Literal}, out __lib)) {{ throw new global::System.DllNotFoundException({libraryName.Literal}); }} }}");

    // NOTE: EmitLazy/Eager logic needs to handle the increased indentation to look clean
    // TODO/FIXME: Append Guid to support overloaded signatures (prevent m.Name collisions)
    if (lazy) EmitLazy(sb, methods, methodAttrSymbol, defaultCc);
    else EmitEager(sb, methods, methodAttrSymbol, defaultCc);

    foreach (var m in methods)
    {
      var ret = m.ReturnType.ToDisplayString();
      var paramsList = string.Join(", ", m.Parameters.Select(static p => $"{p.Type.ToDisplayString()} {p.Name}"));
      var argsList = string.Join(", ", m.Parameters.Select(static p => p.Name));

      sb.Append(innerIndent).AppendLine($"public {ret} {m.Name}({paramsList})");
      sb.Append(innerIndent).AppendLine("{");
      if (lazy) sb.AppendLine($"{innerIndent}    __Ensure_{m.Name}();");
      sb.Append(innerIndent).Append("    ");
      if (!m.ReturnsVoid) sb.Append("return ");
      sb.AppendLine($"__fp_{m.Name}({argsList});");
      sb.Append(innerIndent).AppendLine("}");
    }

    // 4. Close all braces
    sb.Append(Indent()).AppendLine("}"); // Close __Impl
    while (indentLevel > 0)
    {
      indentLevel--;
      sb.Append(Indent()).AppendLine("}"); // Close nested types
    }

    return sb.ToString();
  }

  private static void EmitLazy(StringBuilder sb, IMethodSymbol[] methods, INamedTypeSymbol? methodAttrSymbol, CallingConvention defaultCc)
  {
    // Fields
    foreach (var m in methods)
    {
      var fp = GetFpType(m, methodAttrSymbol, defaultCc);
      sb.AppendLine($"private static readonly global::System.Threading.Lock __lock_{m.Name} = new();"); // .NET 9
      sb.AppendLine($"private static nint __addr_{m.Name};");
      sb.AppendLine($"private static bool __resolved_{m.Name};"); // TODO/CONS: maybe remove this and do a null check against addr field
      sb.AppendLine($"private static {fp} __fp_{m.Name};");
      sb.AppendLine();
    }

    // Ensure methods
    foreach (var m in methods)
    {
      var entry = m.Name;
      sb.AppendLine($"private static void __Ensure_{m.Name}()");
      sb.AppendLine("{");
      sb.AppendLine($"    if (__resolved_{m.Name}) return;");
      sb.AppendLine($"    __lock_{m.Name}.Enter();");
      sb.AppendLine("    try");
      sb.AppendLine("    {");
      sb.AppendLine($"        if (__resolved_{m.Name}) return;");
      sb.AppendLine($"        if (!global::System.Runtime.InteropServices.NativeLibrary.TryGetExport(__lib, {entry.Literal}, out __addr_{m.Name}))");
      sb.AppendLine("        {");
      sb.AppendLine("            ThrowEntryPointNotFoundException();");
      sb.AppendLine("        }");
      sb.AppendLine($"        __fp_{m.Name} = ({GetFpType(m, methodAttrSymbol, defaultCc)})__addr_{m.Name};");
      sb.AppendLine($"        __resolved_{m.Name} = true;");
      sb.AppendLine("    }");
      sb.AppendLine("    finally"); // Ensure the Lock is freed even if exception is thrown
      sb.AppendLine("    {");
      sb.AppendLine($"        __lock_{m.Name}.Exit();");
      sb.AppendLine("    }");
      sb.AppendLine("    return;");
      sb.AppendLine("    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining), global::System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]");
      sb.AppendLine("    static void ThrowEntryPointNotFoundException() =>");
      sb.AppendLine($"        throw new global::System.EntryPointNotFoundException({entry.Literal});");
      sb.AppendLine("}");
      sb.AppendLine();
    }
  }

  private static void EmitEager(StringBuilder sb, IMethodSymbol[] methods, INamedTypeSymbol? methodAttrSymbol, CallingConvention defaultCc)
  {
    sb.AppendLine("static __Impl()");
    sb.AppendLine("{");

    // Initialize function pointers
    foreach (var m in methods)
    {
      var entry = m.Name;
      sb.AppendLine("    {");
      sb.AppendLine($"        if (!global::System.Runtime.InteropServices.NativeLibrary.TryGetExport(__lib, {entry.Literal}, out var __addr_{m.Name}))");
      sb.AppendLine("        {");
      sb.AppendLine("            ThrowEntryPointNotFoundException();");
      sb.AppendLine("            [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining), global::System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]");
      sb.AppendLine("            static void ThrowEntryPointNotFoundException() =>");
      sb.AppendLine($"                throw new global::System.EntryPointNotFoundException({entry.Literal});");
      sb.AppendLine("        }");
      sb.AppendLine($"        __fp_{m.Name} = ({GetFpType(m, methodAttrSymbol, defaultCc)})__addr_{m.Name};");
      sb.AppendLine("    }");
    }

    sb.AppendLine("}");
    sb.AppendLine();

    // Function pointer fields
    foreach (var m in methods)
    {
      sb.AppendLine($"private static readonly {GetFpType(m, methodAttrSymbol, defaultCc)} __fp_{m.Name};");
    }
  }

  private static string GetFpType(IMethodSymbol m, INamedTypeSymbol? methodAttrSymbol, CallingConvention defaultCc)
  {
    // TODO/FIXME: switch to typeof(CallConv*)
    var cc = defaultCc;
    var ccString = cc switch
    {
      CallingConvention.Cdecl => "unmanaged[Cdecl]",
      CallingConvention.StdCall => "unmanaged[Stdcall]",
      CallingConvention.ThisCall => "unmanaged[Thiscall]",
      CallingConvention.FastCall => "unmanaged[Fastcall]",
      _ => "unmanaged" // platform-default
    };

    var args = string.Join(", ", m.Parameters.Select(static p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
    if (args.Length > 0) args += ", ";

    return $"delegate* {ccString}<{args}{m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>"; // C# 9 (function pointer)
  }

  private static bool IsBlittable(ITypeSymbol t)
  {
    // TODO/CONS: maybe remove this, or return true and let compiler/runtime handle it (crash in worst case)
    //return true;

    if (t is null)
    {
      return false;
    }
    if (t is IPointerTypeSymbol or IFunctionPointerTypeSymbol || t.TypeKind == TypeKind.Enum)
    {
      return true;
    }
    return t.SpecialType switch
    {
      SpecialType.System_Void => true,
      SpecialType.System_SByte => true,
      SpecialType.System_Byte => true,
      SpecialType.System_Int16 => true,
      SpecialType.System_UInt16 => true,
      SpecialType.System_Int32 => true,
      SpecialType.System_UInt32 => true,
      SpecialType.System_Int64 => true,
      SpecialType.System_UInt64 => true,
      SpecialType.System_IntPtr => true,
      SpecialType.System_UIntPtr => true,
      SpecialType.System_Single => true,
      SpecialType.System_Double => true,
      // NOTE: bool/char are special, whether they are blittable depends on the context...
      SpecialType.System_Boolean => true,
      SpecialType.System_Char => true,
      // Use best approximation of "blittable" that matches the language's unmanaged rules;
      // General language rule for unmanaged types includes primitives, structs with only unmanaged fields, etc.
      _ => t.IsUnmanagedType
    };
  }

  private static bool IsPartial(INamedTypeSymbol type)
    => type.DeclaringSyntaxReferences
      .Select(static r => r.GetSyntax())
      .OfType<TypeDeclarationSyntax>()
      .Any(static t => t.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)));

  private static string GetTypeKind(INamedTypeSymbol sym) =>
    // Proper Roslyn way (helper for determining class/struct/interface/record)
    sym switch
    {
      { IsRecord: true, IsValueType: true } => "record struct",
      { IsRecord: true, IsValueType: false } => "record",
      { IsValueType: true } => "struct",
      { TypeKind: TypeKind.Interface } => "interface",
      _ => "class"
    };

  private static string GetAccessibilityString(ISymbol symbol) =>
    symbol.DeclaredAccessibility switch
    {
      Accessibility.Public => "public ",
      Accessibility.Private => "private ",
      Accessibility.Internal => "internal ",
      Accessibility.Protected => "protected ",
      Accessibility.ProtectedOrInternal => "protected internal ",
      Accessibility.ProtectedAndInternal => "private protected ",
      _ => ""
    };
}

internal static partial class Extensions
{
  // C# 14 extension blocks are awesome
  extension(string @this)
  {
    public string Literal => SymbolDisplay.FormatLiteral(@this, true);
  }
}
