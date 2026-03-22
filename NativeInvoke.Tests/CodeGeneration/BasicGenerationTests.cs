using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.CodeGeneration;

/// <summary>
/// Tests for basic code generation scenarios
/// </summary>
[TestFixture]
public class BasicGenerationTests
{
  private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

  [Test]
  public void GenerateCode_BasicInterface_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
        "\"testlib\"",
        @"
    [NativeImportMethod]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();",
        "TestClass");

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    Assert.That(generatedSources.Length, Is.GreaterThan(0), "Should generate at least one source file");

    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null, "Should find generated source for TestProperty");

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!, "ITestInterface", "TestProperty");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });
    GeneratedCodeVerifier.VerifyEagerLoading(generatedCode!, new[] { "Add", "Process" });
  }

  [Test]
  public void GenerateCode_InterfaceWithVariousParameterTypes_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
        "\"testlib\"",
        @"
    [NativeImportMethod]
    int AddInt(int a, int b);

    [NativeImportMethod]
    double AddDouble(double a, double b);

    [NativeImportMethod]
    void ProcessFloat(float value);

    [NativeImportMethod]
    long ProcessLong(long value);",
        "TestClass");

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
        new[] { "AddInt", "AddDouble", "ProcessFloat", "ProcessLong" });
  }

  [Test]
  public void GenerateCode_InterfaceWithVoidReturn_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
        "\"testlib\"",
        @"
    [NativeImportMethod]
    void Method1();

    [NativeImportMethod]
    void Method2(int param);",
        "TestClass");

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Method1", "Method2" });
  }

  [Test]
  public void GenerateCode_InterfaceWithNoAttributes_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
        "\"testlib\"",
        @"
    int Add(int a, int b);

    void Process();",
        "TestClass");

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });
    GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Add", null, null);
    GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Process", null, null);
  }

  [Test]
  public void GenerateCode_NestedType_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

namespace TestNamespace
{
    public interface ITestInterface
    {
        [NativeImportMethod]
        int Add(int a, int b);
    }

    public static partial class OuterClass
    {
        public static partial class InnerClass
        {
            [NativeImport(""testlib"")]
            public static partial ITestInterface TestProperty { get; }
        }
    }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "InnerClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!, "TestNamespace.ITestInterface", "TestProperty");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add" });
  }

  [Test]
  public void GenerateCode_MultipleProperties_GeneratesMultipleImplementations()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface1
{
    [NativeImportMethod]
    int Add(int a, int b);
}

public interface ITestInterface2
{
    [NativeImportMethod]
    void Process();
}

public static partial class TestClass
{
    [NativeImport(""lib1"")]
    public static partial ITestInterface1 Property1 { get; }

    [NativeImport(""lib2"")]
    public static partial ITestInterface2 Property2 { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    Assert.That(generatedSources.Length, Is.EqualTo(2), "Should generate two source files");

    var generatedCode1 = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "Property1");
    var generatedCode2 = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "Property2");

    Assert.That(generatedCode1, Is.Not.Null);
    Assert.That(generatedCode2, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode1!, new[] { "Add" });
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode2!, new[] { "Process" });

    // Verify different libraries are used
    Assert.That(generatedCode1!, Does.Contain("\"lib1\""));
    Assert.That(generatedCode2!, Does.Contain("\"lib2\""));
  }

  [Test]
  public void GenerateCode_InterfaceInNamespace_GeneratesCorrectImplementation()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

namespace MyNamespace.SubNamespace
{
    public interface ITestInterface
    {
        [NativeImportMethod]
        int Add(int a, int b);
    }

    public static partial class TestClass
    {
        [NativeImport(""testlib"")]
        public static partial ITestInterface TestProperty { get; }
    }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!, "MyNamespace.SubNamespace.ITestInterface", "TestProperty");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add" });
  }
}
