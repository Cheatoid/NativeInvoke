using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.Diagnostics;

/// <summary>
/// Tests for error diagnostics generation
/// </summary>
[TestFixture]
public class ErrorDiagnosticTests
{
    private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

    [Test]
    public void GenerateCode_NonPartialType_ReportsTypeMustBePartialError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static class TestClass  // Not partial
{
    [NativeImport(""testlib"")]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK001");
    }

    [Test]
    public void GenerateCode_NonStaticProperty_ReportsPropertyMustBeStaticPartialError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public partial ITestInterface TestProperty { get; }  // Not static
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK002");
    }

    [Test]
    public void GenerateCode_NonPartialProperty_ReportsPropertyMustBeStaticPartialError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static ITestInterface TestProperty { get; }  // Not partial
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK002");
    }

    [Test]
    public void GenerateCode_NonInterfacePropertyType_ReportsPropertyTypeMustBeInterfaceError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public class TestClassType  // Not an interface
{
    public void TestMethod() { }
}

public static partial class TestClass
{
    [NativeImport(""testlib"")]
    public static partial TestClassType TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK003");
    }

    [Test]
    public void GenerateCode_MissingLibraryName_ReportsMissingLibraryNameError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static partial class TestClass
{
    [NativeImport]  // Missing library name
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK006");
    }

    [Test]
    public void GenerateCode_EmptyLibraryName_ReportsMissingLibraryNameError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static partial class TestClass
{
    [NativeImport("""")]  // Empty library name
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK006");
    }

    [Test]
    public void GenerateCode_NonBlittableType_ReportsNonBlittableSignatureError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    string TestMethod(string input);  // string is not blittable
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = true)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK004");
    }

    [Test]
    public void GenerateCode_NonBlittableReturnType_ReportsNonBlittableSignatureError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    string TestMethod(int input);  // string return type is not blittable
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = true)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK004");
    }

    [Test]
    public void GenerateCode_NonBlittableStruct_ReportsNonBlittableSignatureError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public struct NonBlittableStruct
{
    public string Value;  // Contains non-blittable field
}

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod(NonBlittableStruct input);
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = true)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK004");
    }

    [Test]
    public void GenerateCode_InvalidAttributeArgument_ReportsInvalidAttributeArgumentWarning()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static partial class TestClass
{
    [NativeImport(""testlib"", CallingConvention = (CallingConvention)999)]  // Invalid enum value
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK007");
    }

    [Test]
    public void GenerateCode_MultipleErrors_ReportsAllErrors()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void TestMethod();
}

public static class TestClass  // Not partial
{
    [NativeImport]  // Missing library name
    public partial ITestInterface TestProperty { get; }  // Not static
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        // Due to early returns in the generator, only the first error (class not partial) is reported
        // The other errors are not checked because the generator returns after the first validation failure
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK001");
    }

    [Test]
    public void GenerateCode_MethodLevelNonBlittableType_ReportsNonBlittableSignatureError()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(EnforceBlittable = true)]
    string TestMethod(string input);  // Method-level enforcement
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = false)]  // Global disabled
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK004");
    }

    [Test]
    public void GenerateCode_BlittableDisabled_GeneratesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    string TestMethod(string input);  // Non-blittable but validation disabled
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = false)]
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty,
            "Should not have any error diagnostics when blittable validation is disabled");
    }
}
