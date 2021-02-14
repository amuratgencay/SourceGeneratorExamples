#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorExamples.Library.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceGeneratorExamples.Library.Models
{
    public class ClassDefinition
    {
        public ClassDefinition(TypeNode typeNode, string nameSuffix, bool ordered)
        {
            Namespace = typeNode.TypeSymbol.ContainingNamespace?.GetFullMetadataName();
            Name = typeNode.TypeSymbol.Name + nameSuffix;
            TypeNode = typeNode.TypeDeclarationSyntax;
            Accessibility = typeNode.TypeSymbol.DeclaredAccessibility;
            TypeSyntax = ParseTypeName(typeNode.TypeSymbol.Name);
            ConstructorToUse = typeNode.TypeSymbol.GetConstructor();
            Ordered = ordered;
        }

        public string? Namespace { get; }

        public string Name { get; }

        public TypeSyntax TypeSyntax { get; }

        public TypeDeclarationSyntax TypeNode { get; }

        public Accessibility Accessibility { get; }

        public IMethodSymbol? ConstructorToUse { get; set; }
        public bool Ordered { get; }
        public List<PropertyDefinition> Properties { get; } = new();
    }
}