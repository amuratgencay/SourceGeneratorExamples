using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorExamples.Library.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceGeneratorExamples.Library.Models
{
    public class PropertyDefinition
    {
        public PropertyDefinition(string fieldName, string propertyName, ITypeSymbol typeSymbol, bool isNullable)
        {
            FieldName = fieldName;
            PropertyName = propertyName;
            TypeSymbol = typeSymbol;
            var typeSyntax = TypeSyntax = ParseTypeName(typeSymbol.ToDisplayString());
            TypeFullMetadataName = typeSymbol.GetFullMetadataName();
            IsNullable = isNullable;
            InternalRepresentationTypeSyntax = !(typeSyntax is NullableTypeSyntax)
                ? NullableType(typeSyntax)
                : typeSyntax;
        }

        public string FieldName { get; }

        public string PropertyName { get; }

        public ITypeSymbol TypeSymbol { get; }

        public TypeSyntax TypeSyntax { get; }

        public TypeSyntax InternalRepresentationTypeSyntax { get; }

        public string TypeFullMetadataName { get; }

        public bool IsNullable { get; }

        public bool IsReferenceType => TypeSymbol.IsReferenceType;
    }
}