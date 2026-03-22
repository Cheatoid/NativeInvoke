using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.Diagnostics;

/// <summary>
/// Tests for warning diagnostics generation
/// </summary>
[TestFixture]
public class WarningDiagnosticTests
{
  private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

  [Test]
  public void GenerateCode_EmptyInterface_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    // No methods
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithOnlyExcludedMethods_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod("""")]
    void ExcludedMethod1();

    [NativeImportMethod("""")]
    void ExcludedMethod2();
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithDefaultMethods_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    // C# 8 default interface implementation (has body)
    void DefaultMethod() { }

    // Not abstract, should be skipped
    void AnotherDefaultMethod() => Console.WriteLine();
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithStaticMethods_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    // Static interface members (C# 8+)
    static void StaticMethod() { }

    static int StaticProperty { get; } = 42;
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithProperties_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    // Properties are not methods, should be skipped
    int SomeProperty { get; set; }

    string ReadOnlyProperty { get; }
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithEvents_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System;
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    // Events are not methods, should be skipped
    event EventHandler SomeEvent;
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
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
  }

  [Test]
  public void GenerateCode_InterfaceWithValidAndInvalidMembers_GeneratesCodeWithoutWarning()
  {
    // Arrange
    var sourceCode = @"
using System;
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    void ValidMethod();  // This should be included

    int SomeProperty { get; }  // This should be ignored

    event EventHandler SomeEvent;  // This should be ignored
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
    Assert.That(diagnostics.Where(d => d.Id == "NINVK005"), Is.Empty,
        "Should not report empty interface warning when there are valid methods");
  }

  [Test]
  public void GenerateCode_InterfaceWithOnlyExplicitOnlyMethods_ReportsEmptyInterfaceWarning()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    void MethodWithoutAttribute();  // No attribute
    void AnotherMethodWithoutAttribute();  // No attribute
}

public static partial class TestClass
{
    [NativeImport(""testlib"", ExplicitOnly = true)]
    public static partial ITestInterface TestProperty { get; }
}";

    // Act
    var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
        SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

    // Assert
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK005");
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
    [NativeImport(""testlib"", CallingConvention = (CallingConvention)1234)]  // Invalid member for CallingConvention property
    public static partial ITestInterface TestProperty { get; }

    public static partial ITestInterface TestProperty => throw new System.NotImplementedException();
}";

    // Act
    var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
        SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

    // Assert
    SourceGeneratorTestHelpers.AssertDiagnostics(diagnostics, "NINVK007");
  }

  [Test]
  public void GenerateCode_MultipleWarnings_ReportsAllWarnings()
  {
    // Arrange
    var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface IEmptyInterface
{
    // No methods
}

public interface IAnotherEmptyInterface
{
    // No methods
}

public static partial class TestClass
{
    [NativeImport(""testlib"", CallingConvention = (CallingConvention)1234)]
    public static partial IEmptyInterface Property1 { get; }

    [NativeImport(""anotherlib"", CallingConvention = (CallingConvention)999)]
    public static partial IAnotherEmptyInterface Property2 { get; }
}";

    // Act
    var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
        SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

    // Assert
    var warningDiagnostics = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
    var warningIds = warningDiagnostics.Select(d => d.Id).ToArray();

    Assert.That(warningIds.Count(id => id == "NINVK005"), Is.EqualTo(2),
        "Should have 2 empty interface warnings");
    Assert.That(warningIds.Count(id => id == "NINVK007"), Is.EqualTo(2),
        "Should have 2 invalid attribute argument warnings");
  }
}
