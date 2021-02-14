using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Filters
{
    public interface IInputDocumentFilter
    {
        IEnumerable<InputDocument> Filter(IEnumerable<InputDocument> inputDocuments,
            INamedTypeSymbol attributeTypeSymbol,
            GeneratorExecutionContext context);
    }
}