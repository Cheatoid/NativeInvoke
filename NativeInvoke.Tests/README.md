# NativeInvoke.Tests

This is a comprehensive NUnit-based test project for the NativeInvoke source generator and NativeImportAttribute. The test suite provides thorough coverage of all functionality, edge cases, and error scenarios.

## Test Structure

The test project is organized into the following categories:

### **Attribute Validation**
- `NativeImportAttributeTests.cs` - Tests for NativeImportAttribute constructor, properties, and validation
- `NativeImportMethodAttributeTests.cs` - Tests for NativeImportMethodAttribute constructor, properties, and validation

### **Code Generation**
- `BasicGenerationTests.cs` - Tests for fundamental code generation scenarios
- `AdvancedGenerationTests.cs` - Tests for advanced features like lazy loading, custom calling conventions, etc.

### **Diagnostics**
- `ErrorDiagnosticTests.cs` - Tests for error diagnostics (NINVK001-NINVK007)
- `WarningDiagnosticTests.cs` - Tests for warning diagnostics

### **Edge Cases**
- `EdgeCaseTests.cs` - Tests for complex scenarios like overloaded methods, keyword parameters, etc.
- `BlittableTypeTests.cs` - Comprehensive tests for blittable type validation

### **Integration**
- `EndToEndTests.cs` - Real-world integration tests and complex scenarios

### **Performance**
- `PerformanceTests.cs` - Performance tests for large interfaces and complex scenarios

### **Helpers**
- `SourceGeneratorTestHelpers.cs` - Utility classes for testing source generators
- `GeneratedCodeVerifier.cs` - Helper methods for verifying generated code

## Test Coverage

### ✅ **NativeImportAttribute Features**
- Constructor validation with library names
- All property setters and getters
- Default values
- Calling conventions (Winapi, Cdecl, StdCall, ThisCall, FastCall)
- Symbol prefix/suffix handling
- Lazy vs eager loading
- Blittable enforcement
- Explicit-only mode
- Interface inheritance
- GC transition suppression

### ✅ **NativeImportMethodAttribute Features**
- All constructors (parameterless, string entry point, ordinal)
- Property overrides (calling convention, GC transition, blittable enforcement)
- Entry point resolution
- Method exclusion

### ✅ **Code Generation Scenarios**
- Basic interface method generation
- Lazy and eager loading
- Custom calling conventions
- GC transition suppression
- Symbol prefix/suffix resolution
- Interface inheritance
- Explicit vs implicit method inclusion
- Method exclusion
- Nested types and namespaces
- Multiple properties

### ✅ **Error Diagnostics (NINVK001-NINVK007)**
- NINVK001: Type must be partial
- NINVK002: Property must be static partial
- NINVK003: Property type must be interface
- NINVK004: Non-blittable signature
- NINVK005: Empty interface (warning)
- NINVK006: Missing library name
- NINVK007: Invalid attribute argument (warning)

### ✅ **Blittable Type Validation**
- Primitive types (int, float, double, etc.)
- Enum types
- Pointer types
- Function pointer types
- Blittable structs
- Non-blittable types (strings, objects, arrays)
- Method-level override of global settings

### ✅ **Edge Cases**
- Overloaded methods
- Generic methods
- Ref/out/in parameters
- Keyword parameter names with escaping
- Long method names
- Special characters in names
- Diamond inheritance patterns
- Nested interfaces and classes

### ✅ **Performance Scenarios**
- Large interfaces (100+ methods)
- Multiple properties (50+ interfaces)
- Deep inheritance hierarchies
- Complex method signatures
- Mixed configuration properties
- Consistent repeated generation

## Running Tests

### Prerequisites
- .NET 10.0 SDK
- NUnit Test Adapter (included in project)

### Command Line
```bash
dotnet test NativeInvoke.Tests.csproj
```

### Visual Studio
1. Open the solution in Visual Studio
2. Build the solution
3. Open Test Explorer
4. Run all tests or select specific tests

### Specific Test Categories
```bash
# Run only attribute validation tests
dotnet test --filter "TestCategory=AttributeValidation"

# Run only code generation tests
dotnet test --filter "TestCategory=CodeGeneration"

# Run only diagnostic tests
dotnet test --filter "TestCategory=Diagnostics"

# Run only performance tests
dotnet test --filter "TestCategory=Performance"
```

## Test Architecture

### SourceGeneratorTestHelpers
Provides utilities for:
- Creating compilations with source code
- Running incremental generators
- Extracting generated sources
- Getting diagnostics

### GeneratedCodeVerifier
Provides methods for:
- Verifying implementation structure
- Checking method implementations
- Validating calling conventions
- Confirming lazy/eager loading patterns
- Checking entry point resolution
- Verifying excluded method stubs

## Key Features Tested

### **Incremental Generation**
- Tests verify that the generator works correctly as an incremental generator
- Multiple runs produce consistent results
- Performance is maintained for large inputs

### **Robust Error Handling**
- All error scenarios are tested with appropriate diagnostics
- Invalid attribute arguments are handled gracefully
- Type validation works correctly

### **Advanced Generator Features**
- Function pointer generation
- Thread-safe lazy loading
- Proper namespace handling
- Nested type support
- Interface inheritance deduplication

### **Real-World Scenarios**
- Windows API patterns (kernel32, user32, etc.)
- Complex struct marshaling
- Multiple library configurations
- Mixed calling conventions

## Contributing

When adding new tests:

1. **Use descriptive test names** that clearly indicate what is being tested
2. **Follow the Arrange-Act-Assert pattern** consistently
3. **Use helper methods** for common verification patterns
4. **Add performance tests** for features that might impact generation speed
5. **Test both success and failure scenarios** for new functionality
6. **Verify generated code structure** using GeneratedCodeVerifier methods

## Future Enhancements

Potential areas for additional testing:
- Cross-platform specific scenarios
- AOT compilation scenarios
- More complex struct layouts
- Function pointer edge cases
- Unicode/encoding scenarios
- Large-scale integration tests

## Notes

- Tests are designed to be fast and reliable
- No external dependencies are required
- All tests run in-memory without file system access
- Performance tests include reasonable time assertions
- The test suite provides comprehensive coverage for regression testing
