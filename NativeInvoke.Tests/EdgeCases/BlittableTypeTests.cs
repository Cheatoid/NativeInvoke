using NativeInvoke.Tests.Helpers;

namespace NativeInvoke.Tests.EdgeCases;

/// <summary>
/// Tests for blittable type validation
/// </summary>
[TestFixture]
public class BlittableTypeTests
{
    private static readonly IIncrementalGenerator Generator = new NativeImportGenerator();

    [Test]
    public void GenerateCode_PrimitiveTypes_AreBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    sbyte TestSByte(sbyte input);

    [NativeImportMethod]
    byte TestByte(byte input);

    [NativeImportMethod]
    short TestShort(short input);

    [NativeImportMethod]
    ushort TestUShort(ushort input);

    [NativeImportMethod]
    int TestInt(int input);

    [NativeImportMethod]
    uint TestUInt(uint input);

    [NativeImportMethod]
    long TestLong(long input);

    [NativeImportMethod]
    ulong TestULong(ulong input);

    [NativeImportMethod]
    float TestFloat(float input);

    [NativeImportMethod]
    double TestDouble(double input);

    [NativeImportMethod]
    nint TestNInt(nint input);

    [NativeImportMethod]
    nuint TestNUInt(nuint input);

    [NativeImportMethod]
    bool TestBool(bool input);

    [NativeImportMethod]
    char TestChar(char input);
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
        Assert.That(diagnostics.Where(d => d.Id == "NINVK004"), Is.Empty,
            "All primitive types should be considered blittable");
    }

    [Test]
    public void GenerateCode_EnumTypes_AreBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public enum ByteEnum : byte { Value1, Value2 }
public enum IntEnum : int { Value1, Value2 }
public enum LongEnum : long { Value1, Value2 }
public enum UIntEnum : uint { Value1, Value2 }

public interface ITestInterface
{
    [NativeImportMethod]
    ByteEnum TestByteEnum(ByteEnum input);

    [NativeImportMethod]
    IntEnum TestIntEnum(IntEnum input);

    [NativeImportMethod]
    LongEnum TestLongEnum(LongEnum input);

    [NativeImportMethod]
    UIntEnum TestUIntEnum(UIntEnum input);
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
        Assert.That(diagnostics.Where(d => d.Id == "NINVK004"), Is.Empty,
            "All enum types should be considered blittable");
    }

    [Test]
    public void GenerateCode_PointerTypes_AreBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    int* TestIntPtr(int* input);

    [NativeImportMethod]
    void* TestVoidPtr(void* input);

    [NativeImportMethod]
    byte* TestBytePtr(byte* input);

    [NativeImportMethod]
    delegate* unmanaged<int> TestFunctionPtr(delegate* unmanaged<int> input);
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
        Assert.That(diagnostics.Where(d => d.Id == "NINVK004"), Is.Empty,
            "All pointer types should be considered blittable");
    }

    [Test]
    public void GenerateCode_BlittableStructs_AreBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

[StructLayout(LayoutKind.Sequential)]
public struct BlittableStruct1
{
    public int IntField;
    public float FloatField;
}

[StructLayout(LayoutKind.Sequential)]
public struct BlittableStruct2
{
    public byte ByteField;
    public double DoubleField;
    public bool BoolField;
    public char CharField;
}

[StructLayout(LayoutKind.Sequential)]
public struct NestedBlittableStruct
{
    public BlittableStruct1 Nested;
    public BlittableStruct2 AnotherNested;
    public long LongField;
}

public interface ITestInterface
{
    [NativeImportMethod]
    BlittableStruct1 TestStruct1(BlittableStruct1 input);

    [NativeImportMethod]
    BlittableStruct2 TestStruct2(BlittableStruct2 input);

    [NativeImportMethod]
    NestedBlittableStruct TestNestedStruct(NestedBlittableStruct input);
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
        Assert.That(diagnostics.Where(d => d.Id == "NINVK004"), Is.Empty,
            "Blittable structs should be considered blittable");
    }

    [Test]
    public void GenerateCode_NonBlittableStructs_AreNotBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

