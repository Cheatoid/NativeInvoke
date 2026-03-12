global using NativeInvoke; // Import our attributes in your project
global using NIMA = NativeInvoke.NativeImportMethodAttribute;

// Alias a few common Win32 types for our example (zero marshalling):
global using BOOL = int; // Win32 BOOL is 4-bytes (0=false, 1=true)
global using DWORD = uint; // double-word
global using UINT = uint;
global using HWND = nint; // window handle
global using unsafe LPCSTR = sbyte*; // ANSI C string
global using unsafe LPCWSTR = char*; // Wide/Unicode string; alternatively, ushort*

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Example;

#region kernel32

[SupportedOSPlatform("windows")] // Optional (for clarity)
public interface IKernel32<TBool, TDWord> // Generics are supported!
  where TBool : unmanaged
  where TDWord : unmanaged
{
  [NIMA("Beep")]
  TBool Boop(TDWord frequency, TDWord duration); // generic with explicit entry point (ignores SymbolPrefix/Suffix)

  BOOL Beep(TDWord frequency, TDWord duration); // overload without attribute, resolved using method's name (respects SymbolPrefix/Suffix)

  [NIMA(null)] // use null or empty string to skip generation
  void IgnoreMe();
}

[SupportedOSPlatform("windows")] // Optional (for clarity)
public interface IKernel
{
  [NIMA(
    CallingConvention = CallingConvention.StdCall // explicit calling convention overrides the default
  )]
  BOOL Beep(int frequency, int duration); // resolved using method's name as entry point

  void SkipMe(); // this should be ignored because ExplicitOnly=true
}

#endregion

#region user32

[SupportedOSPlatform("windows")] // Optional (for clarity)
public unsafe interface IUser32<TAnsiChar, TWideChar>
  where TAnsiChar : unmanaged
  where TWideChar : unmanaged
{
  //[NIMA(EnforceBlittable = false)] // TODO/FIXME: ReadOnlySpan (ref struct) is treated as non-blittable
  int MessageBoxA(HWND hWnd, ReadOnlySpan<byte> lpText, ReadOnlySpan<byte> lpCaption, UINT uType)
  {
    //fixed (byte* textPtr = lpText, captionPtr = lpCaption)
    {
      var textPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(lpText));
      var captionPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(lpCaption));
      return MessageBoxA(hWnd, (TAnsiChar*)textPtr, (TAnsiChar*)captionPtr, uType);
    }
  }
  int MessageBoxA(HWND hWnd, TAnsiChar* lpText, TAnsiChar* lpCaption, UINT uType);
  //[NIMA(EnforceBlittable = false)] // TODO/FIXME: ReadOnlySpan (ref struct) is treated as non-blittable
  int MessageBoxW(HWND hWnd, ReadOnlySpan<TWideChar> lpText, ReadOnlySpan<TWideChar> lpCaption, UINT uType)
  {
    //fixed (void* textPtr = lpText, captionPtr = lpCaption)
    {
      var textPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(lpText));
      var captionPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(lpCaption));
      return MessageBox(hWnd, (TWideChar*)textPtr, (TWideChar*)captionPtr, uType);
    }
  }
  [NIMA("MessageBoxW")]
  int MessageBox(HWND hWnd, TWideChar* lpText, TWideChar* lpCaption, UINT uType);
}

#endregion

internal sealed partial class Win32 // Container can be class/struct/interface/record (nesting is also supported)
{
  private const string kernel32 = "kernel32", user32 = "user32";

  // Made slightly verbose, for easier customization and demonstration purposes...

  #region kernel32

  [NativeImport(
    kernel32 // Specify native library name
    , EnforceBlittable = true // Whether to enforce blittable type validation (applies to all methods, can be overriden per-method)
    , ExplicitOnly = false // Whether only methods explicitly marked with NIMA should be considered
    , Inherited = true // Whether to consider inherited interface methods
    , Lazy = false // Whether to use lazy or eager module loading
    , CallingConvention = CallingConvention.StdCall // Define the default calling convention (default is platform-specific, applies to all methods, can be overriden per-method)
    , SuppressGCTransition = false // Whether to suppress the GC transition (applies to all methods, can be overriden per-method)
    , SymbolPrefix = "" // Define common prefix (prepended to method name unless using explicit entry point)
    , SymbolSuffix = "" // Define common suffix (appended to method name unless using explicit entry point)
  )]
  internal static partial IKernel32<BOOL, DWORD> Kernel32 { get; }

  [NativeImport(
    kernel32 // Specify native library name
             //, EnforceBlittable = true // Whether to enforce blittable type validation (applies to all methods, can be overriden per-method)
    , ExplicitOnly = true // Whether only methods explicitly marked with NIMA should be considered
    , Inherited = false // Whether to consider inherited interface methods
    , Lazy = true // Whether to use lazy or eager module loading
  )]
  internal static partial IKernel Kernel { get; }

  #endregion

  #region user32

  [NativeImport(
    user32 // Specify native library name
    , Lazy = true // Whether to use lazy or eager module loading
    , CallingConvention = CallingConvention.StdCall // Define the default calling convention
  )]
  internal static partial IUser32<sbyte, char> User32 { get; }

  #endregion
}

[SupportedOSPlatform("windows")] // Optional (for clarity)
internal static unsafe partial class Program
{
  #region How you would usually do it... (the standard way, same as DllImport, this looks ugly)

  [LibraryImport("kernel32", EntryPoint = "Beep")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
  private static partial BOOL PlayBeep(DWORD frequency, DWORD duration);

  [LibraryImport("user32", EntryPoint = "MessageBoxA")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
  private static partial int MessageBoxA(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType);

  [LibraryImport("user32", EntryPoint = "MessageBoxW")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
  private static partial int MessageBoxW(HWND hWnd, LPCWSTR lpText, LPCWSTR lpCaption, UINT uType);

  [LibraryImport("user32", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
  private static partial int MessageBox(HWND hWnd, string lpText, string lpCaption, UINT uType);

  #endregion

  private static void Main()
  {
    if (!OperatingSystem.IsWindows()) throw new NotSupportedException("This example is for Windows-only");

    Debugger.Launch();

    #region NativeInvoke

    Win32.Kernel32.Boop(500u, 1000u);
    Win32.Kernel32.Beep(600, 1000); // included because Inherited is true
    Win32.Kernel32.Beep(700u, 1000u);

    const UINT MB_TOPMOST = 0x40000U;

    Win32.User32.MessageBoxA(0, "No pinning, no copying, no marshalling, no allocation via UTF-8 literal strings - ANSI"u8, "NativeInvoke"u8, MB_TOPMOST); // C# 11 (UTF-8 string literals)

    Win32.User32.MessageBoxW(0, "No pinning, no copying, no marshalling, no allocation via C# (UTF-16) string literals - Unicode", "NativeInvoke", MB_TOPMOST); // NOTE: .AsSpan() is redundant since C# 14

    fixed (LPCWSTR text = "The only allocations here are the string literals, which are stored in metadata and interned; Pinning example, Unicode", caption = "NativeInvoke")
    {
      Win32.User32.MessageBox(0, text, caption, MB_TOPMOST);
    }

    #endregion NativeInvoke

    #region LibraryImport

    PlayBeep(800u, 1000u);

    MessageBox(0, "Zero allocation with pinning example, Unicode", "LibraryImport", MB_TOPMOST);

    #endregion LibraryImport

    Console.ReadKey(true);
  }
}
