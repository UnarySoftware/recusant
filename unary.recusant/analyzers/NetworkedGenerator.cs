using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unary.Core.Analyzers
{
    public class NetworkedGenerator : CodeGenerator
    {
        public override void Generate(SourceProductionContext context, ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        }
    }
}
