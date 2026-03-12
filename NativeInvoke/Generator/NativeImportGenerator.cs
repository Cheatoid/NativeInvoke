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
      if (nativeImportAttr is null || nativeImportMethodAttr is null) return; // Ensure we have our attributes

      foreach (var propDecl in propDecls)
      {
        var model = compilation.GetSemanticModel(propDecl.SyntaxTree);
        if (model.GetDeclaredSymbol(propDecl) is not { } propSymbol) continue;

        // Properly identify, ensure it is indeed our specific attribute (not an alias with the same name)
        var attr = propSymbol.GetAttributes()
          .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, nativeImportAttr));
        if (attr is null) continue; // Bail out if the property is not annotated with our attribute

        GenerateForProperty(spc, compilation, propSymbol, attr, nativeImportMethodAttr);
      }
    });
  }

  private static void GenerateForProperty(
    SourceProductionContext spc,
    Compilation compilation,
    IPropertySymbol prop,
    AttributeData pAttr,
    INamedTypeSymbol nativeImportMethodAttr)
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

#if DEBUG
    Debugger.Launch();
#endif

    // Attribute data
    NativeImportAttribute nativeImportAttr;
    {
      var temp = new NativeImportAttribute(string.Empty); // Temporary instance to get the default values
      var libraryName = (string?)pAttr.ConstructorArguments[0].Value ?? "__Internal"; // TODO/FIXME: Report a diagnostic
      var enforceBlittable = (bool)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.EnforceBlittable))
        .Value.Value ?? temp.EnforceBlittable);
      var explicitOnly = (bool)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.ExplicitOnly))
        .Value.Value ?? temp.ExplicitOnly);
      var inherited = (bool)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.Inherited))
        .Value.Value ?? temp.Inherited);
      var lazy = (bool)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.Lazy))
        .Value.Value ?? temp.Lazy);
      var defaultCc = (CallingConvention)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.CallingConvention))
        .Value.Value ?? temp.CallingConvention); // Fallback to platform-default
      var suppressGCTransition = (bool)(pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.SuppressGCTransition))
        .Value.Value ?? temp.SuppressGCTransition);
      var symbolPrefix = (pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.SymbolPrefix))
        .Value.Value ?? temp.SymbolPrefix) as string;
      var symbolSuffix = (pAttr.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportAttribute.SymbolSuffix))
        .Value.Value ?? temp.SymbolSuffix) as string;
      nativeImportAttr = new NativeImportAttribute(libraryName)
      {
        EnforceBlittable = enforceBlittable,
        ExplicitOnly = explicitOnly,
        Inherited = inherited,
        Lazy = lazy,
        CallingConvention = defaultCc,
        SuppressGCTransition = suppressGCTransition,
        SymbolPrefix = symbolPrefix ?? string.Empty,
        SymbolSuffix = symbolSuffix ?? string.Empty
      };
    }

    // Methods
    var methods = new List<MethodData>();
    var seenMethods = new HashSet<string>(StringComparer.Ordinal); // Track seen method signatures to handle duplicates from interface hierarchy

    // Collect interfaces to process based on Inherited setting
    var interfacesToProcess = new List<INamedTypeSymbol> { iface };
    if (nativeImportAttr.Inherited)
    {
      // Add all inherited interfaces (iface.AllInterfaces returns all interfaces this interface inherits from)
      interfacesToProcess.AddRange(iface.AllInterfaces);
    }

    foreach (var member in interfacesToProcess.SelectMany(static currentIface => currentIface.GetMembers()))
    {
      if (member is not IMethodSymbol
        {
          MethodKind: MethodKind.Ordinary,
          IsAbstract: true // Skip C# 8 default interface implementations (methods with body)
        } method)
      {
        continue;
      }

      // Create a unique signature key to detect duplicates from interface hierarchy
      // Format: ReturnType|MethodName|ParamType1,ParamType2,...
      var signatureKey = BuildMethodSignatureKey(method);
      if (!seenMethods.Add(signatureKey)) continue; // Skip if already seen (duplicate from hierarchy)

      // Properly identify, ensure it is indeed our specific attribute (not an alias with the same name)
      var mAttr = method.GetAttributes()
        .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, nativeImportMethodAttr));
      string? entryPoint = null;
      int? ordinal = null;
      var shouldInclude = true;
      if (mAttr is not null) // Attribute is not present
      {
        if (mAttr.ConstructorArguments.Length > 0) // Attribute provided with arguments
        {
          var argValue = mAttr.ConstructorArguments[0].Value;
          if (argValue is int o)
          {
            ordinal = o;
          }
          else
          {
            // Treat both null and empty/whitespace strings as explicit exclusion
            entryPoint = argValue as string; // may be null
            shouldInclude = !string.IsNullOrWhiteSpace(entryPoint);
          }
        }
      }
      else if (nativeImportAttr.ExplicitOnly) // Include only if the attribute is present
      {
        shouldInclude = false;
      }

      // Reconstruct the method attribute and create method data
      var name = $"{method.Name}_{Guid.NewGuid():N}"; // Append a Guid to prevent name collisions for overloaded functions
      entryPoint = shouldInclude ? ResolveMethodEntryPoint(entryPoint, method.Name, nativeImportAttr.SymbolPrefix, nativeImportAttr.SymbolSuffix) : string.Empty;
      var cc = (mAttr?.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportMethodAttribute.CallingConvention))
        .Value.Value ?? null) as CallingConvention?;
      var suppressGCTransition = (mAttr?.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportMethodAttribute.SuppressGCTransition))
        .Value.Value ?? null) as bool?;
      var enforceBlittable = (mAttr?.NamedArguments
        .FirstOrDefault(static kv => kv.Key == nameof(NativeImportMethodAttribute.EnforceBlittable))
        .Value.Value ?? null) as bool?;
      var attr = ordinal.HasValue
        ? new NativeImportMethodAttribute(ordinal.Value)
        : new NativeImportMethodAttribute(entryPoint);
      if (cc.HasValue) attr.CallingConvention = cc.Value;
      if (suppressGCTransition.HasValue) attr.SuppressGCTransition = suppressGCTransition.Value;
      if (enforceBlittable.HasValue) attr.EnforceBlittable = enforceBlittable.Value;
      methods.Add(
        new MethodData(
          method, attr,
          name, entryPoint,
          cc ?? nativeImportAttr.CallingConvention,
          suppressGCTransition ?? nativeImportAttr.SuppressGCTransition,
          enforceBlittable ?? nativeImportAttr.EnforceBlittable,
          shouldInclude
        )
      );
    }

    // Blittability check
    //if (nativeImportAttr.EnforceBlittable) // global flag
    {
      foreach (var data in methods)
      {
        if (!data.EnforceBlittable) continue; // Check per-method override (effective value)
        var m = data.Method;
        if (!IsBlittable(m.ReturnType) || m.Parameters.Any(static p => !IsBlittable(p.Type)))
        {
          spc.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.NonBlittableSignature,
            m.Locations[0],
            $"{iface.Name}.{m.Name}"));
          return;
        }
      }
    }

    if (methods.Count > 0) // Exit early if we have no methods to process
    {
      var source = GenerateSource(containingType, prop, iface, methods.ToArray(), nativeImportAttr);
      // Generate a file per container type
      spc.AddSource($"{containingType.Name}.{prop.Name}-{Guid.NewGuid():N}.g.cs", source); // Append Guid to avoid collisions, just in case
    }
  }

  private static string ResolveMethodEntryPoint(string? entryPoint, string methodName, string? symbolPrefix, string? symbolSuffix)
  {
    if (!string.IsNullOrEmpty(entryPoint)) return entryPoint!; // Use explicit entry point
    return $"{symbolPrefix ?? ""}{methodName}{symbolSuffix ?? ""}";
  }

  private static string BuildMethodSignatureKey(IMethodSymbol method)
  {
    // Build a unique key for the method signature to detect duplicates from interface hierarchy
    // Format: ReturnType|MethodName|ParamType1,ParamType2,...
    var sb = new StringBuilder();
    sb.Append(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    sb.Append('|');
    sb.Append(method.Name);
    sb.Append('|');
    sb.Append(string.Join(",", method.Parameters.Select(static p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
    return sb.ToString();
  }

  private static string GenerateSource(
    INamedTypeSymbol type,
    IPropertySymbol prop,
    INamedTypeSymbol iface,
    MethodData[] methods,
    NativeImportAttribute nativeImportAttr)
  {
    if (methods.Length <= 0) return string.Empty;

    var sb = new StringBuilder(); // TODO: Switch to IndentedTextWriter (to fix nested indentation in Emit*)

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
    var innerImplName = $"__Impl_{prop.Name}_{Guid.NewGuid():N}"; // Append Guid to avoid collisions, just in case
    var propAcc = GetAccessibilityString(prop);
    sb.Append(Indent()).AppendLine($"{propAcc}static partial {iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {prop.Name} => field ??= new {innerImplName}();"); // C# 14 (compiler synthesized backing field)
    sb.AppendLine();

    // 4. Generate inner implementation
    // TODO/CONS: Use C# 11 file-local type (must be defined at top-level)
    sb.Append(Indent()).AppendLine($"private sealed unsafe class {innerImplName} : {iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
    sb.Append(Indent()).AppendLine("{");

    var innerIndent = Indent() + "    ";
    sb.Append(innerIndent).AppendLine("private static readonly nint __lib;"); // C# 9 (enhanced IntPtr)

    // NOTE: EmitLazy/Eager logic needs to handle the increased indentation to look clean
    var lazy = nativeImportAttr.Lazy;
    if (lazy)
    {
      sb.Append(innerIndent).AppendLine($"static {innerImplName}() {{ if (!global::System.Runtime.InteropServices.NativeLibrary.TryLoad({nativeImportAttr.LibraryName.Literal}, out __lib)) {{ throw new global::System.DllNotFoundException({nativeImportAttr.LibraryName.Literal}); }} }}");
      EmitLazy(sb, methods, nativeImportAttr, innerImplName);
    }
    else
    {
      EmitEager(sb, methods, nativeImportAttr, innerImplName);
    }

    foreach (var data in methods)
    {
      var m = data.Method;
      var ret = m.ReturnType.ToDisplayString();
      var paramsList = string.Join(", ", m.Parameters.Select(static p => $"{p.Type.ToDisplayString()} {p.Name}"));
      var argsList = string.Join(", ", m.Parameters.Select(static p => p.Name));

      if (data.ShouldInclude)
      {
        if (!lazy) sb.Append(innerIndent).AppendLine("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        sb.Append(innerIndent).AppendLine($"public {ret} {m.Name}({paramsList})");
        sb.Append(innerIndent).AppendLine("{");
        if (lazy) sb.AppendLine($"{innerIndent}    __Ensure_{data.Name}();");
        sb.Append(innerIndent).Append("    ");
        if (!m.ReturnsVoid) sb.Append("return ");
        sb.AppendLine($"__fp_{data.Name}({argsList});");
        sb.Append(innerIndent).AppendLine("}");
      }
      else
      {
        // Generate throw null stub for excluded methods
        sb.Append(innerIndent).AppendLine($"public {ret} {m.Name}({paramsList}) => throw null;");
      }
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

  private static void EmitLazy(StringBuilder sb, MethodData[] methods, NativeImportAttribute nativeImportAttr, string innerImplName)
  {
    // Fields - only for included methods
    foreach (var data in methods.Where(static m => m.ShouldInclude))
    {
      sb.AppendLine($"private static readonly global::System.Threading.Lock __lock_{data.Name} = new();"); // .NET 9
      sb.AppendLine($"private static nint __addr_{data.Name};"); // C# 9 (enhanced IntPtr)
      sb.AppendLine($"private static bool __resolved_{data.Name};"); // TODO/CONS: Maybe remove this and do a null check against addr field
      sb.AppendLine($"private static {data.FunctionPointerType} __fp_{data.Name};");
      sb.AppendLine();
    }

    // Ensure methods - only for included methods
    foreach (var data in methods.Where(static m => m.ShouldInclude))
    {
      sb.AppendLine($"private static void __Ensure_{data.Name}()");
      sb.AppendLine("{");
      sb.AppendLine($"    if (__resolved_{data.Name}) return;");
      sb.AppendLine($"    __lock_{data.Name}.Enter();");
      sb.AppendLine("    try");
      sb.AppendLine("    {");
      sb.AppendLine($"        if (__resolved_{data.Name}) return;");
      sb.AppendLine($"        if (!global::System.Runtime.InteropServices.NativeLibrary.TryGetExport(__lib, {data.EntryPoint.Literal}, out __addr_{data.Name}))");
      sb.AppendLine("        {");
      sb.AppendLine("            ThrowEntryPointNotFoundException();");
      sb.AppendLine("        }");
      sb.AppendLine($"        __fp_{data.Name} = ({data.FunctionPointerType})__addr_{data.Name};");
      sb.AppendLine($"        __resolved_{data.Name} = true;");
      sb.AppendLine("    }");
      sb.AppendLine("    finally"); // Ensure the Lock is freed even if exception is thrown
      sb.AppendLine("    {");
      sb.AppendLine($"        __lock_{data.Name}.Exit();");
      sb.AppendLine("    }");
      sb.AppendLine("    return;");
      sb.AppendLine("    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining), global::System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]");
      sb.AppendLine("    static void ThrowEntryPointNotFoundException() =>");
      sb.AppendLine($"        throw new global::System.EntryPointNotFoundException({data.EntryPoint.Literal});");
      sb.AppendLine("}");
      sb.AppendLine();
    }
  }

  private static void EmitEager(StringBuilder sb, MethodData[] methods, NativeImportAttribute nativeImportAttr, string innerImplName)
  {
    // Function pointer fields - only for included methods
    foreach (var data in methods.Where(static m => m.ShouldInclude))
    {
      sb.AppendLine($"private static readonly {data.FunctionPointerType} __fp_{data.Name};"); // C# 9 (function pointer)
    }

    sb.AppendLine();
    sb.AppendLine($"static {innerImplName}()");
    sb.AppendLine("{");
    sb.AppendLine($"    if (!global::System.Runtime.InteropServices.NativeLibrary.TryLoad({nativeImportAttr.LibraryName.Literal}, out __lib)) {{ throw new global::System.DllNotFoundException({nativeImportAttr.LibraryName.Literal}); }}");

    // Initialize function pointers - only for included methods
    foreach (var data in methods.Where(static m => m.ShouldInclude))
    {
      sb.AppendLine("    {");
      sb.AppendLine($"        if (!global::System.Runtime.InteropServices.NativeLibrary.TryGetExport(__lib, {data.EntryPoint.Literal}, out var __addr_{data.Name}))");
      sb.AppendLine("        {");
      sb.AppendLine("            ThrowEntryPointNotFoundException();");
      sb.AppendLine("            [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining), global::System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]");
      sb.AppendLine("            static void ThrowEntryPointNotFoundException() =>");
      sb.AppendLine($"                throw new global::System.EntryPointNotFoundException({data.EntryPoint.Literal});");
      sb.AppendLine("        }");
      sb.AppendLine($"        __fp_{data.Name} = ({data.FunctionPointerType})__addr_{data.Name};");
      sb.AppendLine("    }");
    }

    sb.AppendLine("}"); // Close static constructor
    sb.AppendLine();
  }

  private sealed class MethodData
  {
    public MethodData(
      IMethodSymbol Method,
      NativeImportMethodAttribute Attribute,
      string Name,
      string EntryPoint,
      CallingConvention CallingConvention,
      bool SuppressGCTransition,
      bool EnforceBlittable,
      bool ShouldInclude)
    {
      this.Method = Method;
      this.Attribute = Attribute;
      this.Name = Name;
      this.EntryPoint = EntryPoint;
      this.CallingConvention = CallingConvention;
      this.SuppressGCTransition = SuppressGCTransition;
      this.EnforceBlittable = EnforceBlittable;
      this.ShouldInclude = ShouldInclude;
      FunctionPointerType = ResolveFunctionPointerType(this);
    }

    public IMethodSymbol Method { get; }

    /// <summary>unmodified method's reconstructed attribute data</summary>
    public NativeImportMethodAttribute Attribute { get; }

    /// <summary>unique name (for field name)</summary>
    public string Name { get; }

    /// <summary>final effective entry point after resolving attributes</summary>
    public string EntryPoint { get; }

    /// <summary>final effective calling convention after resolving attributes</summary>
    public CallingConvention CallingConvention { get; }

    /// <summary>final effective suppress GC transition after resolving attributes</summary>
    public bool SuppressGCTransition { get; }

    /// <summary>final effective enforce blittable after resolving attributes</summary>
    public bool EnforceBlittable { get; }

    /// <summary>whether this method should be included in native imports (false = generate throw null stub)</summary>
    public bool ShouldInclude { get; }

    /// <summary>final computed function-pointer type</summary>
    public string FunctionPointerType { get; }

    public void Deconstruct(
      out IMethodSymbol Method,
      out NativeImportMethodAttribute Attribute,
      out string Name,
      out string EntryPoint,
      out CallingConvention CallingConvention,
      out bool SuppressGCTransition,
      out bool EnforceBlittable,
      out bool ShouldInclude,
      out string FunctionPointerType)
    {
      Method = this.Method;
      Attribute = this.Attribute;
      Name = this.Name;
      EntryPoint = this.EntryPoint;
      CallingConvention = this.CallingConvention;
      SuppressGCTransition = this.SuppressGCTransition;
      EnforceBlittable = this.EnforceBlittable;
      ShouldInclude = this.ShouldInclude;
      FunctionPointerType = this.FunctionPointerType;
    }
  }

  private static string ResolveFunctionPointerType(MethodData data)
  {
    var suppressGCTransition = data.SuppressGCTransition;
    var ccString = data.CallingConvention switch
    {
      CallingConvention.Cdecl => suppressGCTransition ? "[Cdecl, SuppressGCTransition]" : "[Cdecl]",
      CallingConvention.StdCall => suppressGCTransition ? "[Stdcall, SuppressGCTransition]" : "[Stdcall]",
      CallingConvention.ThisCall => suppressGCTransition ? "[Thiscall, SuppressGCTransition]" : "[Thiscall]",
      CallingConvention.FastCall => suppressGCTransition ? "[Fastcall, SuppressGCTransition]" : "[Fastcall]",
      _ => suppressGCTransition ? "[SuppressGCTransition]" : "" // Fallback to platform-default
    };

    var args = string.Join(", ", data.Method.Parameters.Select(static p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
    if (args.Length > 0) args += ", ";

    return $"delegate* unmanaged{ccString}<{args}{data.Method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>"; // C# 9 (function pointer)
  }

  private static bool IsBlittable(ITypeSymbol t)
  {
    // TODO/CONS: Maybe remove this, or return true and let compiler/runtime handle it (crash in worst case)
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

  private static string GetTypeKind(INamedTypeSymbol symbol) =>
    // Proper Roslyn way (helper for determining class/struct/interface/record)
    symbol switch
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
