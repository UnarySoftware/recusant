
using Microsoft.CodeAnalysis;

namespace Unary.Core.Analyzers
{
    public static class StaticInfo
    {
        public static readonly DiagnosticDescriptor CodeGenErrorDescriptor = new DiagnosticDescriptor(
#pragma warning disable RS2008
        id: "UN0001",
#pragma warning restore RS2008
        title: "Code Generation Error",
        messageFormat: "Error during code generation: {0}",
        category: "Unary.CodeGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    }
}
