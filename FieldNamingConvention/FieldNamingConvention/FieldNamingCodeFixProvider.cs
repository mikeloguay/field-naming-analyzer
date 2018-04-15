using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FieldNamingConvention
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldNamingCodeFixProvider)), Shared]
    public class FieldNamingCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Insert _ at the beginning";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FieldNamingAnalyzer.DIAGNOSTIC_ID); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the field variable identified by the diagnostic.
            var fieldVariable = root.FindToken(diagnosticSpan.Start).Parent
                .AncestorsAndSelf()
                .OfType<VariableDeclaratorSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedSolution: c => InsertInitialUnderscore(context.Document, fieldVariable, c),
                    equivalenceKey: TITLE),
                diagnostic);
        }

        private async Task<Solution> InsertInitialUnderscore(Document document,
            VariableDeclaratorSyntax fieldVariable,
            CancellationToken cancellationToken)
        {
            // Compute new name with the "_"
            var oldIdentifier = fieldVariable.Identifier;
            var newName = $"_{oldIdentifier.Text}";

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(fieldVariable, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer
                .RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken)
                .ConfigureAwait(false);

            // Return the new solution with the new type name.
            return newSolution;
        }
    }
}