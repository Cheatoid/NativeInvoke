namespace NativeInvoke.Tests.AttributeValidation;

/// <summary>
/// Tests for NativeImportAttribute validation and behavior
/// </summary>
[TestFixture]
public class NativeImportAttributeTests
{
  [Test]
  public void NativeImportAttribute_Constructor_WithValidLibraryName_SetsLibraryName()
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("testlib");

    // Assert
    Assert.That(attribute.LibraryName, Is.EqualTo("testlib"));
  }

  [Test]
  public void NativeImportAttribute_Constructor_WithEmptyLibraryName_SetsLibraryName()
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("");

    // Assert
    Assert.That(attribute.LibraryName, Is.EqualTo(string.Empty));
  }

  [Test]
  public void NativeImportAttribute_DefaultPropertyValues_AreCorrect()
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("testlib");

    // Assert
    Assert.That(attribute.EnforceBlittable, Is.True);
    Assert.That(attribute.ExplicitOnly, Is.False);
    Assert.That(attribute.Inherited, Is.False);
    Assert.That(attribute.Lazy, Is.False);
    Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.Winapi));
    Assert.That(attribute.SuppressGCTransition, Is.False);
    Assert.That(attribute.SymbolPrefix, Is.EqualTo(string.Empty));
    Assert.That(attribute.SymbolSuffix, Is.EqualTo(string.Empty));
  }

  [Test]
  public void NativeImportAttribute_PropertySetters_WorkCorrectly()
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("testlib")
    {
      EnforceBlittable = false,
      ExplicitOnly = true,
      Inherited = true,
      Lazy = true,
      CallingConvention = CallingConvention.Cdecl,
      SuppressGCTransition = true,
      SymbolPrefix = "prefix_",
      SymbolSuffix = "_suffix"
    };

    // Assert
    Assert.That(attribute.EnforceBlittable, Is.False);
    Assert.That(attribute.ExplicitOnly, Is.True);
    Assert.That(attribute.Inherited, Is.True);
    Assert.That(attribute.Lazy, Is.True);
    Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.Cdecl));
    Assert.That(attribute.SuppressGCTransition, Is.True);
    Assert.That(attribute.SymbolPrefix, Is.EqualTo("prefix_"));
    Assert.That(attribute.SymbolSuffix, Is.EqualTo("_suffix"));
  }

  [TestCase(CallingConvention.Winapi)]
  [TestCase(CallingConvention.Cdecl)]
  [TestCase(CallingConvention.StdCall)]
  [TestCase(CallingConvention.ThisCall)]
  [TestCase(CallingConvention.FastCall)]
  public void NativeImportAttribute_CallingConvention_AllValuesWork(CallingConvention callingConvention)
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("testlib")
    {
      CallingConvention = callingConvention
    };

    // Assert
    Assert.That(attribute.CallingConvention, Is.EqualTo(callingConvention));
  }

  [Test]
  public void NativeImportAttribute_SymbolProperties_AcceptVariousStrings()
  {
    var testCases = new[]
    {
      ("", ""),
      ("_", "_"),
      ("prefix", "prefix"),
      ("prefix_", "prefix_"),
      ("_prefix", "_prefix"),
      ("lib_", "lib_"),
      ("_lib", "_lib"),
      ("mylib_", "mylib_"),
      ("_mylib", "_mylib")
    };

    foreach (var (prefix, suffix) in testCases)
    {
      // Arrange & Act
      var attribute = new NativeImportAttribute("testlib")
      {
        SymbolPrefix = prefix,
        SymbolSuffix = suffix
      };

      // Assert
      Assert.That(attribute.SymbolPrefix, Is.EqualTo(prefix));
      Assert.That(attribute.SymbolSuffix, Is.EqualTo(suffix));
    }
  }

  [Test]
  public void NativeImportAttribute_MultipleProperties_SetCorrectly()
  {
    // Arrange & Act
    var attribute = new NativeImportAttribute("kernel32")
    {
      EnforceBlittable = true,
      ExplicitOnly = false,
      Inherited = true,
      Lazy = false,
      CallingConvention = CallingConvention.StdCall,
      SuppressGCTransition = true,
      SymbolPrefix = "kernel32_",
      SymbolSuffix = "@4"
    };

    // Assert
    Assert.That(attribute.LibraryName, Is.EqualTo("kernel32"));
    Assert.That(attribute.EnforceBlittable, Is.True);
    Assert.That(attribute.ExplicitOnly, Is.False);
    Assert.That(attribute.Inherited, Is.True);
    Assert.That(attribute.Lazy, Is.False);
    Assert.That(attribute.CallingConvention, Is.EqualTo(CallingConvention.StdCall));
    Assert.That(attribute.SuppressGCTransition, Is.True);
    Assert.That(attribute.SymbolPrefix, Is.EqualTo("kernel32_"));
    Assert.That(attribute.SymbolSuffix, Is.EqualTo("@4"));
  }
}
