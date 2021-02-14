using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class ClassDeclarationSyntaxExtensions
    {
        public static BaseListSyntax AddBase(BaseListSyntax baseList, string baseName)
        {
            baseList = baseList.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(baseName)));
            return baseList;
        }

        public static ClassDeclarationSyntax AddBase(this ClassDeclarationSyntax type, string baseName)
        {
            if (type.BaseList == null) type = type.WithBaseList(SyntaxFactory.BaseList());
            return type.WithBaseList(AddBase(type.BaseList, baseName));
        }

        public static ClassDeclarationSyntax AddPublicMethod(this ClassDeclarationSyntax classDeclarationSyntax,
            TypeSyntax returnType, string name, IEnumerable<StatementSyntax> body) =>
            classDeclarationSyntax.AddMembers(
                SyntaxFactory.MethodDeclaration(returnType, name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(body)));


        public static ClassDeclarationSyntax AddPublicMethod(this ClassDeclarationSyntax classDeclarationSyntax,
            TypeSyntax returnType, string name, ParameterSyntax parameter, params StatementSyntax[] body) =>
            classDeclarationSyntax.AddMembers(SyntaxFactory.MethodDeclaration(returnType, name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameter)
                .AddBodyStatements(body));

        public static ClassDeclarationSyntax AddPublicMethod(this ClassDeclarationSyntax classDeclarationSyntax,
            TypeSyntax returnType, string name, params StatementSyntax[] body) =>
            classDeclarationSyntax.AddMembers( SyntaxFactory.MethodDeclaration(returnType, name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBodyStatements(body));
    }
}