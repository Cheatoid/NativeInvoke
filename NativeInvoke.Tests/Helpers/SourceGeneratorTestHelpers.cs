namespace NativeInvoke.Tests.Helpers;

/// <summary>
/// Helper class for testing source generators with Roslyn
/// </summary>
public static class SourceGeneratorTestHelpers
{
  /// <summary>
  /// Creates a compilation with the specified source code and runs the generator
  /// </summary>
  public static (Compilation Compilation, ImmutableArray<GeneratedSourceResult> GeneratedSources) RunGenerator(
    string sourceCode,
    IIncrementalGenerator generator,
    IEnumerable<string>? additionalReferences = null)
  {
    // Create compilation
    var compilation = CreateCompilation(sourceCode, additionalReferences);

    // Run generator
    var driver = CSharpGeneratorDriver.Create(generator);
    var runResult = driver.RunGenerators(compilation).GetRunResult();

    var generatedSources = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .ToImmutableArray();

    return (compilation, generatedSources);
  }

  /// <summary>
  /// Creates a compilation with the specified source code
  /// </summary>
  public static Compilation CreateCompilation(string sourceCode, IEnumerable<string>? additionalReferences = null)
  {
    var references = new List<string>
    {
      typeof(object).Assembly.Location,
      typeof(Attribute).Assembly.Location,
      typeof(Enumerable).Assembly.Location,
      typeof(RuntimeInformation).Assembly.Location,
      typeof(CSharpCompilation).Assembly.Location,
      // Add specific references for missing types
      typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).Assembly.Location,
      typeof(System.Runtime.InteropServices.CallingConvention).Assembly.Location,
      typeof(System.Runtime.InteropServices.NativeLibrary).Assembly.Location,
      // Try to find System.Runtime and System.Runtime.InteropServices in the runtime directory
      Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "", "System.Runtime.dll"),
      Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "", "System.Runtime.InteropServices.dll"),
      // Try to find netstandard.dll in the runtime directory
      Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "", "netstandard.dll"),
      typeof(NativeImportAttribute).Assembly.Location,
      // Add NativeInvoke assembly reference - try to locate it
      GetNativeInvokeAssemblyPath()
    };

    if (additionalReferences != null)
    {
      references.AddRange(additionalReferences);
    }

    var metadataReferences = references
      .Where(r => !string.IsNullOrEmpty(r))
      .Distinct()
      .Select(r => MetadataReference.CreateFromFile(r!))
      .Cast<MetadataReference>()
      .ToArray();

    var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

    return CSharpCompilation.Create(
      assemblyName: "TestAssembly",
      syntaxTrees: new[] { syntaxTree },
      references: metadataReferences,
      options: new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary,
        allowUnsafe: true,
        nullableContextOptions: NullableContextOptions.Enable));
  }

  /// <summary>
  /// Attempts to locate the NativeInvoke assembly
  /// </summary>
  private static string? GetNativeInvokeAssemblyPath()
  {
    // Try multiple possible relative paths
    var possiblePaths = new[]
    {
      // Relative to test project directory
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "bin", "NativeInvoke.dll"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "NativeInvoke", "bin", "NativeInvoke.dll"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "bin", "NativeInvoke.dll"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NativeInvoke.dll"),

      // Try current directory and subdirectories
      "NativeInvoke.dll",

      // Fallback to common output directories
      Path.Combine("..", "bin", "NativeInvoke.dll"),
      Path.Combine("..", "..", "bin", "NativeInvoke.dll")
    };

    foreach (var path in possiblePaths)
    {
      if (File.Exists(path))
      {
        return Path.GetFullPath(path);
      }
    }

    // Try to find it in the current directory and subdirectories
    var currentDir = Directory.GetCurrentDirectory();
    var dllFiles = Directory.GetFiles(currentDir, "NativeInvoke.dll", SearchOption.AllDirectories);

    if (dllFiles.Length > 0)
    {
      return dllFiles[0];
    }

    return null;
  }

  /// <summary>
  /// Gets all diagnostics from the generator run
  /// </summary>
  public static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(
    Compilation compilation,
    IIncrementalGenerator generator)
  {
    var driver = CSharpGeneratorDriver.Create(generator);
    var runResult = driver.RunGenerators(compilation).GetRunResult();

    return runResult.Results
      .SelectMany(r => r.Diagnostics)
      .ToImmutableArray();
  }

  /// <summary>
  /// Verifies that specific diagnostics are present
  /// </summary>
  public static void AssertDiagnostics(
    ImmutableArray<Diagnostic> diagnostics,
    params string[] expectedIds)
  {
    var actualIds = diagnostics.Select(d => d.Id).ToArray();

    foreach (var expectedId in expectedIds)
    {
      Assert.That(actualIds, Contains.Item(expectedId),
        $"Expected diagnostic '{expectedId}' was not found. Actual diagnostics: {string.Join(", ", actualIds)}");
    }

    Assert.That(actualIds.Length, Is.EqualTo(expectedIds.Length),
      $"Number of diagnostics mismatch. Expected {expectedIds.Length}, got {actualIds.Length}");
  }

  /// <summary>
  /// Gets the generated source code for a specific hint name
  /// </summary>
  public static string? GetGeneratedSource(
    ImmutableArray<GeneratedSourceResult> generatedSources,
    string hintName)
  {
    var source = generatedSources.FirstOrDefault(s => s.HintName.Contains(hintName));
    return generatedSources.Any(s => s.HintName.Contains(hintName)) ? source.SourceText?.ToString() : null;
  }

  /// <summary>
  /// Gets all available hint names for debugging
  /// </summary>
  public static string[] GetAvailableHintNames(ImmutableArray<GeneratedSourceResult> generatedSources)
  {
    return generatedSources.Select(s => s.HintName).ToArray();
  }

  /// <summary>
  /// Creates a test source file with the NativeImport attribute
  /// </summary>
  public static string CreateTestSource(string attributeParams, string interfaceDefinition, string className = "TestClass")
  {
    return $@"
#define NATIVEINVOKE_SOURCE_GENERATOR
using System.Runtime.InteropServices;
using NativeInvoke;

public interface ITestInterface
{{
    {interfaceDefinition}
}}

public static partial class {className}
{{
    [NativeImport({attributeParams})]
    public static partial ITestInterface TestProperty {{ get; }}

    public static partial ITestInterface TestProperty => throw new System.NotImplementedException();
}}";
  }

  /// <summary>
  /// Creates a simple interface definition for testing
  /// </summary>
  public static string CreateSimpleInterface(string methods)
  {
    return $@"
{methods}";
  }
}
