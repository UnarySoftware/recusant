using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
