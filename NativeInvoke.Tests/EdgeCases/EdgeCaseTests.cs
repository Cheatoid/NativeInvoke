using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.EdgeCases;

/// <summary>
/// Tests for edge cases and advanced scenarios
/// </summary>
[TestFixture]
public class EdgeCaseTests
{
  private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

  [Test]
  public void GenerateCode_InterfaceWithGenericMethods_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void GenericMethod<T>(T input) where T : unmanaged;  // Generic methods with unmanaged constraint should be handled
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
      SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

    // Assert
    // Generic methods with unmanaged constraint should be handled without errors
    Assert.That(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False,
      "Should not crash on generic methods with unmanaged constraint");
  }

  [Test]
  public void GenerateCode_InterfaceWithOverloadedMethods_HandlesCorrectly()
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
    double Add(double a, double b);

    [NativeImportMethod]
    float Add(float a, float b);
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    // Should generate all three overloaded methods with unique names
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add" });

    // Check that there are multiple function pointer fields (one for each overload)
    var functionPointerCount = generatedCode!.Split("private static readonly delegate* unmanaged").Length - 1;
    Assert.That(functionPointerCount, Is.EqualTo(3), "Should generate 3 function pointers for overloaded methods");
  }

  [Test]
  public void GenerateCode_InterfaceWithRefParameters_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void RefMethod(ref int value);

    [NativeImportMethod]
    void OutMethod(out int value);

    [NativeImportMethod]
    void InMethod(in int value);
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "RefMethod", "OutMethod", "InMethod" });
  }

  [Test]
  public void GenerateCode_InterfaceWithPointerParameters_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void PointerMethod(int* ptr);

    [NativeImportMethod]
    void VoidPointerMethod(void* ptr);
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "PointerMethod", "VoidPointerMethod" });
  }

  [Test]
  public void GenerateCode_InterfaceWithEnumParameters_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public enum TestEnum : int
{
    Value1 = 1,
    Value2 = 2
}

public interface ITestInterface
{
    [NativeImportMethod]
    void EnumMethod(TestEnum value);

    [NativeImportMethod]
    TestEnum ReturnEnumMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "EnumMethod", "ReturnEnumMethod" });
  }

  [Test]
  public void GenerateCode_InterfaceWithBlittableStructs_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

[StructLayout(LayoutKind.Sequential)]
public struct BlittableStruct
{
    public int IntField;
    public float FloatField;
    public double DoubleField;
}

public interface ITestInterface
{
    [NativeImportMethod]
    void StructMethod(BlittableStruct input);

    [NativeImportMethod]
    BlittableStruct ReturnStructMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "StructMethod", "ReturnStructMethod" });
  }

  [Test]
  public void GenerateCode_DiamondInheritance_HandlesDuplicateMethodsCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface IBase1
{
    [NativeImportMethod]
    void CommonMethod();

    [NativeImportMethod]
    void Base1Method();
}

public interface IBase2
{
    [NativeImportMethod]
    void CommonMethod();  // Same signature as in IBase1

    [NativeImportMethod]
    void Base2Method();
}

public interface IDerived : IBase1, IBase2
{
    [NativeImportMethod]
    void DerivedMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"", Inherited = true)]
    public static partial IDerived TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    // Should have unique methods (CommonMethod should appear only once)
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[] { "CommonMethod", "Base1Method", "Base2Method", "DerivedMethod" });

    // Verify no duplicate implementations
    var commonMethodCount = generatedCode!.Split("public void CommonMethod(").Length - 1;
    Assert.That(commonMethodCount, Is.EqualTo(1), "CommonMethod should appear only once");
  }

  [Test]
  public void GenerateCode_KeywordParameterNames_EscapesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    int Add(int @int, int @string, int @bool);

    [NativeImportMethod]
    void Process(ref int @ref, out int @out);
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }

    public static partial ITestInterface TestProperty => throw new System.NotImplementedException();
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });

    // Verify parameter names are properly escaped
    Assert.That(generatedCode!, Does.Contain("@int"));
    Assert.That(generatedCode!, Does.Contain("@string"));
    Assert.That(generatedCode!, Does.Contain("@bool"));
    Assert.That(generatedCode!, Does.Contain("@ref"));
    Assert.That(generatedCode!, Does.Contain("@out"));
  }

  [Test]
  public void GenerateCode_LongMethodNames_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void VeryLongMethodNameThatExceedsNormalLengthAndMightCauseIssues();

    [NativeImportMethod]
    int AnotherVeryLongMethodNameWithLotsOfCharacters(int parameter);
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[]
      {
        "VeryLongMethodNameThatExceedsNormalLengthAndMightCauseIssues",
        "AnotherVeryLongMethodNameWithLotsOfCharacters"
      });
  }

  [Test]
  public void GenerateCode_SpecialCharactersInNames_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITest_Interface  // Contains underscore
{
    [NativeImportMethod]
    void Test_Method();  // Contains underscore
}

public static partial class Test_Class  // Contains underscore
{
    [NativeImport(""test_lib"")]  // Contains underscore
    public static partial ITest_Interface Test_Property { get; }  // Contains underscore
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "Test_Class.Test_Property");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!, "ITest_Interface", "Test_Property");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Test_Method" });
  }

  [Test]
  public void GenerateCode_NestedInterfaces_HandlesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public static partial class OuterClass
{
    public interface INestedInterface
    {
        [NativeImportMethod]
        void NestedMethod();
    }

    public static partial class InnerClass
    {
        [NativeImport(""testlib"")]
        public static partial INestedInterface TestProperty { get; }
    }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "InnerClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "NestedMethod" });
  }
}
