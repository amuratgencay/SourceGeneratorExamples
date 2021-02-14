using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace SourceGeneratorExamples.Library.Extensions
{
    internal static class SyntaxExtensions
    {
        internal static bool ContainsAttributeType(this SyntaxList<AttributeListSyntax> attributes,
            SemanticModel semanticModel, INamedTypeSymbol attributeType, bool exactMatch = false)
        {
            return attributes.Any(list => list.Attributes.Any(attribute =>
                attributeType.IsAssignableFrom(semanticModel.GetTypeInfo(attribute).Type, exactMatch)));
        }

        internal static SyntaxToken[] GetModifiers(this Accessibility accessibility)
        {
            var list = new List<SyntaxToken>(2);

            switch (accessibility)
            {
                case Accessibility.Internal:
                    list.Add(Token(InternalKeyword));
                    break;
                case Accessibility.Public:
                    list.Add(Token(PublicKeyword));
                    break;
                case Accessibility.Private:
                    list.Add(Token(PrivateKeyword));
                    break;
                case Accessibility.Protected:
                    list.Add(Token(ProtectedKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    list.Add(Token(InternalKeyword));
                    list.Add(Token(ProtectedKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    list.Add(Token(PrivateKeyword));
                    list.Add(Token(ProtectedKeyword));
                    break;
                case Accessibility.NotApplicable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
            }

            return list.ToArray();
        }
    }
}