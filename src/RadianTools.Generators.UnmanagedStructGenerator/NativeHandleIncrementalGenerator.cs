#pragma warning disable RS1032
#pragma warning disable RS1038
#pragma warning disable RS2008

namespace RadianTools.Generators.UnmanagedStructGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[Generator(LanguageNames.CSharp)]
public sealed class NativeHandleIncrementalGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor MustBePartialRule =
        new(ErrorId.MustBePartial,
            "Struct must be partial",
            "Struct '{0}' must be declared as partial to use NativeHandleAttribute.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is StructDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    var decl = (StructDeclarationSyntax)ctx.Node;
                    return ctx.SemanticModel.GetDeclaredSymbol(decl) as INamedTypeSymbol;
                })
            .Where(symbol => symbol is not null);

        context.RegisterSourceOutput(structDeclarations, (spc, symbol) =>
        {
            foreach (var attr in symbol!.GetAttributes())
            {
                if (attr.AttributeClass?.Name is not "NativeHandleAttribute")
                    continue;

                // --- partial 判定 ---
                if (symbol.DeclaringSyntaxReferences
                          .Select(r => r.GetSyntax())
                          .OfType<StructDeclarationSyntax>()
                          .Any(s => !s.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        MustBePartialRule,
                        symbol.Locations.FirstOrDefault(),
                        symbol.Name));
                    continue;
                }

                var ns = symbol.ContainingNamespace.ToDisplayString();
                var name = symbol.Name;

                // BaseTypeName を取得（引数なしなら IntPtr）
                string baseTypeName = "global::System.IntPtr";
                if (attr.ConstructorArguments.Length == 1 &&
                    attr.ConstructorArguments[0].Value is string s &&
                    !string.IsNullOrWhiteSpace(s))
                {
                    baseTypeName = s;
                }

                bool isPointer = baseTypeName.Contains("*");

                spc.AddSource($"{name}_NativeHandle.g.cs",
                    GenerateNativeHandle(ns, name, baseTypeName, isPointer));
            }
        });
    }

    private static string GenerateNativeHandle(
        string ns,
        string name,
        string baseTypeName,
        bool isPointer) => $@"
#nullable enable
namespace {ns}
{{
    using System;

    partial struct {name} : IEquatable<{name}>, IEquatable<IntPtr>
    {{
        private IntPtr _value;
        public static readonly {name} Null = new {name}(IntPtr.Zero);

        public {name}() => _value = IntPtr.Zero;
        public {name}(IntPtr value) => _value = value;{(isPointer ? $@"

        public unsafe {name}({baseTypeName} value) => _value = (IntPtr)value;" : "")}

        public IntPtr Value => _value;
        public bool IsNull => _value == IntPtr.Zero;

        public bool Equals({name} other) => _value == other._value;
        public bool Equals(IntPtr other) => _value == other;

        public override bool Equals(object? obj) => obj is {name} other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static implicit operator IntPtr({name} h) => h._value;
        public static implicit operator {name}(IntPtr v) => new {name}(v);{(isPointer ? $@"

        public static unsafe implicit operator {baseTypeName}({name} h) => ({baseTypeName})h._value;
        public static unsafe implicit operator {name}({baseTypeName} v) => new {name}((IntPtr)v);" : "")}

        public static bool operator ==({name} left, {name} right) => left._value == right._value;
        public static bool operator !=({name} left, {name} right) => left._value != right._value;

        public override string ToString() => $""0x{{_value.ToString(""X"")}}"";
    }}
}}
";
}

#pragma warning restore RS1032
#pragma warning restore RS1038
#pragma warning restore RS2008
