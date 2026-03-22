using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace NativeInvoke.Tests.Integration;

/// <summary>
/// Integration tests to verify NativeInvoke works as a compile-time only dependency
/// </summary>
[TestFixture]
public class CompileTimeOnlyTests
{
  [Test]
  public void NuGetPackage_ConsumedWithoutRuntimeDependency_ShouldWork()
  {
    // This test simulates how a consumer would use NativeInvoke from NuGet
    // where attributes are included for compilation but stripped from output

    var consumerCode = @"
using System;
using NativeInvoke;

namespace ConsumerTest
{
    public class Consumer
    {
        [NativeImport(""kernel32"")]
        public static partial IKernel Kernel { get; }

        public static void Test()
        {
            var result = Kernel.Beep(750, 300);
            Console.WriteLine($""Beep result: {result}"");
        }
    }

    public interface IKernel
    {
        [NativeImportMethod]
        int Beep(uint dwFreq, uint dwDuration);
    }
}";

    var assembly = CompileAsConsumer(consumerCode);

    // Verify the assembly works correctly
    VerifyConsumerAssembly(assembly);
  }

  [Test]
  public void MultipleNativeLibraries_ShouldAllGenerateCorrectly()
  {
    var consumerCode = @"
using System;
using NativeInvoke;

namespace MultiLibTest
{
    public class NativeLibs
    {
        [NativeImport(""kernel32"")]
        public static partial IKernel Kernel { get; }

        [NativeImport(""user32"")]
        public static partial IUser32 User { get; }

        [NativeImport(""advapi32"")]
        public static partial IAdvApi AdvApi { get; }
    }

    public interface IKernel
    {
        [NativeImportMethod(EntryPoint = ""GetTickCount"")]
        uint GetTickCount();
    }

    public interface IUser32
    {
        [NativeImportMethod(EntryPoint = ""GetTickCount"")]
        uint GetTickCount();
    }

    public interface IAdvApi
    {
        [NativeImportMethod(EntryPoint = ""GetTickCount"")]
        uint GetTickCount();
    }
}";

    var assembly = CompileAsConsumer(consumerCode);

    // Should have 3 generated implementation classes
    var implTypes = assembly.GetTypes()
      .Where(t => t.Name.StartsWith("__Impl_"))
      .ToArray();

    Assert.That(implTypes.Length, Is.EqualTo(3),
      "Should generate implementation for each native library");
  }

  [Test]
  public void ComplexInterfaceWithMultipleMethods_ShouldGenerateCorrectly()
  {
    var consumerCode = @"
using System;
using NativeInvoke;

namespace ComplexTest
{
    public class ComplexLib
    {
        [NativeImport(""kernel32"", CallingConvention = CallingConvention.StdCall)]
        public static partial IComplexKernel Kernel { get; }
    }

    public interface IComplexKernel
    {
        [NativeImportMethod(EntryPoint = ""GetTickCount"")]
        uint GetTickCount();

        [NativeImportMethod(EntryPoint = ""GetCurrentThreadId"")]
        uint GetCurrentThreadId();

        [NativeImportMethod(EntryPoint = ""GetCurrentProcessId"")]
        uint GetCurrentProcessId();

        [NativeImportMethod(SuppressGCTransition = true)]
        void Sleep(uint dwMilliseconds);
    }
}";

    var assembly = CompileAsConsumer(consumerCode);

    // Verify the implementation class has all methods
    var implType = assembly.GetTypes()
      .FirstOrDefault(t => t.Name.StartsWith("__Impl_"));

    Assert.That(implType, Is.Not.Null, "Should have generated implementation");

    var methods = implType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
      .Where(m => !m.IsSpecialName)
      .ToArray();

    Assert.That(methods.Length, Is.GreaterThanOrEqualTo(4),
      "Should implement all interface methods");
  }

  private static Assembly CompileAsConsumer(string sourceCode)
  {
    // Create a compilation that simulates a consumer project
    var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

    var compilationOptions = new CSharpCompilationOptions(
      OutputKind.DynamicallyLinkedLibrary,
      optimizationLevel: OptimizationLevel.Release,
      allowUnsafe: true);

    // Get basic references
    var references = GetBasicReferences();

    // Create compilation
    var compilation = CSharpCompilation.Create(
      "ConsumerAssembly",
      new[] { syntaxTree },
      references,
      compilationOptions);

    // Add NativeInvoke source generator (simulates NuGet package analyzer)
    var generator = new NativeInvoke.Generator.NativeImportGenerator();
    var driver = CSharpGeneratorDriver.Create(generator);
    var x = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

    // Check for compilation errors
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    if (errors.Any())
    {
      var errorMessages = string.Join(Environment.NewLine, errors.Select(e => e.GetMessage()));
      throw new InvalidOperationException($"Compilation failed: {errorMessages}");
    }

    return EmitAssembly((CSharpCompilation)updatedCompilation);
  }

  private static Assembly EmitAssembly(CSharpCompilation compilation)
  {
    using var ms = new MemoryStream();
    var result = compilation.Emit(ms);

    if (!result.Success)
    {
      var errors = string.Join(Environment.NewLine,
        result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error));
      throw new InvalidOperationException($"Compilation failed: {errors}");
    }

    return Assembly.Load(ms.ToArray());
  }

  private static void VerifyConsumerAssembly(Assembly assembly)
  {
    // 1. Check that NativeInvoke attributes are NOT present
    var attributeTypes = assembly.GetTypes()
      .Where(t => t.Name.Contains("NativeImportAttribute"))
      .ToArray();

    Assert.That(attributeTypes, Is.Empty,
      "NativeInvoke attributes should be stripped from consumer assembly");

    // 2. Check that generated implementation classes ARE present
    var implTypes = assembly.GetTypes()
      .Where(t => t.Name.StartsWith("__Impl_"))
      .ToArray();

    Assert.That(implTypes, Is.Not.Empty,
      "Generated implementation classes should be present");

    // 3. Check that consumer class IS present
    var consumerType = assembly.GetTypes()
      .FirstOrDefault(t => t.Name.Contains("Consumer") || t.Name.Contains("NativeLib"));

    Assert.That(consumerType, Is.Not.Null,
      "Consumer class should be present");

    // 4. Check that no NativeInvoke assembly references exist
    var nativeInvokeReferences = assembly.GetReferencedAssemblies()
      .Where(r => r.Name.Contains("NativeInvoke"))
      .ToArray();

    Assert.That(nativeInvokeReferences, Is.Empty,
      "Should not reference NativeInvoke assemblies (compile-time only)");
  }

  private static PortableExecutableReference[] GetBasicReferences()
  {
    var assemblies = new[]
    {
      typeof(object).Assembly, // System.Runtime
      typeof(Attribute).Assembly, // System.Runtime
      typeof(System.Runtime.CompilerServices.NullableAttribute).Assembly, // System.Runtime
      typeof(System.IntPtr).Assembly, // System.Runtime.InteropServices
      typeof(System.Runtime.InteropServices.NativeLibrary).Assembly,
      typeof(Console).Assembly, // System.Console
    };

    return assemblies
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .ToArray();
  }
}
