using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Models;

namespace SourceGeneratorExamples.Library.Filters
{
    public class InputDocumentAbstractClassFilter : InputDocumentReportFilter
    {
        protected override DiagnosticDescriptor DiagnosticDescriptor =>
            new("DBG001", "Cannot generate data builder for abstract class",
                "Cannot generate data builder for abstract class {0}",
                "BuilderGenerator", DiagnosticSeverity.Error, true);

        protected override IEnumerable<TypeNode> FilterTypeNodes(InputDocument inputDocument, bool isAbstract) 
            => inputDocument.TypeNodes.Where(typeNode => typeNode.TypeSymbol.IsAbstract == isAbstract);
    }
}