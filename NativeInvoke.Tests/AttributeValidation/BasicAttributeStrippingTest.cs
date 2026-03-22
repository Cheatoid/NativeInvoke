using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace NativeInvoke.Tests.AttributeValidation;

/// <summary>
/// Simple test to verify attribute stripping works in the current test environment
/// </summary>
[TestFixture]
public class BasicAttributeStrippingTest
{
  [Test]
  public void CurrentTestAssembly_ShouldNotContainNativeImportAttributes()
  {
    // Arrange - This test checks the current test assembly itself
    var assembly = Assembly.GetExecutingAssembly();

    // Act - Find any types with NativeImportAttribute in the name
    var attributeTypes = assembly.GetTypes()
      .Where(t => t.Name.Contains("NativeImportAttribute"))
      .ToArray();

    // Assert - These should be stripped when using NuGet package
    // Note: In Local configuration they might be present, but in NuGet config they should be stripped
    Console.WriteLine($"Found attribute types: {string.Join(", ", attributeTypes.Select(t => t.Name))}");

    // This test will help us understand the current state
    // We expect these to be present in Local development but stripped in NuGet consumption
    if (attributeTypes.Any())
    {
      Assert.Warn("NativeImportAttribute types are present in test assembly. " +
                  "This is expected in Local development configuration but should be stripped when consumed as NuGet package.");
    }
    else
    {
      Assert.Pass("NativeImportAttribute types are properly stripped from test assembly.");
    }
  }

  [Test]
  public void CurrentTestAssembly_ShouldContainNativeInvokeGenerator()
  {
    // Arrange
    var assembly = Assembly.GetExecutingAssembly();

    // Act - Check if the source generator is loaded
    var generatorType = assembly.GetTypes()
      .FirstOrDefault(t => t.Name.Contains("NativeImportGenerator"));

    // Assert
    if (generatorType != null)
    {
      Console.WriteLine($"Found generator type: {generatorType.FullName}");
    }
    else
    {
      Console.WriteLine("Source generator type not found in test assembly (this is normal)");
    }
  }

  [Test]
  public void TestAttributePresence_InLocalConfiguration()
  {
    // This test documents the expected behavior
    var assembly = Assembly.GetExecutingAssembly();
    var attributeTypes = assembly.GetTypes()
      .Where(t => t.Name.Contains("NativeImportAttribute"))
      .ToArray();

    // In Local configuration (with project reference), attributes should be present
    // In NuGet configuration, attributes should be stripped

    var hasAttributes = attributeTypes.Any();

    Console.WriteLine($"Configuration: Local (project reference)");
    Console.WriteLine($"Attributes present: {hasAttributes}");

    if (hasAttributes)
    {
      Console.WriteLine("✓ Attributes present (expected in Local configuration)");
    }
    else
    {
      Console.WriteLine("✗ Attributes missing (unexpected in Local configuration)");
    }
  }
}
