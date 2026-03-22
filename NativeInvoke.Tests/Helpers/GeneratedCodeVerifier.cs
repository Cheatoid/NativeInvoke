namespace NativeInvoke.Tests.Helpers;

/// <summary>
/// Helper class for verifying generated source code
/// </summary>
public static class GeneratedCodeVerifier
{
    /// <summary>
    /// Verifies that the generated code contains expected patterns
    /// </summary>
    public static void VerifyGeneratedCode(string generatedCode, params string[] expectedPatterns)
    {
        Assert.That(generatedCode, Is.Not.Null.And.Not.Empty, "Generated code should not be null or empty");

        foreach (var pattern in expectedPatterns)
        {
            Assert.That(generatedCode, Does.Contain(pattern), 
                $"Generated code should contain pattern: {pattern}");
        }
    }

    /// <summary>
    /// Verifies that the generated code does not contain prohibited patterns
    /// </summary>
    public static void VerifyCodeDoesNotContain(string generatedCode, params string[] prohibitedPatterns)
    {
        foreach (var pattern in prohibitedPatterns)
        {
            Assert.That(generatedCode, Does.Not.Contain(pattern), 
                $"Generated code should not contain pattern: {pattern}");
        }
    }

    /// <summary>
    /// Verifies that the generated implementation class has the expected structure
    /// </summary>
    public static void VerifyImplementationStructure(string generatedCode, string interfaceName, string propertyName)
    {
        var expectedPatterns = new[]
        {
            $"private sealed unsafe class __Impl_{propertyName}_",
            $" : {interfaceName}",
            "private static readonly nint __lib;",
            "static __Impl_",
            "NativeLibrary.TryLoad"
        };

        VerifyGeneratedCode(generatedCode, expectedPatterns);
    }

    /// <summary>
    /// Verifies that function pointer fields are generated correctly
    /// </summary>
    public static void VerifyFunctionPointers(string generatedCode, string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var expectedPattern = $"private static readonly delegate* unmanaged";
            Assert.That(generatedCode, Does.Contain(expectedPattern), 
                $"Should contain function pointer for method: {methodName}");
        }
    }

    /// <summary>
    /// Verifies that method implementations are generated correctly
    /// </summary>
    public static void VerifyMethodImplementations(string generatedCode, string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var expectedPattern = $"public {methodName}(";
            Assert.That(generatedCode, Does.Contain(expectedPattern), 
                $"Should contain implementation for method: {methodName}");
        }
    }

    /// <summary>
    /// Verifies lazy loading structure
    /// </summary>
    public static void VerifyLazyLoading(string generatedCode, string[] methodNames)
    {
        var lazyPatterns = new[]
        {
            "private static readonly global::System.Threading.Lock __lock_",
            "private static nint __addr_",
            "private static bool __resolved_",
            "private static void __Ensure_",
            "NativeLibrary.TryGetExport"
        };

        VerifyGeneratedCode(generatedCode, lazyPatterns);

        foreach (var methodName in methodNames)
        {
            Assert.That(generatedCode, Does.Contain($"__Ensure_{methodName}_"), 
                $"Should contain lazy ensure method for: {methodName}");
        }
    }

    /// <summary>
    /// Verifies eager loading structure
    /// </summary>
    public static void VerifyEagerLoading(string generatedCode, string[] methodNames)
    {
        var eagerPatterns = new[]
        {
            "static __Impl_",
            "NativeLibrary.TryLoad",
            "NativeLibrary.TryGetExport"
        };

        VerifyGeneratedCode(generatedCode, eagerPatterns);

        // Verify that function pointers are resolved in static constructor
        foreach (var methodName in methodNames)
        {
            Assert.That(generatedCode, Does.Contain($"__fp_{methodName}"), 
                $"Should contain function pointer field for: {methodName}");
        }
    }

    /// <summary>
    /// Verifies calling convention handling
    /// </summary>
    public static void VerifyCallingConvention(string generatedCode, CallingConvention expectedConvention)
    {
        var expectedConventionString = expectedConvention switch
        {
            CallingConvention.Cdecl => "[Cdecl]",
            CallingConvention.StdCall => "[Stdcall]",
            CallingConvention.ThisCall => "[Thiscall]",
            CallingConvention.FastCall => "[Fastcall]",
            CallingConvention.Winapi => "", // Platform default, no explicit modifier
            _ => throw new ArgumentOutOfRangeException(nameof(expectedConvention), expectedConvention, null)
        };

        if (!string.IsNullOrEmpty(expectedConventionString))
        {
            Assert.That(generatedCode, Does.Contain(expectedConventionString), 
                $"Should contain calling convention: {expectedConventionString}");
        }
    }

    /// <summary>
    /// Verifies GC transition suppression
    /// </summary>
    public static void VerifyGCTransitionSuppression(string generatedCode, bool expectedSuppression)
    {
        if (expectedSuppression)
        {
            Assert.That(generatedCode, Does.Contain("[SuppressGCTransition]"), 
                "Should contain GC transition suppression");
        }
        else
        {
            Assert.That(generatedCode, Does.Not.Contain("[SuppressGCTransition]"), 
                "Should not contain GC transition suppression");
        }
    }

    /// <summary>
    /// Verifies entry point resolution with prefixes and suffixes
    /// </summary>
    public static void VerifyEntryPointResolution(string generatedCode, string methodName, string? prefix, string? suffix)
    {
        var expectedEntryPoint = $"{prefix ?? ""}{methodName}{suffix ?? ""}";
        var expectedPattern = $"NativeLibrary.TryGetExport(__lib, \"{expectedEntryPoint}\"";
        
        Assert.That(generatedCode, Does.Contain(expectedPattern), 
            $"Should contain entry point resolution for: {expectedEntryPoint}");
    }

    /// <summary>
    /// Verifies that excluded methods generate throw null stubs
    /// </summary>
    public static void VerifyExcludedMethodStub(string generatedCode, string methodName)
    {
        var expectedPattern = $"public {methodName}(";
        var throwPattern = "=> throw null;";
        
        Assert.That(generatedCode, Does.Contain(expectedPattern), 
            $"Should contain method signature for excluded method: {methodName}");
        Assert.That(generatedCode, Does.Contain(throwPattern), 
            $"Should contain throw null stub for excluded method: {methodName}");
    }
}
