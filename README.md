[<img width="100%" alt="NativeInvoke banner" src="https://raw.github.com/Cheatoid/NativeInvoke/main/NativeInvoke.jpg" />](https://github.com/Cheatoid/NativeInvoke) [<img src="https://capsule-render.vercel.app/api?type=waving&color=0:00f7ff,100:ff00e6&height=150&section=header&text=🌟+ᑎᗩTIᐯEIᑎᐯOKE+✨&fontSize=40&fontColor=ffffff" width="100%" />](https://github.com/Cheatoid/NativeInvoke)

<div align="center">

[![Stars](https://img.shields.io/github/stars/Cheatoid/NativeInvoke?style=flat-square&color=00f0ff&labelColor=001f2b)](https://github.com/Cheatoid/NativeInvoke/stargazers) 
[![Issues](https://img.shields.io/github/issues/Cheatoid/NativeInvoke?style=flat-square&color=397f14&labelColor=002b00)](https://github.com/Cheatoid/NativeInvoke/issues) 
[![License](https://img.shields.io/github/license/Cheatoid/NativeInvoke?style=flat-square&color=ff00e6&labelColor=2b001f)](https://github.com/Cheatoid/NativeInvoke/blob/main/LICENSE) 
[![Discord](https://img.shields.io/badge/Discord-Join-2c2f33?style=flat-square&logo=discord&logoColor=white&labelColor=5865f2)](https://discord.gg/FVCeYxwWtB)
[![NativeInvoke](https://readme-typing-svg.herokuapp.com?font=Fira+Code&duration=5000&pause=2500&size=32&color=00F7FF&center=true&vCenter=true&width=800&lines=hello,+stranger;welcome+to+NativeInvoke+codebase;made+with+%E2%9D%A4%EF%B8%8F+by+Cheatoid)](https://github.com/Cheatoid/NativeInvoke)
</div>

### *High-performance, source-generated P/Invoke*

NativeInvoke is a modern, zero-overhead, generics-capable P/Invoke generator for .NET.  
It uses Roslyn source generation to enforce **blittable**, **function-pointer based**, **lazy-loaded** native bindings - without the runtime overhead of `DllImport`.

You write clean interfaces.  
NativeInvoke generates the unsafe bits for you at compile-time.  
- Cross-platform and AOT/JIT-friendly.
- No `DllImport`.
- No delegate allocation (no pinning).
- No runtime dependencies.
- No marshalling.
- No reflection.
- No dynamic codegen (dynamic IL).
- Just pure compile-time generation glue.

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 🚀 Quick Installation

Install the NuGet package:
```bash
dotnet add package NativeInvoke
```

Or edit your `.csproj` to always stay up-to-date (followed by `dotnet restore --no-cache`):
```xml
<ItemGroup>
    <PackageReference Include="NativeInvoke" Version="*"/>
</ItemGroup>
```

How floating versions work:
- `*-*`: Latest version including pre-releases (e.g., `1.1.0-beta.1`, `2.0.0-alpha.2`)
- `*`: Latest stable version only
- `1.*`: Latest stable version with major version 1
- `1.2.*`: Latest stable version with major.minor 1.2

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 🧠 Why NativeInvoke?

| Feature                   | Benefit                                 |
|---------------------------|-----------------------------------------|
| **Source-generated**      | Zero runtime overhead                   |
| **Function pointers**     | Faster than `DllImport`                 |
| **Lazy-loading support**  | Load symbols/functions only when needed |
| **Interface-based**       | Fully mockable for testing              |
| **Generics support**      | Use generics in P/Invoke                |
| **No static pollution**   | Clean public API surface                |
| **.NET 9 `Lock` support** | Modern, allocation-free synchronization |

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 🛠 Requirements

- C# 14 / .NET 9 or later
- Unsafe code enabled (`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`)
- Roslyn source generators enabled (default in SDK-style projects)

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## ✨ Example Usage

> 📚 [**See Attribute docs**](https://github.com/Cheatoid/NativeInvoke/blob/main/NativeInvoke/NativeImportAttribute.cs).  

Checkout the [**full Example project**](https://github.com/Cheatoid/NativeInvoke/tree/main/Example) for more!  

<details>

<summary>Click here to toggle an example for <a href="https://learn.microsoft.com/en-us/windows/win32/api/utilapiset/nf-utilapiset-beep">playing a <i>beep</i> sound on Windows platform</a> (a.k.a. <code>System.Console.Beep</code>)</summary>

### 1. Define your native interface

```csharp
global using NativeInvoke; // Import our attributes in your project
global using NIMA = NativeInvoke.NativeImportMethodAttribute;

using BOOL = int; // Win32 BOOL is 4-bytes (0=false, 1=true)
using DWORD = uint; // double-word

#if NET6_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")] // Optional (for clarity)
#endif
public interface IKernel32<TBool> // Generics are supported!
  where TBool : unmanaged
{
  [NIMA("Beep")] // Optional; Use this attribute if you want to load a different name/ordinal,
                 // or override a calling convention per function (defaults to platform-specific).
  TBool Boop(DWORD frequency, DWORD duration);

  [NIMA(null)] // Use null or empty string to skip generation; could also omit this and set ExplicitOnly=true
  void IgnoreMe();
}
```

### 2. Expose it via a `static partial` property

The property can be nested anywhere you want (class/struct/interface/record), and you can use any accessibility level you need - the generator will match your declaration.

```csharp
public static partial class Win32
{
  private const string kernel32 = "kernel32";
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
  public static partial IKernel32<BOOL> Kernel32 { get; }
}
```

### 3. Call it like a normal .NET API

```csharp
Win32.Kernel32.Boop(600u, 300u);
```

Under the hood, NativeInvoke generates:

- A nested sealed `__Impl` class implementing your (generic) interface
- Static (readonly) function pointer fields (`delegate* unmanaged`)
- Lazy or eager symbol resolution (`NativeLibrary`)
- A clean property implementation using the `field` keyword
- Thread-safe lazy initialization using .NET 9 `Lock` type

All without touching your container type.

</details>

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 💡 Future/Experiments (ToDo list)

- [ ] Support C# 9 / .NET 5 and later via `#if`; current source generator is relying on C# 14 features and .NET 9 API
- [ ] Add support for loading symbol from numeric ordinal
- [x] ~~Implement default symbol name prefix and suffix~~
- [x] ~~Add `EnforceBlittable` and `ExplicitOnly` flags~~
- [ ] Switch to `[UnmanagedCallConv]`/`typeof(CallConv*)` for future-proofed calling conventions (MemberFunction, Swift, etc.)
- [x] ~~Format generated code using Roslyn, or use `IndentedTextWriter` for source-code generation~~
- [x] ~~Append `Guid` to generated fields (to prevent name collisions for overloaded functions)~~
- [ ] Make unit tests
- [ ] Auto-generate proper page for docs and examples (maybe use GitHub io page or wiki)
- [ ] Explore micro-optimization: IL weaver via `Fody`, replace interface dispatch and `DllImport` calls with `calli`

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 🙏 Contributing

PRs, issues, and ideas are welcome.  
NativeInvoke is built for developers who want **maximum performance** without sacrificing **clean API design**.

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 💖 Support

If you like this or you are using this in your project, consider:
- [Becoming a ⭐](https://github.com/Cheatoid/NativeInvoke/stargazers) 🤩
- Spreading the word

[<img width="100%" alt="separator" src="https://raw.github.com/Cheatoid/gh_assets/_/images/rainbow-separator.png" />](https://github.com/Cheatoid/NativeInvoke)

## 📄 License

[MIT](https://github.com/Cheatoid/NativeInvoke/blob/main/LICENSE) - do whatever you want, just don't blame me if you `calli` into oblivion.
