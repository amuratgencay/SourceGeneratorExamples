using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorExamples.Library.Extensions;
using SourceGeneratorExamples.Library.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace SourceGeneratorExamples.Library.Services.BuilderServices
{
    public class UnorderedBuilderService : BuilderService
    {
        public virtual IEnumerable<CompilationUnit> GetCompilationUnits(GeneratorExecutionContext context,
            IEnumerable<ClassDefinition> classDefinitions) =>
            GenerateBuilders(context, classDefinitions, context.CancellationToken);

        private static CompilationUnit GenerateBuilder(
            GeneratorExecutionContext context, ClassDefinition builder,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var builderClass = ClassDeclaration(builder.Name)
                .AddModifiers(builder.Accessibility.GetModifiers())
                .AddModifiers(Token(PartialKeyword));

            // Add instance fields
            builderClass = builderClass
                .WithMembers(new SyntaxList<MemberDeclarationSyntax>(
                    builder.Properties.Select(property =>
                        FieldDeclaration(
                                VariableDeclaration(property.InternalRepresentationTypeSyntax)
                                    .AddVariables(VariableDeclarator(Identifier(property.FieldName)))
                            )
                            .AddModifiers(Token(PrivateKeyword))
                    )));

            // Add parameter-less constructor
            builderClass = builderClass.AddMembers(
                ConstructorDeclaration(builder.Name)
                    .AddModifiers(Token(PublicKeyword))
                    .AddBodyStatements()
            );

            // Add constructor from other builder of the same type
            var otherBuilderIdentifier = Identifier("otherBuilder");
            builderClass = builderClass
                .AddMembers(
                    ConstructorDeclaration(builder.Name)
                        .AddParameterListParameters(Parameter(otherBuilderIdentifier)
                            .WithType(ParseTypeName(builder.Name)))
                        .AddModifiers(Token(PublicKeyword))
                        .AddBodyStatements(
                            builder.Properties.Select(property =>
                                    (StatementSyntax) ExpressionStatement(AssignmentExpression(
                                        SimpleAssignmentExpression,
                                        IdentifierName(property.FieldName),
                                        MemberAccessExpression(SimpleMemberAccessExpression,
                                            IdentifierName(otherBuilderIdentifier),
                                            IdentifierName(property.FieldName))
                                    ))
                                )
                                .ToArray()
                        )
                );

            // Add constructor from a pre-existing/built instance
            var existingInstanceIdentifier = Identifier("existingInstance");
            builderClass = builderClass
                .AddMembers(
                    ConstructorDeclaration(builder.Name)
                        .AddParameterListParameters(Parameter(existingInstanceIdentifier)
                            .WithType(builder.TypeSyntax))
                        .AddModifiers(Token(PublicKeyword))
                        .AddBodyStatements(
                            builder.Properties.Select(property =>
                                    (StatementSyntax) ExpressionStatement(AssignmentExpression(
                                        SimpleAssignmentExpression,
                                        IdentifierName(property.FieldName),
                                        MemberAccessExpression(SimpleMemberAccessExpression,
                                            IdentifierName(existingInstanceIdentifier),
                                            IdentifierName(property.PropertyName))
                                    ))
                                )
                                .ToArray()
                        )
                );

            builderClass = AddMutationMethods(builder, builderClass);

            if (!TryAddBuildMethod(context, builder, ref builderClass)) return null;

            var originalCompilationUnit =
                builder.TypeNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (originalCompilationUnit is null) return null;

            var originalNamespaceDeclaration =
                builder.TypeNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            var declaration = originalNamespaceDeclaration is null
                ? (MemberDeclarationSyntax) builderClass
                : NamespaceDeclaration(
                    originalNamespaceDeclaration.Name,
                    originalNamespaceDeclaration.Externs,
                    originalNamespaceDeclaration.Usings,
                    new SyntaxList<MemberDeclarationSyntax>(builderClass));

            var compilationUnit = CompilationUnit()
                .WithExterns(originalCompilationUnit.Externs)
                .WithUsings(originalCompilationUnit.Usings)
                .AddMembers(declaration)
                .WithLeadingTrivia(Comment(GeneratedByDataBuilderGeneratorPreamble),
                    Trivia(NullableDirectiveTrivia(Token(EnableKeyword), true)))
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .NormalizeWhitespace();
            return new CompilationUnit(builder.Name + ".cs", compilationUnit);
        }

        private static IEnumerable<CompilationUnit> GenerateBuilders(
            GeneratorExecutionContext context, IEnumerable<ClassDefinition> builders,
            CancellationToken cancellationToken)
        {
            return builders.Select(builder
                    => GenerateBuilder(context, builder, cancellationToken))
                .Where(compilationUnit => compilationUnit != null).ToList();
        }


        private static ClassDeclarationSyntax AddMutationMethods(ClassDefinition builder,
            ClassDeclarationSyntax builderClass)
        {
            builder.Properties.ForEach(property =>
            {
                builderClass = CreateWithMethod(builder, builderClass, property);
                builderClass = CreateWithoutMethod(builder, builderClass, property);
            });

            return builderClass;
        }

        private static ClassDeclarationSyntax CreateWithMethod(
            ClassDefinition builder,
            ClassDeclarationSyntax builderClass,
            PropertyDefinition propertyDefinition)
        {
            var lowerCamelParameterName = propertyDefinition.PropertyName.ToCamelCase();
            var upperCamelParameterName = propertyDefinition.PropertyName.ToPascalCase();

            var localBuilderIdentifier = Identifier("mutatedBuilder");

            builderClass = builderClass.AddPublicMethod(ParseTypeName(builder.Name),
                "With" + upperCamelParameterName,
                Parameter(Identifier(lowerCamelParameterName)).WithType(propertyDefinition.TypeSyntax),
                CreateLocalVariable(builder, localBuilderIdentifier),
                AssignLocalVariable(localBuilderIdentifier, propertyDefinition,
                    IdentifierName(lowerCamelParameterName)),
                ReturnStatement(IdentifierName(localBuilderIdentifier)));

            return builderClass;
        }

        private static ClassDeclarationSyntax CreateWithoutMethod(
            ClassDefinition builder,
            ClassDeclarationSyntax builderClass,
            PropertyDefinition propertyDefinition)
        {
            if (!propertyDefinition.IsNullable && !(propertyDefinition.TypeSyntax is NullableTypeSyntax))
                return builderClass;

            var upperCamelParameterName = propertyDefinition.PropertyName.ToPascalCase();

            var localBuilderIdentifier = Identifier("mutatedBuilder");

            builderClass = builderClass.AddPublicMethod(ParseTypeName(builder.Name),
                "Without" + upperCamelParameterName,
                CreateLocalVariable(builder, localBuilderIdentifier),
                AssignLocalVariable(localBuilderIdentifier, propertyDefinition,
                    LiteralExpression(NullLiteralExpression)),
                ReturnStatement(IdentifierName(localBuilderIdentifier)));

            return builderClass;
        }

        private static ExpressionStatementSyntax AssignLocalVariable(SyntaxToken localBuilderIdentifier,
            PropertyDefinition property, ExpressionSyntax assignedExpressionSyntax)
        {
            return ExpressionStatement(AssignmentExpression(SimpleAssignmentExpression,
                MemberAccessExpression(SimpleMemberAccessExpression,
                    IdentifierName(localBuilderIdentifier), IdentifierName(property.FieldName)),
                assignedExpressionSyntax));
        }

        private static LocalDeclarationStatementSyntax CreateLocalVariable(ClassDefinition builder,
            SyntaxToken localBuilderIdentifier)
        {
            return LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
                .AddVariables(VariableDeclarator(localBuilderIdentifier)
                    .WithInitializer(EqualsValueClause(
                        ObjectCreationExpression(ParseTypeName(builder.Name))
                            .AddArgumentListArguments(Argument(ThisExpression()))))));
        }


        private static bool TryAddBuildMethod(GeneratorExecutionContext context,
            ClassDefinition classDefinition,
            ref ClassDeclarationSyntax builderClass)
        {
            var objectType = classDefinition.TypeSyntax;
            var buildMethodStatements = new List<StatementSyntax>();

            var propertiesSetViaConstructor = new List<PropertyDefinition>();

            if (!GetCreationExpression(context, classDefinition, objectType, propertiesSetViaConstructor,
                buildMethodStatements, out ObjectCreationExpressionSyntax creationExpression))
                return false;

            var buildingInstanceIdentifier = Identifier("instance");
            buildMethodStatements.Add(CreateLocalVariable(buildingInstanceIdentifier, creationExpression));
            buildMethodStatements.AddRange(classDefinition.Properties.Except(propertiesSetViaConstructor)
                .Select(property => GetNullableArgument(property, buildingInstanceIdentifier)));
            buildMethodStatements.Add(ReturnStatement(IdentifierName(buildingInstanceIdentifier)));
            builderClass = builderClass.AddPublicMethod(objectType, "Build", buildMethodStatements);

            return true;
        }

        private static bool GetCreationExpression(GeneratorExecutionContext context, ClassDefinition classDefinition,
            TypeSyntax objectType, ICollection<PropertyDefinition> propertiesSetViaConstructor,
            ICollection<StatementSyntax> buildMethodStatements,
            out ObjectCreationExpressionSyntax creationExpression)
        {
            creationExpression = ObjectCreationExpression(objectType);
            if (classDefinition.ConstructorToUse is { } constructorToUse)
            {
                var arguments = new ArgumentSyntax[constructorToUse.Parameters.Length];
                var blocked = false;
                for (var i = 0; i < constructorToUse.Parameters.Length; i++)
                {
                    var parameterName = constructorToUse.Parameters[i].Name;
                    var matchingProperty = classDefinition.Properties.FirstOrDefault(p =>
                        p.PropertyName.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
                    if (matchingProperty == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(CannotInferBuilderPropertyFromArgumentDiagnostic,
                            constructorToUse.Parameters[i].Locations[0]));
                        blocked = true;
                    }
                    else
                    {
                        arguments[i] = Argument(matchingProperty.PropertyAccessAndDefaultingExpression());
                        propertiesSetViaConstructor.Add(matchingProperty);
                    }

                    foreach (var property in propertiesSetViaConstructor.Where(x => x.IsReferenceType && !x.IsNullable))
                    {
                        var throwStatement = ThrowStatement(
                            ObjectCreationExpression(ParseTypeName("System.InvalidOperationException"))
                                .AddArgumentListArguments(Argument(LiteralExpression(
                                    StringLiteralExpression,
                                    Literal(
                                        $"No value present for required property '{property.PropertyName}'.")))));
                        buildMethodStatements.Add(IfStatement(IdentifierName(property.FieldName).NullCheck(),
                            throwStatement));
                    }
                }

                if (blocked) return false;

                creationExpression = creationExpression.AddArgumentListArguments(arguments);
            }
            else
            {
                creationExpression = creationExpression.WithArgumentList(ArgumentList());
            }

            return true;
        }

        private static StatementSyntax GetNullableArgument(PropertyDefinition property,
            SyntaxToken buildingInstanceIdentifier)
        {
            return IfStatement(
                PrefixUnaryExpression(LogicalNotExpression,
                    ParenthesizedExpression(IdentifierName(property.FieldName).NullCheck())),
                ExpressionStatement(
                    AssignmentExpression(SimpleAssignmentExpression,
                        MemberAccessExpression(SimpleMemberAccessExpression,
                            IdentifierName(buildingInstanceIdentifier),
                            IdentifierName(property.PropertyName)),
                        property.PropertyAccessUnwrappingNullable())));
        }

        private static LocalDeclarationStatementSyntax CreateLocalVariable(SyntaxToken buildingInstanceIdentifier,
            ExpressionSyntax creationExpression)
        {
            return LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
                .AddVariables(VariableDeclarator(buildingInstanceIdentifier)
                    .WithInitializer(EqualsValueClause(creationExpression))));
        }
    }
}