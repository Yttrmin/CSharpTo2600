#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VCSFramework.V2.Templates
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
