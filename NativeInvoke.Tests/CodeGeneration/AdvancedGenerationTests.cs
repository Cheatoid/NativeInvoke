using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.CodeGeneration;

/// <summary>
/// Tests for advanced code generation scenarios
/// </summary>
[TestFixture]
public class AdvancedGenerationTests
{
    private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

    [Test]
    public void GenerateCode_LazyLoading_GeneratesLazyImplementation()
    {
        // Arrange
        var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
            "\"testlib\", Lazy = true",
            @"
    [NativeImportMethod]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();",
            "TestClass");

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyLazyLoading(generatedCode!, new[] { "Add", "Process" });
        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });
    }

    [Test]
    public void GenerateCode_CustomCallingConvention_GeneratesCorrectConvention()
    {
        // Arrange
        var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
            "\"testlib\", CallingConvention = CallingConvention.Cdecl",
            @"
    [NativeImportMethod]
    int Add(int a, int b);",
            "TestClass");

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyCallingConvention(generatedCode!, CallingConvention.Cdecl);
        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add" });
    }

    [Test]
    public void GenerateCode_SuppressGCTransition_GeneratesCorrectCode()
    {
        // Arrange
        var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
            "\"testlib\", SuppressGCTransition = true",
            @"
    [NativeImportMethod]
    int Add(int a, int b);",
            "TestClass");

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyGCTransitionSuppression(generatedCode!, true);
        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add" });
    }

    [Test]
    public void GenerateCode_SymbolPrefixAndSuffix_GeneratesCorrectEntryPoints()
    {
        // Arrange
        var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
            "\"testlib\", SymbolPrefix = \"lib_\", SymbolSuffix = \"_impl\"",
            @"
    [NativeImportMethod]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();",
            "TestClass");

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Add", "lib_", "_impl");
        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Process", "lib_", "_impl");
        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });
    }

    [Test]
    public void GenerateCode_ExplicitOnly_IncludesOnlyAttributedMethods()
    {
        // Arrange
        var sourceCode = SourceGeneratorTestHelpers.CreateTestSource(
            "\"testlib\", ExplicitOnly = true",
            @"
    [NativeImportMethod]
    int IncludedMethod(int a, int b);

    int ExcludedMethod(int a, int b);",
            "TestClass");

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "IncludedMethod" });
        GeneratedCodeVerifier.VerifyExcludedMethodStub(generatedCode!, "ExcludedMethod");
    }

    [Test]
    public void GenerateCode_InheritedInterface_IncludesInheritedMethods()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface IBaseInterface
{
    [NativeImportMethod]
    void BaseMethod();
}

public interface IDerivedInterface : IBaseInterface
{
    [NativeImportMethod]
    void DerivedMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"", Inherited = true)]
    public static partial IDerivedInterface TestProperty { get; }
}";

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "BaseMethod", "DerivedMethod" });
    }

    [Test]
    public void GenerateCode_CustomMethodEntryPoint_UsesCustomEntryPoint()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(""custom_add_func"")]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();
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

        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Add", null, null);
        Assert.That(generatedCode!, Does.Contain("\"custom_add_func\""));
        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "Process", null, null);
        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "Add", "Process" });
    }

    [Test]
    public void GenerateCode_OrdinalEntryPoint_UsesOrdinalResolution()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(42)]
    void OrdinalMethod();

    [NativeImportMethod]
    void NamedMethod();
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

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "OrdinalMethod", "NamedMethod" });
        // For ordinal, the entry point should be the method name since we don't have a custom string
        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "OrdinalMethod", null, null);
        GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "NamedMethod", null, null);
    }

    [Test]
    public void GenerateCode_ExcludedMethod_GeneratesThrowNullStub()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    int IncludedMethod(int a, int b);

    [NativeImportMethod("""")]
    void ExcludedMethod();
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

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "IncludedMethod" });
        GeneratedCodeVerifier.VerifyExcludedMethodStub(generatedCode!, "ExcludedMethod");
    }

    [Test]
    public void GenerateCode_MethodLevelCallingConvention_OverridesGlobalConvention()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(CallingConvention = CallingConvention.StdCall)]
    void StdCallMethod();

    [NativeImportMethod(CallingConvention = CallingConvention.Cdecl)]
    void CdeclMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"", CallingConvention = CallingConvention.Winapi)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "StdCallMethod", "CdeclMethod" });
        Assert.That(generatedCode!, Does.Contain("[Stdcall]"));
        Assert.That(generatedCode!, Does.Contain("[Cdecl]"));
    }

    [Test]
    public void GenerateCode_MethodLevelGCTransition_OverridesGlobalSetting()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(SuppressGCTransition = true)]
    void SuppressedMethod();

    [NativeImportMethod(SuppressGCTransition = false)]
    void NotSuppressedMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"", SuppressGCTransition = false)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

        // Assert
        var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "TestClass.TestProperty");
        Assert.That(generatedCode, Is.Not.Null);

        GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "SuppressedMethod", "NotSuppressedMethod" });

        // Check that one method has suppression and one doesn't
        var suppressedCount = generatedCode!.Split("[SuppressGCTransition]").Length - 1;
        Assert.That(suppressedCount, Is.EqualTo(1), "Exactly one method should have GC transition suppression");
    }
}
