namespace NativeInvoke;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class NativeImportAttribute : Attribute
{
  public NativeImportAttribute(string libraryName)
  {
    LibraryName = libraryName;
  }

  public string LibraryName { get; }

  public CallingConvention CallingConvention { get; set; } = CallingConvention.Winapi; // TODO/FIXME: switch to typeof(CallConv*)

  //public bool SetLastError { get; set; }

  public bool Lazy { get; set; } = false;

  public string? SymbolPrefix { get; set; } // TODO
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class NativeImportMethodAttribute : Attribute
{
  public NativeImportMethodAttribute()
  {
  }

  public NativeImportMethodAttribute(string? entryPoint)
  {
    Ordinal = null;
    EntryPoint = entryPoint;
  }

  public NativeImportMethodAttribute(int ordinal)
  {
    EntryPoint = null;
    Ordinal = (ushort)ordinal;
  }

  public string? EntryPoint { get; }

  public ushort? Ordinal { get; } // TODO

  public CallingConvention CallingConvention { get; set; } // TODO/FIXME: switch to typeof(CallConv*)

  //public bool SetLastError { get; set; }
}
