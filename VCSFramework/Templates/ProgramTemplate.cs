#nullable enable
using System;

namespace VCSFramework.Templates
{
    public abstract class ProgramTemplate
    {
        /// <summary>The type that is attributed with <see cref="TemplatedProgramAttribute"/>.</summary>
        protected internal Type ProgramType { get; }

        internal abstract string GeneratedTypeName { get; }

        internal ProgramTemplate(Type programType)
        {
            ProgramType = programType;
        }

        internal abstract string GenerateSourceText();
    }
}
