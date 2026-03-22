using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace NativeInvoke.Tests.AttributeValidation;

/// <summary>
/// Tests to verify that NativeInvoke attributes are properly stripped from compiled assemblies
/// when consumed as a NuGet package (compile-time only dependency)
/// </summary>
[TestFixture]
public class AttributeStrippingTests
{
  private const string TestSourceCode = @"
using System;
using NativeInvoke;

namespace TestNamespace
{
    public class TestClass
    {
        [NativeImport(""kernel32"")]
        public static partial IKernel32 Kernel32 { get; }

        [NativeImport(""user32"")]
        public static partial IUser32 User32 { get; }
    }

    public interface IKernel32
    {
        [NativeImportMethod(EntryPoint = ""Beep"")]
        int Beep(uint dwFreq, uint dwDuration);
    }

    public unsafe interface IUser32
    {
        [NativeImportMethod(EntryPoint = ""MessageBoxA"")]
        int MessageBox(IntPtr hWnd, sbyte* lpText, sbyte* lpCaption, uint uType);
    }
}";

  [Test]
  public void CompiledAssembly_ShouldNotContainNativeImportAttributes()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var attributeTypes = assembly.GetTypes()
      .Where(t => t.Name.Contains("NativeImportAttribute"))
      .ToArray();

    // Assert
    Assert.That(attributeTypes, Is.Empty,
      "NativeImportAttribute and NativeImportMethodAttribute should be stripped from compiled assembly");
  }

  [Test]
  public void CompiledAssembly_ShouldContainGeneratedImplementationClasses()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var generatedTypes = assembly.GetTypes()
      .Where(t => t.Name.StartsWith("__Impl_"))
      .ToArray();

    // Assert
    Assert.That(generatedTypes, Is.Not.Empty,
      "Generated implementation classes should be present");
    Assert.That(generatedTypes.Length, Is.GreaterThanOrEqualTo(2),
      "Should have at least 2 generated implementations (Kernel32 and User32)");
  }

  [Test]
  public void CompiledAssembly_ShouldContainGeneratedTestClass()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var testClassType = assembly.GetType("TestNamespace.TestClass");

    // Assert
    Assert.That(testClassType, Is.Not.Null,
      "Test class should be present in compiled assembly");
  }

  [Test]
  public void GeneratedCode_ShouldNotHaveAttributeCustomAttributes()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var testClassType = assembly.GetType("TestNamespace.TestClass");
    var properties = testClassType?.GetProperties();

    // Assert
    Assert.That(properties, Is.Not.Null, "Properties should exist");

    foreach (var property in properties)
    {
      var attributes = property.GetCustomAttributes(false);
      var nativeImportAttributes = attributes
        .Where(a => a.GetType().Name.Contains("NativeImportAttribute"))
        .ToArray();

      Assert.That(nativeImportAttributes, Is.Empty,
        $"Property '{property.Name}' should not have NativeImportAttribute in compiled assembly");
    }
  }

  [Test]
  public void GeneratedInterfaces_ShouldNotHaveAttributeCustomAttributes()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var kernel32Type = assembly.GetType("TestNamespace.IKernel32");
    var user32Type = assembly.GetType("TestNamespace.IUser32");

    // Assert
    Assert.That(kernel32Type, Is.Not.Null, "IKernel32 interface should be present");
    Assert.That(user32Type, Is.Not.Null, "IUser32 interface should be present");

    var kernel32Methods = kernel32Type.GetMethods();
    var user32Methods = user32Type.GetMethods();

    foreach (var method in kernel32Methods.Concat(user32Methods))
    {
      var attributes = method.GetCustomAttributes(false);
      var nativeImportMethodAttributes = attributes
        .Where(a => a.GetType().Name.Contains("NativeImportMethodAttribute"))
        .ToArray();

      Assert.That(nativeImportMethodAttributes, Is.Empty,
        $"Method '{method.Name}' should not have NativeImportMethodAttribute in compiled assembly");
    }
  }

  [Test]
  public void AssemblyMetadata_ShouldIndicateDevelopmentDependency()
  {
    // Arrange
    var assembly = CreateTestCompilation();

    // Act
    var referencedAssemblies = assembly.GetReferencedAssemblies()
      .Where(r => r.Name.Contains("NativeInvoke"))
      .ToArray();

    // Assert
    Assert.That(referencedAssemblies, Is.Empty,
      "Compiled assembly should not reference NativeInvoke assemblies (development dependency)");
  }

  private static Assembly CreateTestCompilation()
  {
    // Create syntax tree from test source
    var syntaxTree = CSharpSyntaxTree.ParseText(TestSourceCode);

    // Define compilation options
    var compilationOptions = new CSharpCompilationOptions(
      OutputKind.DynamicallyLinkedLibrary,
      optimizationLevel: OptimizationLevel.Release,
      allowUnsafe: true);

    // Create compilation with necessary references
    var compilation = CSharpCompilation.Create(
      "TestAssembly",
      new[] { syntaxTree },
      GetMetadataReferences(),
      compilationOptions);

    // Add the NativeInvoke source generator
    var generator = new NativeInvoke.Generator.NativeImportGenerator();
    var driver = CSharpGeneratorDriver.Create(generator);
    var x = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out _);
    return EmitAssembly((CSharpCompilation)updatedCompilation);
  }

  private static PortableExecutableReference[] GetMetadataReferences()
  {
    var assemblies = new[]
    {
      typeof(object).Assembly, // System.Runtime
      typeof(Attribute).Assembly, // System.Runtime
      typeof(System.Runtime.CompilerServices.NullableAttribute).Assembly, // System.Runtime
      typeof(System.IntPtr).Assembly, // System.Runtime.InteropServices
      typeof(System.Runtime.InteropServices.NativeLibrary).Assembly,
    };

    return assemblies
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .ToArray();
  }

  private static Assembly EmitAssembly(CSharpCompilation compilation)
  {
    using var ms = new MemoryStream();
    var result = compilation.Emit(ms);

    if (!result.Success)
    {
      var errors = string.Join(Environment.NewLine,
        result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error));
      throw new InvalidOperationException($"Compilation failed: {errors}");
    }

    return Assembly.Load(ms.ToArray());
  }
}

/// <summary>
/// Extension methods for compilation testing
/// </summary>
public static class CompilationExtensions
{
  public static Assembly EmitAssembly(this CSharpCompilation compilation)
  {
    using var ms = new MemoryStream();
    var result = compilation.Emit(ms);

    if (!result.Success)
    {
      var errors = string.Join(Environment.NewLine,
        result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error));
      throw new InvalidOperationException($"Compilation failed: {errors}");
    }

    return Assembly.Load(ms.ToArray());
  }
}
