namespace NativeInvoke.Tests.AttributeValidation;

/// <summary>
/// Tests for NativeImportMethodAttribute validation and behavior
/// </summary>
[TestFixture]
public class NativeImportMethodAttributeTests
{
    [Test]
    public void NativeImportMethodAttribute_ParameterlessConstructor_SetsDefaults()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute();

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.Null);
        Assert.That(attribute.CallingConvention, Is.EqualTo((CallingConvention)0)); // platform-specific
        Assert.That(attribute.SuppressGCTransition, Is.False);
        Assert.That(attribute.EnforceBlittable, Is.False);
    }

    [Test]
    public void NativeImportMethodAttribute_StringConstructor_SetsEntryPoint()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("custom_entry");

        // Assert
        Assert.That(attribute.EntryPoint, Is.EqualTo("custom_entry"));
        Assert.That(attribute.Ordinal, Is.Null);
    }

    [Test]
    public void NativeImportMethodAttribute_StringConstructor_WithEmptyString_SetsEmptyEntryPoint()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("");

        // Assert
        Assert.That(attribute.EntryPoint, Is.EqualTo(""));
        Assert.That(attribute.Ordinal, Is.Null);
    }

    [Test]
    public void NativeImportMethodAttribute_StringConstructor_WithNull_SetsNullEntryPoint()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(null);

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.Null);
    }

    [Test]
    public void NativeImportMethodAttribute_IntConstructor_SetsOrdinal()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(42);

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.EqualTo(42));
    }

    [Test]
    public void NativeImportMethodAttribute_IntConstructor_WithZero_SetsZeroOrdinal()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(0);

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.EqualTo(0));
    }

    [Test]
    public void NativeImportMethodAttribute_IntConstructor_WithNegativeOrdinal_SetsNegativeOrdinal()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(-1);

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.EqualTo(-1));
    }

    [TestCase(CallingConvention.Winapi)]
    [TestCase(CallingConvention.Cdecl)]
    [TestCase(CallingConvention.StdCall)]
    [TestCase(CallingConvention.ThisCall)]
    [TestCase(CallingConvention.FastCall)]
    public void NativeImportMethodAttribute_CallingConvention_AllValuesWork(CallingConvention callingConvention)
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("test")
        {
            CallingConvention = callingConvention
        };

        // Assert
        Assert.That(attribute.CallingConvention, Is.EqualTo(callingConvention));
    }

    [Test]
    public void NativeImportMethodAttribute_SuppressGCTransition_SettingWorks()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("test")
        {
            SuppressGCTransition = true
        };

        // Assert
        Assert.That(attribute.SuppressGCTransition, Is.True);
    }

    [Test]
    public void NativeImportMethodAttribute_EnforceBlittable_SettingWorks()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("test")
        {
            EnforceBlittable = true
        };

        // Assert
        Assert.That(attribute.EnforceBlittable, Is.True);
    }

    [Test]
    public void NativeImportMethodAttribute_AllProperties_SetCorrectly()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute("custom_func")
        {
            CallingConvention = CallingConvention.Cdecl,
            SuppressGCTransition = true,
            EnforceBlittable = false
        };

        // Assert
        Assert.That(attribute.EntryPoint, Is.EqualTo("custom_func"));
        Assert.That(attribute.Ordinal, Is.Null);
        Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.Cdecl));
        Assert.That(attribute.SuppressGCTransition, Is.True);
        Assert.That(attribute.EnforceBlittable, Is.False);
    }

    [Test]
    public void NativeImportMethodAttribute_OrdinalConstructor_AllProperties_SetCorrectly()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(123)
        {
            CallingConvention = CallingConvention.StdCall,
            SuppressGCTransition = false,
            EnforceBlittable = true
        };

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.EqualTo(123));
        Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.StdCall));
        Assert.That(attribute.SuppressGCTransition, Is.False);
        Assert.That(attribute.EnforceBlittable, Is.True);
    }

    [Test]
    public void NativeImportMethodAttribute_ParameterlessConstructor_AllProperties_SetCorrectly()
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute()
        {
            CallingConvention = CallingConvention.ThisCall,
            SuppressGCTransition = true,
            EnforceBlittable = true
        };

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.Null);
        Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.ThisCall));
        Assert.That(attribute.SuppressGCTransition, Is.True);
        Assert.That(attribute.EnforceBlittable, Is.True);
    }

    [TestCase("simple_func")]
    [TestCase("_underscore_func")]
    [TestCase("func_with_numbers_123")]
    [TestCase("MixedCaseFunc")]
    [TestCase("func.with.dots")]
    [TestCase("func_with_underscores_and_numbers_123")]
    public void NativeImportMethodAttribute_EntryPoint_AcceptsVariousStrings(string entryPoint)
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(entryPoint);

        // Assert
        Assert.That(attribute.EntryPoint, Is.EqualTo(entryPoint));
        Assert.That(attribute.Ordinal, Is.Null);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(42)]
    [TestCase(255)]
    [TestCase(65535)]
    [TestCase(-1)]
    [TestCase(-42)]
    public void NativeImportMethodAttribute_Ordinal_AcceptsVariousIntegers(int ordinal)
    {
        // Arrange & Act
        var attribute = new NativeImportMethodAttribute(ordinal);

        // Assert
        Assert.That(attribute.EntryPoint, Is.Null);
        Assert.That(attribute.Ordinal, Is.EqualTo(ordinal));
    }
}
