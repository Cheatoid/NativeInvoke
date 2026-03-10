# 🌟 **NativeInvoke**

### *High-performance, source-generated P/Invoke*

NativeInvoke is a modern, zero-overhead, generics-capable P/Invoke generator for .NET.  
It uses Roslyn source generation to enforce **blittable**, **function-pointer based**, **lazy-loaded** native bindings - without the runtime overhead of `DllImport`.

You write clean interfaces.  
NativeInvoke generates the unsafe bits.

---

## 🚀 Quick Installation

Install the NuGet package:

```bash
dotnet add package NativeInvoke
```

Or edit your `.csproj` (followed by `dotnet restore`):

```xml
<ItemGroup>
    <PackageReference Include="NativeInvoke" Version="1.1.0"/>
</ItemGroup>
```

---

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

- Cross-platform and AOT/JIT-friendly.
- No `DllImport`.
- No delegate allocation.
- No runtime dependencies.
- No marshalling.
- No reflection.
- No dynamic codegen (dynamic IL).
- Just pure compile-time generation glue.

---

## 🛠 Requirements

- C# 14 / .NET 9 or later
- Unsafe code enabled (`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`)
- Roslyn source generators enabled (default in SDK-style projects)

---

## ✨ Example Usage

Slightly verbose example to play a [beep sound on Windows](https://learn.microsoft.com/en-us/windows/win32/api/utilapiset/nf-utilapiset-beep) (without `Console.Beep`):

### 1. Define your native interface

```csharp
global using NativeInvoke; // Import our attributes in your project

using BOOL = int; // Win32 BOOL is 4-bytes (0=false, 1=true)
using DWORD = uint; // double-word

#if NET6_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")] // Optional (for clarity)
#endif
public interface IKernel32<TBool> // Generics are supported!
  where TBool : unmanaged
{
  [NativeImportMethod("Beep")] // Optional; Use this attribute if you want to load a different name/ordinal,
                               // or override a calling convention per function (defaults to platform-specific).
  TBool Boop(DWORD frequency, DWORD duration);

  [NativeImportMethod(null)] // Use null or empty string to skip generation.
  void IgnoreMe();
}
```

### 2. Expose it via a `static partial` property

The property can be nested anywhere you want (class/struct/interface/record), and you can use any accessibility level you need - the generator will match your declaration.

```csharp
public static partial class Win32
{
  // Specify native library name.
  // Optionally set the default calling convention, name prefix/suffix, or whether to use eager/lazy loading.
  [NativeImport("kernel32", Lazy = true)]
  public static partial IKernel32<BOOL> Kernel32 { get; }
}
```

### 3. Call it like a normal .NET API

See [Example project](/Example) for more.

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

---

## 💡 Future/Experiments (ToDo list)

- [ ] Support C# 9 / .NET 5 and later via `#if`; current source generator is relying on C# 14 features and .NET 9 API
- [ ] Add support for loading symbol from numeric ordinal (ushort)
- [x] ~~Implement default symbol name prefix and suffix~~
- [ ] Switch to `typeof(CallConv*)` for future-proofed calling conventions (MemberFunction, Swift, etc.)
- [ ] Use `IndentedTextWriter` for source-code generation
- [x] ~~Append `Guid` to generated fields (to prevent name collisions for overloaded functions)~~
- [ ] Make unit tests
- [ ] Explore micro-optimization: IL weaver via `Fody`, replace interface dispatch and `DllImport` calls with `calli`

---

## 🙏 Contributing

PRs, issues, and ideas are welcome.  
NativeInvoke is built for developers who want **maximum performance** without sacrificing **clean API design**.

---

## 💖 Support

If you like this or you are using this in your project, consider:
- [Becoming a ⭐](https://github.com/Cheatoid/NativeInvoke/stargazers) 🤩
- Spreading the word

---

## 📄 License

MIT - do whatever you want, just don't blame me if you `calli` into oblivion.