[StructLayout(LayoutKind.Sequential)]
public struct NonBlittableStruct1
{
    public string StringField;  // Non-blittable field
}

[StructLayout(LayoutKind.Sequential)]
public struct NonBlittableStruct2
{
    public object ObjectField;  // Non-blittable field
}

public struct NonBlittableStruct3  // No explicit layout
{
    public int IntField;
    public string StringField;  // Non-blittable field
}

public interface ITestInterface
{
    [NativeImportMethod]
    NonBlittableStruct1 TestStruct1(NonBlittableStruct1 input);

    [NativeImportMethod]
    NonBlittableStruct2 TestStruct2(NonBlittableStruct2 input);

    [NativeImportMethod]
    NonBlittableStruct3 TestStruct3(NonBlittableStruct3 input);
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
        var nonBlittableErrors = diagnostics.Where(d => d.Id == "NINVK004").ToArray();
        Assert.That(nonBlittableErrors.Length, Is.EqualTo(3),
            "All non-blittable structs should generate errors");
    }

    [Test]
    public void GenerateCode_ReferenceTypes_AreNotBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    string TestString(string input);

    [NativeImportMethod]
    object TestObject(object input);

    [NativeImportMethod]
    int[] TestArray(int[] input);

    [NativeImportMethod]
    System.Collections.Generic.List<int> TestList(System.Collections.Generic.List<int> input);
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
        var nonBlittableErrors = diagnostics.Where(d => d.Id == "NINVK004").ToArray();
        Assert.That(nonBlittableErrors.Length, Is.EqualTo(4),
            "All reference types should generate non-blittable errors");
    }

    [Test]
    public void GenerateCode_MixedBlittableAndNonBlittable_ReportsOnlyNonBlittable()
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
}

public interface ITestInterface
{
    [NativeImportMethod]
    int BlittableMethod(int a, int b);  // Blittable

    [NativeImportMethod]
    BlittableStruct BlittableStructMethod(BlittableStruct input);  // Blittable

    [NativeImportMethod]
    string NonBlittableMethod(string input);  // Non-blittable

    [NativeImportMethod]
    object AnotherNonBlittableMethod(object input);  // Non-blittable
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
        var nonBlittableErrors = diagnostics.Where(d => d.Id == "NINVK004").ToArray();
        Assert.That(nonBlittableErrors.Length, Is.EqualTo(2),
            "Only non-blittable methods should generate errors");
    }

    [Test]
    public void GenerateCode_BlittableValidationDisabled_AllowsNonBlittable()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod]
    string TestString(string input);  // Non-blittable

    [NativeImportMethod]
    object TestObject(object input);  // Non-blittable
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = false)]  // Validation disabled
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        Assert.That(diagnostics.Where(d => d.Id == "NINVK004"), Is.Empty,
            "Should not generate blittable errors when validation is disabled");
    }

    [Test]
    public void GenerateCode_MethodLevelBlittableOverride_OverridesGlobalSetting()
    {
        // Arrange
        var sourceCode = @"
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{
    [NativeImportMethod(EnforceBlittable = false)]
    string NonBlittableMethod(string input);  // Method-level disabled

    [NativeImportMethod(EnforceBlittable = true)]
    string AnotherNonBlittableMethod(string input);  // Method-level enabled
}

public static partial class TestClass
{
    [NativeImport(""testlib"", EnforceBlittable = true)]  // Global enabled
    public static partial ITestInterface TestProperty { get; }
}";

        // Act
        var diagnostics = SourceGeneratorTestHelpers.GetGeneratorDiagnostics(
            SourceGeneratorTestHelpers.CreateCompilation(sourceCode), Generator);

        // Assert
        var nonBlittableErrors = diagnostics.Where(d => d.Id == "NINVK004").ToArray();
        Assert.That(nonBlittableErrors.Length, Is.EqualTo(1),
            "Only method with enforced blittable validation should generate error");
    }
}
