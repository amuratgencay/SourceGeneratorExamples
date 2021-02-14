using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Filters;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Services
{
    public class FilterService
    {
        private readonly INamedTypeSymbol _attributeTypeSymbol;
        private readonly GeneratorExecutionContext _context;

        private readonly List<IInputDocumentFilter> _inputDocumentFilters =
            new()
            {
                new InputDocumentAttributeFilter(),
                new InputDocumentTypeSymbolFilter(),
                new InputDocumentAbstractClassFilter()
            };

        public FilterService(GeneratorExecutionContext context, INamedTypeSymbol attributeTypeSymbol)
        {
            _context = context;
            _attributeTypeSymbol = attributeTypeSymbol;
        }

        public IEnumerable<InputDocument> Filter(IEnumerable<InputDocument> inputDocuments)
        {
            _inputDocumentFilters.ForEach(filter =>
            {
                inputDocuments = filter.Filter(inputDocuments, _attributeTypeSymbol, _context);
            });
            return inputDocuments;
        }

        public static FilterService CreateInstance(GeneratorExecutionContext context,
            INamedTypeSymbol attributeTypeSymbol) =>
            new(context, attributeTypeSymbol);
    }
}