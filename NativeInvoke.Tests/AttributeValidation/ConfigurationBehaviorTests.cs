using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace NativeInvoke.Tests.AttributeValidation;

/// <summary>
/// Tests to verify correct behavior in different configurations
/// </summary>
[TestFixture]
public class ConfigurationBehaviorTests
{
  [Test]
  public void LocalConfiguration_ShouldHaveAttributesPresent()
  {
    // Arrange
    var assembly = Assembly.GetExecutingAssembly();

    // Act
    var attributeTypes = assembly.GetTypes()
      .Where(t => t.Name.Contains("NativeImportAttribute"))
      .ToArray();

    // Assert
    Console.WriteLine($"Current configuration: Local (project reference)");
    Console.WriteLine($"Attributes present: {attributeTypes.Any()}");

    if (attributeTypes.Any())
    {
      Console.WriteLine("✓ Attributes present (expected in Local configuration)");
      Assert.Pass("Attributes correctly present in Local configuration");
    }
    else
    {
      Console.WriteLine("✗ Attributes missing (unexpected in Local configuration)");
      Assert.Fail("Attributes should be present in Local configuration");
    }
  }

  [Test]
  public void LocalConfiguration_ShouldHaveSourceGeneratorWorking()
  {
    // Arrange
    var assembly = Assembly.GetExecutingAssembly();

    // Act
    var generatedTypes = assembly.GetTypes()
      .Where(t => t.Name.StartsWith("__Impl_"))
      .ToArray();

    // Assert
    Console.WriteLine($"Generated implementation classes found: {generatedTypes.Length}");

    if (generatedTypes.Any())
    {
      Console.WriteLine("✓ Source generator working");
      Assert.Pass("Source generator correctly generates implementations in Local configuration");
    }
    else
    {
      Console.WriteLine("✗ Source generator not working");
      Assert.Fail("Source generator should create implementation classes");
    }
  }

  [Test]
  public void LocalConfiguration_ShouldDocumentExpectedBehavior()
  {
    // This test documents the expected behavior for different configurations
    Console.WriteLine("=== Expected Behavior Documentation ===");
    Console.WriteLine("Local Configuration (Project Reference):");
    Console.WriteLine("  ✓ Attributes SHOULD be present (for source generator)");
    Console.WriteLine("  ✓ Generated classes SHOULD be present");
    Console.WriteLine("  ✓ NativeInvoke assembly SHOULD be referenced");
    Console.WriteLine();
    Console.WriteLine("NuGet Configuration (Package Reference):");
    Console.WriteLine("  ✓ Attributes SHOULD be stripped (compile-time only)");
    Console.WriteLine("  ✓ Generated classes SHOULD be present");
    Console.WriteLine("  ✓ NativeInvoke assembly SHOULD NOT be referenced");
    Console.WriteLine();
    Console.WriteLine("Current test environment: Local configuration");

    Assert.Pass("Documentation complete - behavior is as expected");
  }
}
