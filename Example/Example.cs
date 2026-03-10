global using NativeInvoke; // to import our attributes in your project

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BOOL = int; // Win32 BOOL is 4-bytes (0=false, 1=true)
using DWORD = uint; // double-word

namespace Example;

#if NET6_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")] // Optional (for clarity)
#endif
public interface IKernel32<TBool, TDWord>
  where TBool : unmanaged
  where TDWord : unmanaged
{
  [NativeImportMethod("Beep")]
  TBool Boop(TDWord frequency, TDWord duration); // generic with explicit entry point

  [NativeImportMethod(CallingConvention = CallingConvention.StdCall)]
  BOOL Boop(int frequency, int duration); // calling convention override

  BOOL Beep(TDWord frequency, TDWord duration); // no attribute

  [NativeImportMethod(null)]
  void IgnoreMe(); // should be skipped
}

internal sealed partial record Win32
{
  private const string LibName = "kernel32";

  [NativeImport(
    LibName, // Specify native library name
    Lazy = false, // Whether to use lazy or eager module loading
    CallingConvention = CallingConvention.Cdecl, // Define the default calling convention
    SymbolPrefix = "begin_", // Define common symbol prefix
    SymbolSuffix = "_end", // Define common symbol suffix
    Inherited = false // Whether to consider interface inheritance members
  )]
  //internal static partial IKernel32 Kernel32 { get; }
  internal static partial IKernel32<BOOL, DWORD> Kernel32 { get; }
}

internal static partial class Program
{
  [LibraryImport("kernel32", EntryPoint = "Beep")]
  private static partial BOOL PlayBeep(DWORD f, DWORD d);

  private static void Main()
  {
    Win32.Kernel32.Boop(750, 2000);
    PlayBeep(750, 2000);
  }
}
