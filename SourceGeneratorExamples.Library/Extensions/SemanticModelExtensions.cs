using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class SemanticModelExtensions
    {
        public static bool IsNullable(this SemanticModel semanticModel, IPropertySymbol propertySymbol)
        {
            if (propertySymbol.Type.IsValueType)
                return propertySymbol.Type.IsNullable();

            return AnnotationsEnabled(semanticModel, propertySymbol) && IsNullableTypeSyntax(propertySymbol);
        }

        private static bool AnnotationsEnabled(SemanticModel semanticModel, ISymbol propertySymbol)
        {
            return semanticModel.GetNullableContext(propertySymbol.Locations[0].SourceSpan.Start).AnnotationsEnabled();
        }

        private static bool IsNullableTypeSyntax(ISymbol propertySymbol)
        {
            return propertySymbol.DeclaringSyntaxReferences.Length != 0 &&
                   (propertySymbol.DeclaringSyntaxReferences[0].GetSyntax()
                       as PropertyDeclarationSyntax)?.Type is NullableTypeSyntax;
        }
    }
}