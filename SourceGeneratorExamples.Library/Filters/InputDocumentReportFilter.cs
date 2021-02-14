using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Extensions;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Filters
{
    public abstract class InputDocumentReportFilter : IInputDocumentFilter
    {
        protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }

        public IEnumerable<InputDocument> Filter(IEnumerable<InputDocument> inputDocuments,
            INamedTypeSymbol attributeTypeSymbol, GeneratorExecutionContext context)
        {
            var enumerable = inputDocuments as InputDocument[] ?? inputDocuments.ToArray();
            ReportDiagnostic(enumerable, context);
            return enumerable.Select(GetInputDocument);
        }

        protected void ReportDiagnostic(IEnumerable<InputDocument> inputDocuments, GeneratorExecutionContext context)
        {
            inputDocuments
                .ForEach(inputDocument =>
                    FilterTypeNodes(inputDocument, true)
                        .ForEach(typeNode => context.ReportDiagnostic(DiagnosticDescriptor, typeNode.TypeSymbol)));
        }

        protected abstract IEnumerable<TypeNode> FilterTypeNodes(InputDocument inputDocument, bool reportCase);

        protected InputDocument GetInputDocument(InputDocument inputDocument) 
            => new(inputDocument.SemanticModel, FilterTypeNodes(inputDocument, false));
    }
}