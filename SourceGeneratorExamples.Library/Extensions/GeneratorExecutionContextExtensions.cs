using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class GeneratorExecutionContextExtensions
    {
        public static INamedTypeSymbol GetTypeByMetadataName(this GeneratorExecutionContext context, string fullName)
        {
            return context.Compilation.GetTypeByMetadataName(fullName);
        }

        public static IEnumerable<SyntaxTree> GetSyntaxTrees(this GeneratorExecutionContext context)
        {
            return context.Compilation.SyntaxTrees;
        }

        public static SemanticModel GetSemanticModel(this GeneratorExecutionContext context, SyntaxTree syntaxTree)
        {
            return context.Compilation.GetSemanticModel(syntaxTree);
        }

        public static void ReportDiagnostic(this GeneratorExecutionContext context,
            DiagnosticDescriptor descriptor,
            INamedTypeSymbol typeSymbol)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, typeSymbol.Locations.First(), typeSymbol.Name));
        }


        public static IEnumerable<InputDocument> GetInputDocuments(this GeneratorExecutionContext context)
        {
            return context.GetSyntaxTrees().Select(syntaxTree
                => GetInputDocument(syntaxTree, context.GetSemanticModel(syntaxTree)));
        }

        private static InputDocument GetInputDocument(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            return new(semanticModel, GetTypeNodes(syntaxTree, semanticModel));
        }

        private static IEnumerable<TypeNode> GetTypeNodes(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            return syntaxTree.GetTypeDeclarations().Select(typeNode => GetTypeNode(semanticModel, typeNode));
        }

        private static TypeNode GetTypeNode(SemanticModel semanticModel, TypeDeclarationSyntax typeNode)
        {
            return new(typeNode, (INamedTypeSymbol) semanticModel.GetDeclaredSymbol(typeNode));
        }
    }
}