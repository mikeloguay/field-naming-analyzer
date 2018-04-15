using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FieldNamingConvention
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FieldNamingAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "FieldNamingConvention";
        private const string CATEGORY = "Naming";

        private static readonly LocalizableString Title = "Field names should start with '_'.";
        private static readonly LocalizableString MessageFormat = "Field name '{0}' does not start with '_'";
        private static readonly LocalizableString Description = "Field name does not follow the dotNetMalaga naming conventions";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            Title,
            MessageFormat,
            CATEGORY,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeFields, SymbolKind.Field);
        }

        private static void AnalyzeFields(SymbolAnalysisContext context)
        {
            var fieldSymbol = context.Symbol;


            if (!string.IsNullOrWhiteSpace(fieldSymbol.Name) && !fieldSymbol.Name.StartsWith("_"))
            {
                var diagnostic = Diagnostic.Create(Rule, fieldSymbol.Locations[0], fieldSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
