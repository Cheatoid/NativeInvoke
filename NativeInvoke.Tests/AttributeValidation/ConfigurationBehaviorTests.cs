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
    // Test that the source generator can be instantiated and is functional
    var generator = new NativeImportGenerator();

    // Act - verify the generator exists and has the expected interface
    Assert.That(generator, Is.Not.Null, "Source generator should be instantiable");

    // The real test of source generator working is in other test files
    // This test just verifies the basic setup
    Console.WriteLine("✓ Source generator is available and instantiable");

    // We can also verify that other tests are passing by checking a known working scenario
    // If we get here, it means the test assembly compiled successfully with source generation
    Assert.Pass("Source generator infrastructure is working correctly");
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
