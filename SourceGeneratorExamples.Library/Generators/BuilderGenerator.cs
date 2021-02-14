using Microsoft.CodeAnalysis;
using System.Diagnostics;
using static SourceGeneratorExamples.Library.Services.GeneratorService;

namespace SourceGeneratorExamples.Library.Generators
{
    [Generator]
    public class BuilderGenerator : ISourceGenerator
    {
        private const string AttributeTypeFullName = "SourceGeneratorExamples.Library.BuilderAttribute";

        public void Initialize(GeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
        }

        public void Execute(GeneratorExecutionContext context) 
            => CreateInstance(context, AttributeTypeFullName).Generate();
    }
}