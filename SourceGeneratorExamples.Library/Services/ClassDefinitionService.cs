using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Extensions;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Services
{
    public class ClassDefinitionService
    {
        public static Dictionary<string, ClassDefinition> GenerateClassDefinitions(
            IEnumerable<InputDocument> inputDocuments, INamedTypeSymbol attributeTypeSymbol)
        {
            var classDefinitions = new Dictionary<string, ClassDefinition>();
            inputDocuments.ForEach(inputDocument => inputDocument.TypeNodes
                .ForEach(typeNode =>
                    GetClassDefinition(classDefinitions, typeNode, inputDocument.SemanticModel, attributeTypeSymbol)));
            return classDefinitions;
        }

        private static void GetClassDefinition(IDictionary<string, ClassDefinition> classDefinitions,
            TypeNode typeNode, SemanticModel semanticModel, ISymbol attributeTypeSymbol)
        {
            string typeFullMetadataName = typeNode.TypeSymbol.GetFullMetadataName();
            var attr = typeNode.TypeSymbol.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass.Equals(attributeTypeSymbol));
            var ordered = attr.ToString().EndsWith($".{attributeTypeSymbol.Name}(true)");

            if (!classDefinitions.TryGetValue(typeFullMetadataName, out var classDefinition))
            {
                classDefinition = new ClassDefinition(typeNode, "Builder", ordered);
                classDefinitions.Add(typeFullMetadataName, classDefinition);
            }

            classDefinition.Properties.AddRange(typeNode.TypeSymbol.GetAllMembers()
                .Select(property => GetPropertyDefinition(semanticModel, property)));
        }

        private static PropertyDefinition GetPropertyDefinition(SemanticModel semanticModel, IPropertySymbol property) =>
            new(property.Name.ToUnderscoreCase(), property.Name, property.Type, semanticModel.IsNullable(property));
    }
}