#pragma warning disable RS1032
#pragma warning disable RS1038
#pragma warning disable RS2008

namespace RadianTools.Generators.UnmanagedStructGenerator;

using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public sealed class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            // FixedCharsAttribute
            ctx.AddSource("FixedCharsAttribute.g.cs", @"
namespace RadianTools.Generators.UnmanagedStructGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = false)]
    internal sealed class FixedCharsAttribute : System.Attribute
    {
        public int Length { get; }
        public FixedCharsAttribute(int length) => Length = length;
    }
}");

            // FixedBufferAttribute
            ctx.AddSource("FixedBufferAttribute.g.cs", @"
namespace RadianTools.Generators.UnmanagedStructGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = false)]
    internal sealed class FixedBufferAttribute : System.Attribute
    {
        public int Length { get; }
        public System.Type ElementType { get; }

        public FixedBufferAttribute(int length, System.Type elementType)
        {
            Length = length;
            ElementType = elementType;
        }
    }
}");

            // NativeHandleAttribute
            ctx.AddSource("NativeHandleAttribute.g.cs", @"
namespace RadianTools.Generators.UnmanagedStructGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = false)]
    internal sealed class NativeHandleAttribute : System.Attribute
    {
        public string BaseTypeName { get; }

        /// <summary>デフォルトは IntPtr ベース</summary>
        public NativeHandleAttribute()
        {
            BaseTypeName = ""global::System.IntPtr"";
        }

        /// <summary>
        /// ベース型の完全修飾名を文字列で指定してください。
        /// 例: ""global::System.IntPtr"" または ""global::YourNamespace.ITEMIDLIST*""
        /// </summary>
        public NativeHandleAttribute(string baseTypeName)
        {
            BaseTypeName = baseTypeName;
        }
    }
}");
        });
    }
}

#pragma warning restore RS1032
#pragma warning restore RS1038
#pragma warning restore RS2008
