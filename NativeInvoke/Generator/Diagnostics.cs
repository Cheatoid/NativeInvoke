namespace NativeInvoke.Generator;

internal static partial class Diagnostics
{
  public static readonly DiagnosticDescriptor TypeMustBePartial = new(
    id: "NINVK001",
    title: "Containing type must be static partial class",
    messageFormat: "Type '{0}' must be a partial to use [NativeImport] on a property",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor PropertyMustBeStaticPartial = new(
    id: "NINVK002",
    title: "Property must be static partial",
    messageFormat: "Property '{0}' must be declared as 'static partial' to use [NativeImport]",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor PropertyTypeMustBeInterface = new(
    id: "NINVK003",
    title: "Property type must be an interface",
    messageFormat: "Property '{0}' must have an interface type to use [NativeImport]",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor NonBlittableSignature = new(
    id: "NINVK004",
    title: "Non-blittable signature",
    messageFormat: "Method '{0}' has a non-blittable signature and cannot be generated",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor EmptyInterface = new(
    id: "NINVK005",
    title: "Interface has no valid methods",
    messageFormat: "Interface '{0}' has no valid methods to generate (ensure the interface contains at least one method)",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor MissingLibraryName = new(
    id: "NINVK006",
    title: "Missing library name",
    messageFormat: "[NativeImport] attribute requires a library name (provide a valid library name via the constructor parameter)",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor InvalidAttributeArgument = new(
    id: "NINVK007",
    title: "Invalid attribute argument",
    messageFormat: "Invalid value for '{0}' parameter in [NativeImport] attribute (expected {1}, but received an incompatible value)",
    category: "NativeInvoke",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
}
