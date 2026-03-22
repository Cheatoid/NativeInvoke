using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.Performance;

/// <summary>
/// Performance tests for the source generator
/// </summary>
[TestFixture]
public class PerformanceTests
{
  private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

  [Test]
  public void GenerateCode_LargeInterface_HandlesCorrectly()
  {
    // Arrange - Create an interface with many methods
    var methodDefinitions = new StringBuilder();
    for (int i = 0; i < 100; i++)
    {
      methodDefinitions.AppendLine($"    [NativeImportMethod]");
      methodDefinitions.AppendLine($"    int Method{i}(int a, int b);");
    }

    var sourceCode = $@"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ILargeInterface
{{
{methodDefinitions}
}}

public static partial class TestClass
{{
    [NativeImport(""testlib"")]
    public static partial ILargeInterface TestProperty {{ get; }}
}}";

    // Act
    var startTime = DateTime.UtcNow;
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
    var endTime = DateTime.UtcNow;
    var generationTime = endTime - startTime;

    // Assert
    Assert.That(generatedSources.Length, Is.GreaterThan(0), "Should generate source files");
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    // Verify all methods are generated
    for (int i = 0; i < 100; i++)
    {
      Assert.That(generatedCode!, Does.Contain($"Method{i}("),
          $"Should contain Method{i}");
    }

    // Performance assertion (should complete in reasonable time)
    Assert.That(generationTime.TotalSeconds, Is.LessThan(5.0),
        "Generation should complete within 5 seconds");
  }

