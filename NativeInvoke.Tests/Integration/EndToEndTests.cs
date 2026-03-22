using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.Integration;

/// <summary>
/// End-to-end integration tests
/// </summary>
[TestFixture]
public class EndToEndTests
{
  private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

  [Test]
  public void GenerateCode_CompleteRealWorldScenario_GeneratesCorrectly()
  {
    // Arrange - Simulate a real-world scenario with complex interface
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

// Complex interface with various method signatures
public interface IKernel32
{
    [NativeImportMethod(""GetTickCount"")]
    uint GetTickCount();

    [NativeImportMethod(""GetCurrentProcessId"")]
    uint GetCurrentProcessId();

    [NativeImportMethod(""Sleep"")]
    void Sleep(uint dwMilliseconds);

    [NativeImportMethod(""GetTickCount64"")]
    ulong GetTickCount64();

    [NativeImportMethod(CallingConvention = CallingConvention.StdCall)]
    bool Beep(uint dwFreq, uint dwDuration);
}

public static partial class NativeMethods
{
    [NativeImport(""kernel32"",
        CallingConvention = CallingConvention.Winapi,
        SuppressGCTransition = false,
        EnforceBlittable = true,
        Lazy = false)]
    public static partial IKernel32 Kernel32 { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeMethods.Kernel32");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!, "IKernel32", "Kernel32");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[] { "GetTickCount", "GetCurrentProcessId", "Sleep", "GetTickCount64", "Beep" });

    // Verify custom entry points
    Assert.That(generatedCode!, Does.Contain("\"GetTickCount\""));
    Assert.That(generatedCode!, Does.Contain("\"GetCurrentProcessId\""));
    Assert.That(generatedCode!, Does.Contain("\"Sleep\""));
    Assert.That(generatedCode!, Does.Contain("\"GetTickCount64\""));

    // Verify calling convention override
    Assert.That(generatedCode!, Does.Contain("[Stdcall]"));

    // Verify eager loading
    GeneratedCodeVerifier.VerifyEagerLoading(generatedCode!,
      new[] { "GetTickCount", "GetCurrentProcessId", "Sleep", "GetTickCount64", "Beep" });
  }

  [Test]
  public void GenerateCode_MultipleLibrariesWithDifferentSettings_GeneratesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ILib1
{
    [NativeImportMethod]
    int Add(int a, int b);

    [NativeImportMethod]
    void Process();
}

public interface ILib2
{
    [NativeImportMethod(""custom_multiply"")]
    int Multiply(int a, int b);

    [NativeImportMethod(42)]
    void OrdinalFunction();
}

public static partial class NativeLibs
{
    [NativeImport(""lib1"",
        Lazy = true,
        CallingConvention = CallingConvention.Cdecl,
        SymbolPrefix = ""lib1_"")]
    public static partial ILib1 Lib1 { get; }

    [NativeImport(""lib2"",
        Lazy = false,
        CallingConvention = CallingConvention.StdCall,
        ExplicitOnly = true)]
    public static partial ILib2 Lib2 { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    Assert.That(generatedSources.Length, Is.EqualTo(2), "Should generate two source files");

    var lib1Code = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeLibs.Lib1");
    var lib2Code = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeLibs.Lib2");

    Assert.That(lib1Code, Is.Not.Null);
    Assert.That(lib2Code, Is.Not.Null);

    // Verify Lib1 (lazy loading)
    GeneratedCodeVerifier.VerifyLazyLoading(lib1Code!, new[] { "Add", "Process" });
    GeneratedCodeVerifier.VerifyCallingConvention(lib1Code!, CallingConvention.Cdecl);
    GeneratedCodeVerifier.VerifyEntryPointResolution(lib1Code!, "Add", "lib1_", null);
    GeneratedCodeVerifier.VerifyEntryPointResolution(lib1Code!, "Process", "lib1_", null);

    // Verify Lib2 (eager loading)
    GeneratedCodeVerifier.VerifyEagerLoading(lib2Code!, new[] { "Multiply", "OrdinalFunction" });
    GeneratedCodeVerifier.VerifyCallingConvention(lib2Code!, CallingConvention.StdCall);
    Assert.That(lib2Code!, Does.Contain("\"custom_multiply\""));
  }

  [Test]
  public void GenerateCode_InterfaceInheritanceWithComplexHierarchy_GeneratesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface IBaseInterface
{
    [NativeImportMethod]
    void BaseMethod1();

    [NativeImportMethod(""base_custom"")]
    void BaseMethod2();
}

public interface IIntermediateInterface : IBaseInterface
{
    [NativeImportMethod]
    void IntermediateMethod();

    [NativeImportMethod(""intermediate_custom"")]
    void IntermediateMethodCustom();
}

public interface IFinalInterface : IIntermediateInterface
{
    [NativeImportMethod]
    void FinalMethod();
}

public static partial class NativeMethods
{
    [NativeImport(""testlib"",
        Inherited = true,
        SymbolPrefix = ""test_"")]
    public static partial IFinalInterface TestLib { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeMethods.TestLib");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[] { "BaseMethod1", "BaseMethod2", "IntermediateMethod", "IntermediateMethodCustom", "FinalMethod" });

    // Verify custom entry points
    Assert.That(generatedCode!, Does.Contain("\"base_custom\""));
    Assert.That(generatedCode!, Does.Contain("\"intermediate_custom\""));

    // Verify prefix for default entry points
    GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "BaseMethod1", "test_", null);
    GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "IntermediateMethod", "test_", null);
    GeneratedCodeVerifier.VerifyEntryPointResolution(generatedCode!, "FinalMethod", "test_", null);
  }

  [Test]
  public void GenerateCode_MixedExplicitAndImplicitMethods_GeneratesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface IMixedInterface
{
    [NativeImportMethod]  // Explicit (default included)
    void ExplicitMethod1();

    [NativeImportMethod(""custom_entry"")]  // Explicit with custom entry
    void ExplicitMethod2();

    [NativeImportMethod("""")]  // Explicitly excluded
    void ExcludedMethod();

    void ImplicitMethod1();  // Implicit (should be included)

    void ImplicitMethod2();  // Implicit (should be included)
}

public static partial class NativeMethods
{
    [NativeImport(""testlib"", ExplicitOnly = false)]  // Include implicit methods
    public static partial IMixedInterface TestLib { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeMethods.TestLib");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[] { "ExplicitMethod1", "ExplicitMethod2", "ImplicitMethod1", "ImplicitMethod2" });
    GeneratedCodeVerifier.VerifyExcludedMethodStub(generatedCode!, "ExcludedMethod");

    // Verify custom entry point
    Assert.That(generatedCode!, Does.Contain("\"custom_entry\""));
  }

  [Test]
  public void GenerateCode_AllCallingConventions_GeneratesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ICallingConventionInterface
{
    [NativeImportMethod(CallingConvention = CallingConvention.Winapi)]
    void WinApiMethod();

    [NativeImportMethod(CallingConvention = CallingConvention.Cdecl)]
    void CdeclMethod();

    [NativeImportMethod(CallingConvention = CallingConvention.StdCall)]
    void StdCallMethod();

    [NativeImportMethod(CallingConvention = CallingConvention.ThisCall)]
    void ThisCallMethod();

    [NativeImportMethod(CallingConvention = CallingConvention.FastCall)]
    void FastCallMethod();
}

public static partial class NativeMethods
{
    [NativeImport(""testlib"")]
    public static partial ICallingConventionInterface TestLib { get; }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "NativeMethods.TestLib");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!,
      new[] { "WinApiMethod", "CdeclMethod", "StdCallMethod", "ThisCallMethod", "FastCallMethod" });

    // Verify calling conventions
    Assert.That(generatedCode!, Does.Contain("[Cdecl]"));
    Assert.That(generatedCode!, Does.Contain("[Stdcall]"));
    Assert.That(generatedCode!, Does.Contain("[Thiscall]"));
    Assert.That(generatedCode!, Does.Contain("[Fastcall]"));

    // WinApi should not have explicit modifier (platform default)
    var winApiMethodCount = generatedCode!.Split("[Winapi]").Length - 1;
    Assert.That(winApiMethodCount, Is.EqualTo(0), "WinApi should not have explicit calling convention modifier");
  }

  [Test]
  public void GenerateCode_ComplexNestedTypeStructure_GeneratesCorrectly()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

namespace OuterNamespace.InnerNamespace
{
    public interface ITestInterface
    {
        [NativeImportMethod]
        void TestMethod();
    }

    public static partial class OuterClass
    {
        public static partial class MiddleClass
        {
            public static partial class InnerClass
            {
                [NativeImport(""testlib"")]
                public static partial ITestInterface TestProperty { get; }
            }
        }
    }
}";

    // Act
    var (compilation, generatedSources) = SourceGeneratorTestHelpers.RunGenerator(sourceCode, Generator);

    // Assert
    var generatedCode = SourceGeneratorTestHelpers.GetGeneratedSource(generatedSources, "InnerClass.TestProperty");
    Assert.That(generatedCode, Is.Not.Null);

    GeneratedCodeVerifier.VerifyImplementationStructure(generatedCode!,
      "OuterNamespace.InnerNamespace.ITestInterface", "TestProperty");
    GeneratedCodeVerifier.VerifyMethodImplementations(generatedCode!, new[] { "TestMethod" });

    // Verify namespace structure
    Assert.That(generatedCode!, Does.Contain("namespace OuterNamespace.InnerNamespace"));
  }
}
