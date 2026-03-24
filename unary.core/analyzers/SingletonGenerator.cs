using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Unary.Core.Analyzers
{
    public class SingletonGenerator : CodeGenerator
    {
        public override void Generate(SourceProductionContext context, ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
            {
                return;
            }

            var result = GetAttributeFromHierarchy(classSymbol, "SingletonProvider", true);

            if (result == null)
            {
                return;
            }

            string singletonPath;
            int genericPathIndex;

            if (result.ConstructorArguments.Length != 2)
            {
                return;
            }

            var arg0 = result.ConstructorArguments[0];
            if (arg0.Kind == TypedConstantKind.Primitive && arg0.Value is string strValue)
            {
                singletonPath = strValue;
            }
            else
            {
                return;
            }

            var arg1 = result.ConstructorArguments[1];
            if (arg1.Kind == TypedConstantKind.Primitive && arg1.Value is int intValue)
            {
                genericPathIndex = intValue;
            }
            else
            {
                return;
            }

            if (genericPathIndex != -1)
            {
                var baseType = classSymbol.BaseType;

                if (baseType != null && baseType.TypeArguments.Length > 0 && genericPathIndex < baseType.TypeArguments.Length)
                {
                    var targetSymbol = baseType.TypeArguments[genericPathIndex];

                    singletonPath = string.Format(singletonPath, targetSymbol.Name);
                }
            }

            if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(Diagnostic.Create(StaticInfo.CodeGenErrorDescriptor,
                Location.Create(classDeclaration.SyntaxTree, classDeclaration.Span), "Class with a [SingletonProvider] attribute inheritance has to be partial."));
                return;
            }

            var namespaceName = GetNamespace(classDeclaration);
            var className = classDeclaration.Identifier.Text;

            var sourceText = GenerateWrapperClass(namespaceName, className, singletonPath);
            context.AddSource($"{className}.g.cs", SourceText.From(sourceText, System.Text.Encoding.UTF8));
        }

        private string GenerateWrapperClass(string namespaceName, string className, string providerName)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using Godot;");
            sb.AppendLine("using Unary.Core;\n");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public partial class {className}");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static {className} Singleton");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            get");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                return {providerName}<{className}>();");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
    }
}
