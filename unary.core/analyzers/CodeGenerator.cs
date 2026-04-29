using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Unary.Core.Analyzers
{
    public abstract class CodeGenerator
    {
        public abstract void Generate(SourceProductionContext context, ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel);

        public string GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            var parent = classDeclaration.Parent;
            while (parent != null && !(parent is NamespaceDeclarationSyntax))
            {
                parent = parent.Parent;
            }

            if (parent is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Name.ToString();
            }

            return null;
        }

        public static AttributeData GetAttributeFromHierarchy(INamedTypeSymbol symbol, string attributeName, bool excludeDefining = false)
        {
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var queue = new Queue<INamedTypeSymbol>();
            queue.Enqueue(symbol);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || visited.Contains(current))
                    continue;

                visited.Add(current);

                var attributeData = current.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == attributeName || attr.AttributeClass?.Name == attributeName + "Attribute");

                if (excludeDefining && SymbolEqualityComparer.Default.Equals(symbol, current))
                {
                    // Skip the defining symbol's own attribute
                }
                else if (attributeData != null)
                {
                    return attributeData;
                }

                if (current.BaseType != null)
                {
                    queue.Enqueue(current.BaseType);
                }

                foreach (var iface in current.Interfaces)
                {
                    queue.Enqueue(iface);
                }
            }

            return null;
        }
    }
}
