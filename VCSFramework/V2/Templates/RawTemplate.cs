﻿using System;
using System.Reflection;

namespace VCSFramework.V2.Templates
{
    public sealed class RawTemplate : ProgramTemplate
    {
        private readonly MethodInfo UserEntryPoint;
        internal override string GeneratedTypeName => "RawTemplatedProgram";

        internal RawTemplate(Type programType) : base(programType)
        {
            // @TODO - Throw if non-void or arity > 0.
            UserEntryPoint = programType.Assembly.GetEntryPoint() ?? throw new ArgumentException($"{programType.FullName} does not contain the entry point.");
        }

        internal override string GenerateSourceText()
        {
            return
$@"// <auto-generated>
// This file was autogenerated as part of the CSharpTo2600 compilation process.
// </auto-generated>

public static class {GeneratedTypeName}
{{
    // The true entry point of the compiled VCS program.
    public static void Main()
    {{
        // A RawTemplate is the lightest template available and has no special features.
        // It's also the default template used if you didn't mark a type with [ProgramTemplate], but have a 'static void Main()' entry point method.

        // Call user entry point. It is up to the user to manually manage everything that's required for a functioning VCS program.
        {$"{UserEntryPoint.DeclaringType!.FullName}.{UserEntryPoint.Name}();"}
    }}
}}";
        }
    }
}
