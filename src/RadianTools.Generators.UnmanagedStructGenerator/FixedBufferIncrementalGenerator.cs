#pragma warning disable RS1032
#pragma warning disable RS1038
#pragma warning disable RS2008

namespace RadianTools.Generators.UnmanagedStructGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[Generator(LanguageNames.CSharp)]
public sealed class FixedBufferIncrementalGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor UnmanagedTypeRule =
        new(ErrorId.UnmanagedType,
            "FixedBufferAttribute requires an unmanaged type",
            "Type '{0}' is not unmanaged. FixedBufferAttribute only supports unmanaged types.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LengthMustBePositiveRule =
        new(ErrorId.LengthMustBePositive,
            "Length must be greater than zero",
            "FixedBufferAttribute length must be greater than zero (was {0}).",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MustBePartialRule =
        new(ErrorId.MustBePartial,
            "Struct must be partial",
            "Struct '{0}' must be declared as partial to use this generator.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CharTypeWarning =
        new(WarningId.CharType,
            "Use FixedCharsAttribute instead of FixedBufferAttribute for char",
            "Type 'char' should not be used with FixedBufferAttribute. Use FixedCharsAttribute instead.",
            "Usage",
            DiagnosticSeverity.Warning,
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
                // --- 対象外はスキップ ---
                if (attr.AttributeClass?.Name is not ("FixedCharsAttribute" or "FixedBufferAttribute"))
                    continue;

                // --- FBGE003: partial 判定 ---
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

                // --- FixedCharsAttribute ---
                if (attr.AttributeClass?.Name == "FixedCharsAttribute")
                {
                    int len = (int)attr.ConstructorArguments[0].Value!;
                    spc.AddSource($"{name}_FixedChars.g.cs", GenerateFixedChars(ns, name, len));
                    continue;
                }

                // --- FixedBufferAttribute ---
                int length = (int)attr.ConstructorArguments[0].Value!;
                var typeArg = attr.ConstructorArguments[1].Value as INamedTypeSymbol;
                string elemType = typeArg?.ToDisplayString() ?? "byte";

                if (length <= 0)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        LengthMustBePositiveRule,
                        symbol.Locations.FirstOrDefault(),
                        length));
                    continue;
                }

                if (typeArg is not null && !typeArg.IsUnmanagedType)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        UnmanagedTypeRule,
                        symbol.Locations.FirstOrDefault(),
                        typeArg.ToDisplayString()));
                    continue;
                }

                if (typeArg?.SpecialType == SpecialType.System_Char)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        CharTypeWarning,
                        symbol.Locations.FirstOrDefault()));
                    continue;
                }

                spc.AddSource($"{name}_FixedBuffer.g.cs", GenerateFixedBuffer(ns, name, elemType, length));
            }
        });
    }

    private static string GenerateFixedChars(string ns, string name, int len) => $@"
namespace {ns}
{{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    partial struct {name}
    {{
        public unsafe fixed char _value[{len}];

        public Span<char> AsSpan()
        {{
            unsafe
            {{
                fixed(char* p = _value)
                {{
                    return new Span<char>(p, {len});
                }}
            }}
        }}

        public override string ToString()
        {{
            unsafe
            {{
                fixed (char* p = _value)
                {{
                    var span = new System.Span<char>(p, {len});
                    int end = span.IndexOf('\0');
                    if (end < 0) return """";
                    return new string(span.Slice(0, end));
                }}
            }}
        }}
    }}
}}
";

    private static string GenerateFixedBuffer(string ns, string name, string elemType, int len) => $@"
namespace {ns}
{{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    partial struct {name}
    {{
        public unsafe fixed {elemType} _value[{len}];

        public Span<{elemType}> AsSpan()
        {{
            fixed({elemType}* p = _value)
            {{
                return new Span<{elemType}>(p, {len});
            }}
        }}

        public override string ToString()
        {{
            unsafe
            {{
                fixed ({elemType}* p = _value)
                {{
                    var span = new System.Span<{elemType}>(p, {len});
                    var sb = new System.Text.StringBuilder({len} * 2);
                    foreach (var v in span)
                        sb.Append(v.ToString(""X""));
                    return sb.ToString();
                }}
            }}
        }}
    }}
}}
";
}

#pragma warning restore RS1032
#pragma warning restore RS1038
#pragma warning restore RS2008
