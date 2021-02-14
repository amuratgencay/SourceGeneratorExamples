using System;
using System.Diagnostics;

namespace SourceGeneratorExamples.Library
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [Conditional("CodeGeneration")]
    public sealed class BuilderAttribute : Attribute
    {
        public BuilderAttribute(bool ordered = false)
        {
            Ordered = ordered;
        }

        public bool Ordered { get; set; }
    }
}