using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorExamples.Library.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class ExpressionSyntaxExtensions
    {
        public static ExpressionSyntax NullCheck(this ExpressionSyntax expressionToCheck)
        {
            return IsPatternExpression(expressionToCheck,
                ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)));
        }

        public static ExpressionSyntax PropertyAccessAndDefaultingExpression(this PropertyDefinition property)
        {
            return property.IsReferenceType
                ? (ExpressionSyntax) IdentifierName(property.FieldName)
                : BinaryExpression(SyntaxKind.CoalesceExpression, IdentifierName(property.FieldName),
                    LiteralExpression(SyntaxKind.DefaultLiteralExpression));
        }

        public static ExpressionSyntax PropertyAccessUnwrappingNullable(this PropertyDefinition property)
        {
            return property.IsReferenceType
                ? (ExpressionSyntax) IdentifierName(property.FieldName)
                : MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(property.FieldName),
                    IdentifierName("Value"));
        }
    }
}