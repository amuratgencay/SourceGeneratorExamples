using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Filters
{
    public class InputDocumentTypeSymbolFilter : IInputDocumentFilter
    {
        public IEnumerable<InputDocument> Filter(IEnumerable<InputDocument> inputDocuments,
            INamedTypeSymbol attributeTypeSymbol, GeneratorExecutionContext context) =>
            inputDocuments.Select(GetInputDocument);

        private static InputDocument GetInputDocument(InputDocument inputDocument) 
            => new(inputDocument.SemanticModel, inputDocument.TypeNodes);
    }
}