  [Test]
  public void GenerateCode_MultipleProperties_HandlesCorrectly()
  {
    // Arrange - Create many properties with interfaces
    var interfaceDefinitions = new StringBuilder();
    var propertyDefinitions = new StringBuilder();

    for (int i = 0; i < 50; i++)
    {
      interfaceDefinitions.AppendLine($"public interface ITestInterface{i}");
      interfaceDefinitions.AppendLine("{");
      interfaceDefinitions.AppendLine($"    [NativeImportMethod]");
      interfaceDefinitions.AppendLine($"    int Method{i}(int a, int b);");
      interfaceDefinitions.AppendLine("}");
      interfaceDefinitions.AppendLine();

      propertyDefinitions.AppendLine($"    [NativeImport(\"testlib{i}\")]");
      propertyDefinitions.AppendLine($"    public static partial ITestInterface{i} Property{i} {{ get; }}");
    }

    var sourceCode = $@"
using System.Runtime.InteropServices;
using NativeInvoke;

{interfaceDefinitions}

public static partial class TestClass
{{
{propertyDefinitions}
}}";

    // Act
    var startTime = DateTime.UtcNow;
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
    var endTime = DateTime.UtcNow;
    var generationTime = endTime - startTime;

    // Assert
    Assert.That(generatedSources.Length, Is.EqualTo(50), "Should generate 50 source files");

    // Performance assertion
    Assert.That(generationTime.TotalSeconds, Is.LessThan(10.0),
        "Generation should complete within 10 seconds");

    // Verify each generated file is correct
    for (int i = 0; i < 50; i++)
    {
      var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, $"Property{i}");
      Assert.That(generatedCode, Is.Not.Null, $"Should generate code for Property{i}");
      Assert.That(generatedCode!, Does.Contain($"Method{i}("), $"Should contain Method{i} in Property{i}");
      Assert.That(generatedCode!, Does.Contain($"\"testlib{i}\""), $"Should reference testlib{i} in Property{i}");
    }
  }

  [Test]
  public void GenerateCode_DeepInheritanceHierarchy_HandlesCorrectly()
  {
    // Arrange - Create deep inheritance hierarchy
    var interfaceDefinitions = new StringBuilder();
    var inheritanceChain = "";

    for (int i = 0; i < 20; i++)
    {
      interfaceDefinitions.AppendLine($"public interface IInterface{i} {inheritanceChain}");
      interfaceDefinitions.AppendLine("{");
      interfaceDefinitions.AppendLine($"    [NativeImportMethod]");
      interfaceDefinitions.AppendLine($"    void Method{i}();");
      interfaceDefinitions.AppendLine("}");
      interfaceDefinitions.AppendLine();

      inheritanceChain = $": IInterface{i}";
    }

    var sourceCode = $@"
using System.Runtime.InteropServices;
using NativeInvoke;

{interfaceDefinitions}

public static partial class TestClass
{{
    [NativeImport(""testlib"", Inherited = true)]
    public static partial IInterface19 TestProperty {{ get; }}
}}";

    // Act
    var startTime = DateTime.UtcNow;
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
    var endTime = DateTime.UtcNow;
    var generationTime = endTime - startTime;

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    // Verify all inherited methods are generated
    for (int i = 0; i < 20; i++)
    {
      Assert.That(generatedCode!, Does.Contain($"Method{i}("),
          $"Should contain inherited Method{i}");
    }

    // Performance assertion
    Assert.That(generationTime.TotalSeconds, Is.LessThan(5.0),
        "Generation should complete within 5 seconds");
  }

  [Test]
  public void GenerateCode_ComplexMethodSignatures_HandlesCorrectly()
  {
    // Arrange - Create interface with complex method signatures
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

[StructLayout(LayoutKind.Sequential)]
public struct ComplexStruct
{
    public int Field1;
    public float Field2;
    public double Field3;
    public bool Field4;
    public char Field5;
}

public enum TestEnum : int { Value1, Value2, Value3 }

public interface IComplexInterface
{
    [NativeImportMethod]
    void Method1(int a, float b, double c);

    [NativeImportMethod]
    ComplexStruct Method2(ComplexStruct input);

    [NativeImportMethod]
    TestEnum Method3(TestEnum input);

    [NativeImportMethod]
    void Method4(ref int input, out float output);

    [NativeImportMethod]
    void Method5(in ComplexStruct input);

    [NativeImportMethod]
    int* Method6(int* input);

    [NativeImportMethod]
    void Method7(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j);

    [NativeImportMethod]
    ComplexStruct Method8(ComplexStruct s1, ComplexStruct s2, ComplexStruct s3);
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = true)]
    public static partial IComplexInterface TestProperty { get; }

    public static partial IComplexInterface TestProperty => throw new System.NotImplementedException();
}";

    // Act
    var startTime = DateTime.UtcNow;
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
    var endTime = DateTime.UtcNow;
    var generationTime = endTime - startTime;

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
        new[] { "Method1", "Method2", "Method3", "Method4", "Method5", "Method6", "Method7", "Method8" });

    // Performance assertion
    Assert.That(generationTime.TotalSeconds, Is.LessThan(3.0),
        "Complex signature generation should complete within 3 seconds");
  }

  [Test]
  public void GenerateCode_MixedConfigurationProperties_HandlesCorrectly()
  {
    // Arrange - Create properties with various configurations
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void Method1();

    [NativeImportMethod(""custom_entry"")]
    void Method2();

    [NativeImportMethod(42)]
    void Method3();

    [NativeImportMethod("""")]
    void Method4();

    [NativeImportMethod(CallingConvention = CallingConvention.Cdecl)]
    void Method5();

    [NativeImportMethod(SuppressGCTransition = true)]
    void Method6();

    [NativeImportMethod(EnforceBlittable = false)]
    void Method7();
}

public static partial class TestClass
{
    [NativeImport(""testlib"",
        Lazy = true,
        CallingConvention = CallingConvention.StdCall,
        SuppressGCTransition = false,
        EnforceBlittable = true,
        ExplicitOnly = false,
        Inherited = false,
        SymbolPrefix = ""pref_"",
        SymbolSuffix = ""_suff"")]
    public static partial ITestInterface Property1 { get; }

    [NativeImport(""testlib2"",
        Lazy = false,
        CallingConvention = CallingConvention.Cdecl,
        SuppressGCTransition = true,
        EnforceBlittable = false,
        ExplicitOnly = true,
        Inherited = true,
        SymbolPrefix = """",
        SymbolSuffix = """")]
    public static partial ITestInterface Property2 { get; }
}";

    // Act
    var startTime = DateTime.UtcNow;
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
    var endTime = DateTime.UtcNow;
    var generationTime = endTime - startTime;

    // Assert
    Assert.That(generatedSources.Length, Is.EqualTo(2), "Should generate 2 source files");

    var property1Code = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "Property1");
    var property2Code = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "Property2");

    Assert.That(property1Code, Is.Not.Null);
    Assert.That(property2Code, Is.Not.Null);

    // Verify Property1 (includes all methods except excluded)
    GeneratedCodeVerifier.VerifyLazyLoading(property1Code!, new[] { "Method1", "Method2", "Method3", "Method5", "Method6", "Method7" });
    GeneratedCodeVerifier.VerifyExcludedMethodStub(property1Code!, "Method4");
    GeneratedCodeVerifier.VerifyEntryPointResolution(property1Code!, "Method1", "pref_", "_suff");

    // Verify Property2 (only explicitly included methods)
    GeneratedCodeVerifier.VerifyEagerLoading(property2Code!, new[] { "Method1", "Method2", "Method3", "Method5", "Method6", "Method7" });
    GeneratedCodeVerifier.VerifyExcludedMethodStub(property2Code!, "Method4");

    // Performance assertion
    Assert.That(generationTime.TotalSeconds, Is.LessThan(5.0),
        "Mixed configuration generation should complete within 5 seconds");
  }

  [Test]
  public void GenerateCode_RepeatedGeneration_ConsistentResults()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    var generatedCodes = new List<string>();

    // Act - Generate multiple times
    for (int i = 0; i < 10; i++)
    {
      var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);
      var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
      Assert.That(generatedCode, Is.Not.Null, $"Generation {i} should produce code");

      // Normalize GUIDs to make comparison deterministic
      var normalizedCode = System.Text.RegularExpressions.Regex.Replace(generatedCode!,
          @"[a-f0-9]{32}",
          "NORMALIZED_GUID");

      generatedCodes.Add(normalizedCode);
    }

    // Assert - All generations should be identical
    for (int i = 1; i < generatedCodes.Count; i++)
    {
      Assert.That(generatedCodes[i], Is.EqualTo(generatedCodes[0]),
          $"Generation {i} should match first generation");
    }
  }
}
