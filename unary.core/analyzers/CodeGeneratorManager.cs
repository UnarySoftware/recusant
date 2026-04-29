using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Unary.Core.Analyzers
{
    [Generator]
    public class CodeGeneratorManager : IIncrementalGenerator
    {
        private List<CodeGenerator> _generators = new List<CodeGenerator>();

        public void Initialize(IncrementalGeneratorInitializationContext generatorContext)
        {
            /*
            #if DEBUG
                        if (!Debugger.IsAttached)
                        {
                            Debugger.Launch();
                        }
            #endif
            */
            var classDeclarations = generatorContext.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax,
                    transform: (ctx, token) => ctx.Node as ClassDeclarationSyntax)
                .Where(c => c != null);

            var compilationAndClasses = generatorContext.CompilationProvider
                .Combine(classDeclarations.Collect());

            _generators.Clear();
            _generators.Add(new SingletonGenerator());

            generatorContext.RegisterSourceOutput(compilationAndClasses, (context, source) =>
            {
                var (compilation, classList) = source;

                foreach (var classDecl in classList)
                {
                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);

                    foreach (var generator in _generators)
                    {
                        generator.Generate(context, classDecl, semanticModel);
                    }
                }
            });
        }
    }
}
