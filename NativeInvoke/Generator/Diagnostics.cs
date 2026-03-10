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
}
