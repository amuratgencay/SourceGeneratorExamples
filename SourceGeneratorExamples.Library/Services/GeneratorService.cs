using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using SourceGeneratorExamples.Library.Extensions;
using SourceGeneratorExamples.Library.Models;
using SourceGeneratorExamples.Library.Services.BuilderServices;
using static SourceGeneratorExamples.Library.Services.ClassDefinitionService;

namespace SourceGeneratorExamples.Library.Services
{
    public class GeneratorService
    {
        private readonly INamedTypeSymbol _attributeTypeSymbol;
        private readonly GeneratorExecutionContext _context;
        private readonly FilterService _filterService;

        private GeneratorService(GeneratorExecutionContext context, string attributeTypeFullName)
        {
            _context = context;
            _attributeTypeSymbol = context.GetTypeByMetadataName(attributeTypeFullName);
            _filterService = FilterService.CreateInstance(_context, _attributeTypeSymbol);
        }

        public static GeneratorService CreateInstance(GeneratorExecutionContext context, string attributeTypeFullName) 
            => new(context, attributeTypeFullName);

        private IEnumerable<CompilationUnit> GetCompilationUnits()
        {
            var filteredInputDocuments = _filterService.Filter(_context.GetInputDocuments());
            var classDefinitions = GenerateClassDefinitions(filteredInputDocuments, _attributeTypeSymbol);


            var unOrdered = new UnorderedBuilderService()
                .GetCompilationUnits(_context, classDefinitions.Values.Where(x => !x.Ordered));

            var ordered = new OrderedBuilderService()
                .GetCompilationUnits(_context, classDefinitions.Values.Where(x => x.Ordered));

            return unOrdered.Concat(ordered);
        }

        public void Generate()
        {
            GetCompilationUnits()
                .ForEach(compilationUnit =>
                {
                    _context.AddSource(compilationUnit.Name, compilationUnit.CompilationUnitSyntax.GetText(Encoding.UTF8));
                });
        }
    }
}