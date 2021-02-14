using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratorExamples.Library.Models
{
    public class InputDocument
    {
        public InputDocument(SemanticModel semanticModel, IEnumerable<TypeNode> typeNodes)
        {
            SemanticModel = semanticModel;
            TypeNodes = typeNodes;
        }

        public SemanticModel SemanticModel { get; }
        public IEnumerable<TypeNode> TypeNodes { get; }
    }

    public class TypeNode
    {
        public TypeNode(TypeDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol typeSymbol)
        {
            TypeDeclarationSyntax = typeDeclarationSyntax;
            TypeSymbol = typeSymbol;
        }

        public TypeDeclarationSyntax TypeDeclarationSyntax { get; }
        public INamedTypeSymbol TypeSymbol { get; }
    }

    public class CompilationUnit
    {
        public CompilationUnit(string name, CompilationUnitSyntax compilationUnitSyntax)
        {
            Name = name;
            CompilationUnitSyntax = compilationUnitSyntax;
        }

        public string Name { get; }
        public CompilationUnitSyntax CompilationUnitSyntax { get; }
    }
}