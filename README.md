# UnmanagedStructGenerator

C# source generators for unmanaged structs: NativeHandle wrappers and fixed-size buffers.

## ✨ Features
- **NativeHandleIncrementalGenerator**  
  Generates strongly-typed unmanaged handle structs from `[NativeHandleAttribute]`.

- **FixedBufferIncrementalGenerator**  
  Generates fixed-size buffer structs from `[FixedBufferAttribute]` or `[FixedCharsAttribute]`.

## 🚀 Getting Started

### Installation
Add the generator DLL as an analyzer reference in your project:

```xml
<ItemGroup>
  <Analyzer Include="path\to\RadianTools.Generators.UnmanagedStructGenerator.dll" />
</ItemGroup>
```

*(Alternatively, pack as a local NuGet package and install it.)*

---

## 📝 Usage Examples

### NativeHandle

**User code:**
```csharp
[NativeHandle]
public partial struct WindowHandle { }
```

**Generated code (example):**
```csharp
partial struct WindowHandle : IEquatable<WindowHandle>, IEquatable<IntPtr>
{
    private IntPtr _value;
    public static readonly WindowHandle Null = new WindowHandle(IntPtr.Zero);

    public WindowHandle() => _value = IntPtr.Zero;
    public WindowHandle(IntPtr value) => _value = value;

    public IntPtr Value => _value;
    public bool IsNull => _value == IntPtr.Zero;

    public bool Equals(WindowHandle other) => _value == other._value;
    public bool Equals(IntPtr other) => _value == other;

    public override bool Equals(object? obj) => obj is WindowHandle other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public static implicit operator IntPtr(WindowHandle h) => h._value;
    public static implicit operator WindowHandle(IntPtr v) => new WindowHandle(v);

    public static bool operator ==(WindowHandle left, WindowHandle right) => left._value == right._value;
    public static bool operator !=(WindowHandle left, WindowHandle right) => left._value != right._value;

    public override string ToString() => $"0x{_value.ToString("X")}";
}
```

---

### FixedBuffer

**User code:**
```csharp
[FixedBuffer(16, typeof(byte))]
public partial struct KeyData { }
```

**Generated code (example):**
```csharp
partial struct KeyData
{
    public unsafe fixed byte _value[16];

    public override string ToString()
    {
        unsafe
        {
            fixed (byte* p = _value)
            {
                var span = new Span<byte>(p, 16);
                var sb = new StringBuilder(32);
                foreach (var v in span)
                    sb.Append(v.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
```

---

### FixedChars

**User code:**
```csharp
[FixedChars(260)]
public partial struct CharsMaxPath { }
```

**Generated code (example):**
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
partial struct CharsMaxPath
{
    public unsafe fixed char _value[260];

    public override string ToString()
    {
        unsafe
        {
            fixed (char* p = _value)
            {
                var span = new Span<char>(p, 260);
                int end = span.IndexOf('\0');
                if (end < 0) end = span.Length;
                return new string(span.Slice(0, end));
            }
        }
    }
}
```

---

## 🧪 Requirements
- Generator project: **.NET Standard 2.0**  
  (Source generators are recommended to be built targeting `netstandard2.0`)
- Consumer project: **.NET 6.0 or later**  
  (Incremental Source Generators are supported starting with the .NET 6 SDK / C# 10)
- IDE: **Visual Studio 2022 or later**, or any environment with .NET 6 SDK support

## 📄 License
This project is licensed under the [0BSD License](LICENSE).  
Copyright (c) 2025 radian
