# RadianTools.Generators.UnmanagedStructGenerator

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
