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
    public class OrderedBuilderService : BuilderService
    {
        public virtual IEnumerable<CompilationUnit> GetCompilationUnits(GeneratorExecutionContext context,
            IEnumerable<ClassDefinition> classDefinitions)
        {
            return GenerateBuilders(context, classDefinitions, context.CancellationToken);
        }

        private static CompilationUnit GenerateBuilder(
            GeneratorExecutionContext context, ClassDefinition builder,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();


            var holderInterfaces = new Dictionary<string, InterfaceDeclarationSyntax>();

            static MethodDeclarationSyntax GetMethodDeclaration(TypeSyntax typeSyntax, string name)
            {
                return MethodDeclaration(new SyntaxList<AttributeListSyntax>(), new SyntaxTokenList(),
                    typeSyntax, null, Identifier(name), null, ParameterList(),
                    new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                    null,
                    Token(SemicolonToken));
            }

            foreach (var property in builder.Properties)
            {
                var propertyName = property.PropertyName;
                var interfaceName = $"I{propertyName}Holder";
                var holderInterface = InterfaceDeclaration(interfaceName)
                    .AddModifiers(builder.Accessibility.GetModifiers())
                    .AddMembers(GetMethodDeclaration(ParseTypeName("bool"), $"IsValid{propertyName}")
                        .AddParameterListParameters(Parameter(Identifier(propertyName.ToCamelCase()))
                            .WithType(property.TypeSyntax)));

                holderInterfaces.Add(propertyName, holderInterface);
            }

            var builderInterfaceName = $"I{builder.Name}";


            var builderInterface = InterfaceDeclaration(builderInterfaceName)
                .AddModifiers(builder.Accessibility.GetModifiers())
                .AddMembers(GetMethodDeclaration(builder.TypeSyntax, "Build"));

            holderInterfaces.Add(builderInterfaceName, builderInterface);

            for (var i = 0; i < holderInterfaces.Count - 1; i++)
            {
                var propertyName1 = holderInterfaces.Keys.ToArray()[i];
                var interfaceSyntax1 = holderInterfaces[propertyName1];
                var property = builder.Properties[i];

                var propertyName2 = holderInterfaces.Keys.ToArray()[i + 1];
                var interfaceName = $"I{propertyName2}Holder";
                if (i + 1 >= holderInterfaces.Count - 1)
                    interfaceName = $"I{builder.Name}";

                interfaceSyntax1 = interfaceSyntax1.AddMembers(
                    GetMethodDeclaration(ParseTypeName(interfaceName), $"With{propertyName1}")
                        .AddParameterListParameters(Parameter(Identifier(propertyName1.ToCamelCase()))
                            .WithType(property.TypeSyntax)));

                holderInterfaces[propertyName1] = interfaceSyntax1;
            }


            var builderClass = ClassDeclaration(builder.Name)
                .AddModifiers(builder.Accessibility.GetModifiers())
                .AddModifiers(Token(PartialKeyword));

            builderClass = holderInterfaces.Values.Select(holderInterface => holderInterface.Identifier.Text)
                .Aggregate(builderClass, (current, holderInterfaceName) => current.AddBase(holderInterfaceName));


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


            foreach (var property in builder.Properties)
            {
                var propertyName = property.PropertyName;
                var returnStatement =
                    ReturnStatement(Token(ReturnKeyword), ParseExpression("true"), Token(SemicolonToken));

                if (!property.IsNullable && property.IsReferenceType && !(property.TypeSyntax is NullableTypeSyntax))
                    returnStatement = ReturnStatement(Token(ReturnKeyword),
                        PrefixUnaryExpression(LogicalNotExpression,
                            ParenthesizedExpression(IdentifierName(propertyName.ToCamelCase()).NullCheck())),
                        Token(SemicolonToken));

                builderClass = builderClass
                    .AddMembers(MethodDeclaration(ParseTypeName("bool"), $"IsValid{propertyName}")
                        .AddModifiers(Token(PublicKeyword))
                        .AddParameterListParameters(Parameter(Identifier(propertyName.ToCamelCase()))
                            .WithType(property.TypeSyntax))
                        .AddBodyStatements(returnStatement));
            }


            // Add parameter-less constructor
            builderClass = builderClass.AddMembers(
                ConstructorDeclaration(builder.Name)
                    .AddModifiers(Token(PrivateKeyword))
                    .AddBodyStatements()
            );

            // Add constructor from other builder of the same type
            var otherBuilderIdentifier = Identifier("otherBuilder");
            builderClass = builderClass
                .AddMembers(
                    ConstructorDeclaration(builder.Name)
                        .AddParameterListParameters(Parameter(otherBuilderIdentifier)
                            .WithType(ParseTypeName(builder.Name)))
                        .AddModifiers(Token(PrivateKeyword))
                        .AddBodyStatements(
                            builder.Properties.Select(property =>
                                    (StatementSyntax)ExpressionStatement(AssignmentExpression(
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


            // Add static create method
            builderClass = builderClass.AddMembers(
                MethodDeclaration(ParseTypeName($"I{holderInterfaces.Keys.First()}Holder"),
                        builder.TypeSyntax.ToString())
                    .AddModifiers(Token(PublicKeyword), Token(StaticKeyword))
                    .AddBodyStatements(ReturnStatement(Token(ReturnKeyword),
                        ObjectCreationExpression(ParseTypeName(builder.Name)).AddArgumentListArguments(),
                        Token(SemicolonToken))));


            builderClass = AddMutationMethods(builder, builderClass, holderInterfaces);

            if (!TryAddBuildMethod(context, builder, ref builderClass)) return null;

            var originalCompilationUnit =
                builder.TypeNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (originalCompilationUnit is null) return null;

            var originalNamespaceDeclaration =
                builder.TypeNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var syntaxList = new List<MemberDeclarationSyntax>
            {
                builderClass
            };
            syntaxList.AddRange(holderInterfaces.Values);

            if (builder.TypeNode.Modifiers.Any(x => x.Text.Equals("partial"))){

                var toStringClass = ClassDeclaration(builder.TypeSyntax.ToString())
                    .AddModifiers(builder.Accessibility.GetModifiers())
                    .AddModifiers(Token(PartialKeyword));

                var props = string.Join(", ",
                    builder.Properties.Select(x => $"{x.PropertyName.ToCamelCase()}= {{{x.PropertyName}}}"));
                toStringClass = toStringClass
                    .AddMembers(MethodDeclaration(ParseTypeName("string"), "ToString")
                        .AddModifiers(Token(PublicKeyword), Token(OverrideKeyword))
                        .AddBodyStatements(ReturnStatement(Token(ReturnKeyword),
                            ParseExpression($"$\"{builder.TypeSyntax}({props})\""),
                            Token(SemicolonToken))));

                toStringClass = toStringClass
                    .AddMembers(MethodDeclaration(ParseTypeName($"I{holderInterfaces.Keys.First()}Holder"), "Builder")
                        .AddModifiers(Token(InternalKeyword), Token(StaticKeyword))
                        .AddBodyStatements(ReturnStatement(Token(ReturnKeyword),
                            ParseExpression($"{builder.Name}.{builder.TypeSyntax}()"), Token(SemicolonToken))));


                syntaxList.Add(toStringClass);
            }


            var declaration = originalNamespaceDeclaration is null
                ? (MemberDeclarationSyntax)builderClass
                : NamespaceDeclaration(
                    originalNamespaceDeclaration.Name,
                    originalNamespaceDeclaration.Externs,
                    originalNamespaceDeclaration.Usings,
                    new SyntaxList<MemberDeclarationSyntax>(syntaxList));

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
            ClassDeclarationSyntax builderClass,
            Dictionary<string, InterfaceDeclarationSyntax> holderInterfaces)
        {
            for (var i = 0; i < builder.Properties.Count; i++)
            {
                var property = builder.Properties[i];
                var holderName = holderInterfaces.Keys.ToList()[i + 1];
                var holderInterface = holderInterfaces[holderName];
                holderName = holderInterface.Identifier.Text;
                builderClass = CreateWithMethod(builder, builderClass, property, holderName);
            }

            return builderClass;
        }

        private static ClassDeclarationSyntax CreateWithMethod(
            ClassDefinition builder,
            ClassDeclarationSyntax builderClass,
            PropertyDefinition propertyDefinition,
            string holderName)
        {
            var lowerCamelParameterName = propertyDefinition.PropertyName.ToCamelCase();
            var upperCamelParameterName = propertyDefinition.PropertyName.ToPascalCase();

            var localBuilderIdentifier = Identifier("mutatedBuilder");

            builderClass = builderClass.AddPublicMethod(ParseTypeName(holderName),
                "With" + upperCamelParameterName,
                Parameter(Identifier(lowerCamelParameterName)).WithType(propertyDefinition.TypeSyntax),
                CreateLocalVariable(builder, localBuilderIdentifier),
                AssignLocalVariable(localBuilderIdentifier, propertyDefinition,
                    IdentifierName(lowerCamelParameterName)),
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

                    throwStatement = ThrowStatement(
                        ObjectCreationExpression(ParseTypeName("System.ArgumentException"))
                            .AddArgumentListArguments(Argument(LiteralExpression(
                                StringLiteralExpression,
                                Literal(
                                    $"Value is not valid for '{property.PropertyName}'.")))));

                    buildMethodStatements.Add(IfStatement(PrefixUnaryExpression(LogicalNotExpression,
                            InvocationExpression(IdentifierName($"IsValid{property.PropertyName}"))
                                .AddArgumentListArguments(Argument(property.PropertyAccessAndDefaultingExpression()))),
                        throwStatement));
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
                    InvocationExpression(IdentifierName($"IsValid{property.PropertyName}"))
                        .AddArgumentListArguments(Argument(property.PropertyAccessAndDefaultingExpression()))),
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