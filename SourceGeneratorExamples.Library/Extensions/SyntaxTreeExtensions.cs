using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class SyntaxTreeExtensions
    {
        public static IEnumerable<TypeDeclarationSyntax> GetTypeDeclarations(this SyntaxTree syntaxTree)
        {
            return syntaxTree.GetRoot()
                .DescendantNodesAndSelf(n => n is CompilationUnitSyntax ||
                                             n is NamespaceDeclarationSyntax ||
                                             n is TypeDeclarationSyntax)
                .OfType<TypeDeclarationSyntax>();
        }
    }
}