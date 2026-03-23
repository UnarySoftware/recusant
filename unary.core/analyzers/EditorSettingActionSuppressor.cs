using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Unary.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EditorSettingActionSuppressor : DiagnosticSuppressor
    {
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = new[]
        {
            new SuppressionDescriptor("UNRY0001", "IDE0051", "Implicit suppression")
        }.ToImmutableArray();

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                var node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
                if (node != null)
                {
                    var model = context.GetSemanticModel(node.SyntaxTree);
                    var declaredSymbol = model.GetDeclaredSymbol(node, context.CancellationToken);
                    if (declaredSymbol.GetAttributes().Any(a => a.AttributeClass.Name.Contains("Editor")) == true)
                    {
                        context.ReportSuppression(Suppression.Create(SupportedSuppressions[0], diagnostic));
                    }
                }
            }
        }
    }
}
