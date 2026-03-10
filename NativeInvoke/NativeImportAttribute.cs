// This file is part of NativeInvoke NuGet package; https://github.com/Cheatoid/NativeInvoke
// Licensed under MIT. Copyright © Cheatoid.

using System.Runtime.InteropServices;

namespace NativeInvoke;

/// <summary>
/// Specifies that a static partial property should generate a native library import implementation.
/// The property type must be an interface that defines the native methods to import.
/// </summary>
/// <remarks>
/// <para>
/// This attribute enables source generation of native library imports using function pointers
/// and the <c>NativeLibrary</c> API, providing a type-safe and efficient way to call native functions.
/// </para>
/// <para>
/// The generated implementation creates a private sealed class that implements the interface,
/// with each method mapped to a native function pointer loaded from the specified library.
/// </para>
/// <example>
/// Basic usage:
/// <code>
/// public interface IMyLib
/// {
///     [NativeImportMethod] int Add(int a, int b);
///     [NativeImportMethod] int Subtract(int a, int b);
/// }
///
/// public static partial class Native
/// {
///     [NativeImport("mylib")]
///     public static partial IMyLib MyLib { get; }
/// }
/// </code>
/// </example>
/// <example>
/// With inherited interface members:
/// <code>
/// public interface IBase { [NativeImportMethod] void BaseMethod(); }
/// public interface IDerived : IBase { [NativeImportMethod] void DerivedMethod(); }
///
/// [NativeImport("mylib", Inherited = true)]
/// public static partial IDerived MyLib { get; }
/// // Generates implementations for both BaseMethod and DerivedMethod
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class NativeImportAttribute : Attribute
{
  /// <summary>
  /// Initializes a new instance of the <see cref="NativeImportAttribute"/> class
  /// with the specified library name.
  /// </summary>
  /// <param name="libraryName">
  /// The name or path of the native library to load.
  /// This can be a library name (e.g., "kernel32"), a short name (e.g., "mylib"),
  /// or a full path to the library file.
  /// </param>
  public NativeImportAttribute(string libraryName)
  {
    LibraryName = libraryName;
  }

  /// <summary>
  /// Gets the name or path of the native library to import functions from.
  /// </summary>
  /// <value>
  /// The library name or path specified in the constructor.
  /// This is passed to <c>NativeLibrary.TryLoad</c> for resolution.
  /// </value>
  public string LibraryName { get; }

  /// <summary>
  /// Gets or sets a value indicating whether to include inherited interface members
  /// in the generated implementation.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to generate implementations for methods defined on
  /// the interface and all interfaces it inherits from; <see langword="false"/>
  /// to generate implementations only for methods directly declared on the interface.
  /// Default is <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// When set to <see langword="true"/>, the source generator will traverse the entire
  /// interface hierarchy and generate implementations for all methods found, including
  /// those from base interfaces.
  /// </para>
  /// <para>
  /// Duplicate methods from diamond-shaped interface hierarchies are automatically
  /// deduplicated based on method signature (return type, name, and parameter types).
  /// </para>
  /// </remarks>
  public bool Inherited { get; set; } = false;

  /// <summary>
  /// Gets or sets a value indicating whether to use lazy loading for native function pointers.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to resolve function pointers on first use with thread-safe
  /// lazy initialization; <see langword="false"/> to resolve all function pointers
  /// eagerly in the static constructor. Default is <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// When <see langword="false"/> (eager loading), all function pointers are resolved
  /// when the implementation class is first accessed. This provides fail-fast behavior
  /// if any entry points are missing, but may cause unnecessary loading of unused functions.
  /// </para>
  /// <para>
  /// When <see langword="true"/> (lazy loading), each function pointer is resolved
  /// on its first invocation using a lock for thread safety. This is useful when:
  /// <list type="bullet">
  /// <item>Not all functions may be available on all platforms</item>
  /// <item>You want to minimize startup time</item>
  /// <item>You only need a subset of the functions</item>
  /// </list>
  /// </para>
  /// </remarks>
  public bool Lazy { get; set; } = false;

  /// <summary>
  /// Gets or sets the default calling convention for imported functions.
  /// </summary>
  /// <value>
  /// The calling convention to use for methods that don't specify their own.
  /// Default is <see cref="CallingConvention.Winapi"/>, which uses the platform default
  /// (<see cref="CallingConvention.StdCall"/> on Windows x86,
  /// <see cref="CallingConvention.Cdecl"/> on other platforms).
  /// </value>
  /// <remarks>
  /// <para>
  /// This serves as the default for all methods in the interface. Individual methods
  /// can override this using <see cref="NativeImportMethodAttribute.CallingConvention"/>.
  /// </para>
  /// <para>
  /// Note: Future versions may switch to using <c>CallConv*</c> types for better
  /// support of advanced calling conventions like Swift and member function calls.
  /// </para>
  /// </remarks>
  public CallingConvention CallingConvention { get; set; } = CallingConvention.Winapi;

  /// <summary>
  /// Gets or sets a value indicating whether to suppress the GC transition when
  /// calling native functions.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to suppress the runtime's transition from managed to
  /// unmanaged code, avoiding the overhead of cooperative GC mode;
  /// <see langword="false"/> to use the standard transition behavior.
  /// Default is <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Suppressing the GC transition can significantly improve performance for
  /// short-running native calls, but requires careful consideration:
  /// <list type="bullet">
  /// <item>The native function must not call back into managed code</item>
  /// <item>The native function must not block for extended periods</item>
  /// <item>The native function must not access managed objects</item>
  /// </list>
  /// </para>
  /// <para>
  /// This serves as the default for all methods in the interface. Individual methods
  /// can override this using <see cref="NativeImportMethodAttribute.SuppressGCTransition"/>.
  /// </para>
  /// </remarks>
  public bool SuppressGCTransition { get; set; } = false;

  /// <summary>
  /// Gets or sets a prefix to prepend to method names when resolving entry points.
  /// </summary>
  /// <value>
  /// A string to prepend to the method name. Default is an empty string.
  /// </value>
  /// <remarks>
  /// <para>
  /// This is useful when native functions have a consistent naming prefix that
  /// differs from the interface method names.
  /// </para>
  /// <example>
  /// <code>
  /// // Native functions: mylib_create, mylib_destroy, mylib_process
  /// [NativeImport("mylib", SymbolPrefix = "mylib_")]
  /// public static partial IMyLib MyLib { get; }
  ///
  /// public interface IMyLib
  /// {
  ///     [NativeImportMethod] void create();
  ///     [NativeImportMethod] void destroy();
  ///     [NativeImportMethod] void process();
  /// }
  /// </code>
  /// </example>
  /// </remarks>
  public string SymbolPrefix { get; set; } = "";

  /// <summary>
  /// Gets or sets a suffix to append to method names when resolving entry points.
  /// </summary>
  /// <value>
  /// A string to append to the method name. Default is an empty string.
  /// </value>
  /// <remarks>
  /// <para>
  /// This is useful when native functions have a consistent naming suffix that
  /// differs from the interface method names.
  /// </para>
  /// <example>
  /// <code>
  /// // Native functions: init_impl, cleanup_impl, process_impl
  /// [NativeImport("mylib", SymbolSuffix = "_impl")]
  /// public static partial IMyLib MyLib { get; }
  ///
  /// public interface IMyLib
  /// {
  ///     [NativeImportMethod] void init();
  ///     [NativeImportMethod] void cleanup();
  ///     [NativeImportMethod] void process();
  /// }
  /// </code>
  /// </example>
  /// </remarks>
  public string SymbolSuffix { get; set; } = "";
}

