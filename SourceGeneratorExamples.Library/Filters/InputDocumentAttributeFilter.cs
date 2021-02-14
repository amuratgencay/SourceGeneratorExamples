using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Extensions;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Filters
{
    public class InputDocumentAttributeFilter : IInputDocumentFilter
    {
        public IEnumerable<InputDocument> Filter(IEnumerable<InputDocument> inputDocuments,
            INamedTypeSymbol attributeTypeSymbol, GeneratorExecutionContext context) =>
            inputDocuments.Select(inputDocument => GetInputDocument(inputDocument, attributeTypeSymbol));

        private static InputDocument GetInputDocument(InputDocument inputDocument, INamedTypeSymbol attributeTypeSymbol) =>
            new(inputDocument.SemanticModel, inputDocument.TypeNodes
                .Where(typeNode => typeNode.TypeDeclarationSyntax.AttributeLists
                    .ContainsAttributeType(inputDocument.SemanticModel, attributeTypeSymbol)));
    }
}