/// <summary>
/// Controls how an interface method is mapped to a native library entry point.
/// Applied to methods within an interface that is used with <see cref="NativeImportAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is optional. Methods without this attribute will be included
/// using their name as the entry point (with prefix/suffix from the parent
/// <see cref="NativeImportAttribute"/> applied).
/// </para>
/// <para>
/// Use this attribute to customize entry point resolution, specify ordinals
/// for ordinal-based exports, or exclude specific methods from generation.
/// </para>
/// <example>
/// Custom entry point:
/// <code>
/// public interface IMyLib
/// {
///     // Maps to "native_add" instead of "Add"
///     [NativeImportMethod("native_add")]
///     int Add(int a, int b);
///
///     // Maps to ordinal 42
///     [NativeImportMethod(42)]
///     void SomeFunction();
///
///     // Excluded from generation (entry point is empty)
///     [NativeImportMethod("")]
///     void NotAvailable();
///
///     // Uses default name "Multiply"
///     int Multiply(int a, int b);
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class NativeImportMethodAttribute : Attribute
{
  /// <summary>
  /// Initializes a new instance of the <see cref="NativeImportMethodAttribute"/> class
  /// that uses the method name as the entry point.
  /// </summary>
  /// <remarks>
  /// This constructor is useful when you only want to override the calling convention
  /// or GC transition behavior without changing the entry point.
  /// </remarks>
  public NativeImportMethodAttribute() { }

  /// <summary>
  /// Initializes a new instance of the <see cref="NativeImportMethodAttribute"/> class
  /// with a custom entry point name.
  /// </summary>
  /// <param name="entryPoint">
  /// The name of the native function to bind to. Pass an empty or whitespace string
  /// to exclude this method from the generated implementation.
  /// </param>
  public NativeImportMethodAttribute(string? entryPoint)
  {
    Ordinal = null;
    EntryPoint = entryPoint;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="NativeImportMethodAttribute"/> class
  /// with an ordinal-based entry point.
  /// </summary>
  /// <param name="ordinal">
  /// The ordinal number of the exported function. This is useful for libraries
  /// that export functions by ordinal rather than by name.
  /// </param>
  /// <remarks>
  /// Ordinal-based resolution is more efficient than name-based resolution and
  /// works even when the function is not exported by name. Note that ordinal
  /// values may differ across library versions, so use with caution.
  /// </remarks>
  public NativeImportMethodAttribute(int ordinal)
  {
    EntryPoint = null;
    Ordinal = ordinal;
  }

  /// <summary>
  /// Gets the custom entry point name, if specified.
  /// </summary>
  /// <value>
  /// The entry point name if the attribute was constructed with a string;
  /// <see langword="null"/> if an ordinal was used or the parameterless constructor was used.
  /// </value>
  /// <remarks>
  /// When this property is an empty or whitespace string, the method is excluded
  /// from the generated implementation.
  /// </remarks>
  public string? EntryPoint { get; }

  /// <summary>
  /// Gets the ordinal number of the exported function, if specified.
  /// </summary>
  /// <value>
  /// The ordinal number if the attribute was constructed with an integer;
  /// <see langword="null"/> if an entry point name was used or the parameterless constructor was used.
  /// </value>
  /// <remarks>
  /// Ordinal-based imports are resolved using <c>NativeLibrary.TryGetExport</c>.
  /// On Windows, ordinals are typically 16-bit values (ushort), but this property
  /// uses <see cref="int"/> for cross-platform compatibility and future-proofing.
  /// </remarks>
  public int? Ordinal { get; }

  /// <summary>
  /// Gets or sets the calling convention for this specific method.
  /// </summary>
  /// <value>
  /// The calling convention to use. Default is <see cref="CallingConvention.Winapi"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// When set, this overrides the default calling convention specified in
  /// <see cref="NativeImportAttribute.CallingConvention"/> for this method only.
  /// </para>
  /// <para>
  /// Note: Future versions may switch to using <c>CallConv*</c> types for better
  /// support of advanced calling conventions like Swift and member function calls.
  /// </para>
  /// </remarks>
  public CallingConvention CallingConvention { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether to suppress the GC transition for this specific method.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to suppress the GC transition; <see langword="false"/>
  /// to use the default behavior. Default is <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// When set, this overrides the default setting in
  /// <see cref="NativeImportAttribute.SuppressGCTransition"/> for this method only.
  /// See <see cref="NativeImportAttribute.SuppressGCTransition"/> for important
  /// safety considerations when suppressing GC transitions.
  /// </remarks>
  public bool SuppressGCTransition { get; set; }
}
