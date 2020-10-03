﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VCSFramework.V2.Templates.Standard
{
    public sealed class StandardTemplate : ProgramTemplate
    {
        private readonly ImmutableArray<MethodInfo> VBlanks;
        private readonly ImmutableArray<MethodInfo> KernelMethods;
        private readonly MethodInfo Kernel;
        private readonly MethodInfo? Overscan;

        internal override string GeneratedTypeName => "StandardTemplatedProgram";

        public StandardTemplate(Type programType) : base(programType)
        {
            var methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            VBlanks = programType.GetMethods(methodFlags)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(VBlankAttribute)))
                .ToImmutableArray();

            KernelMethods = programType.GetMethods(methodFlags)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(KernelAttribute)))
                .ToImmutableArray();
        }

        internal override string GenerateSourceText()
        {
            var prefix = "\t\t\t";
            var vblankCodeBuilder = new StringBuilder();
            if (!VBlanks.Any())
            {
                vblankCodeBuilder.AppendLine($"{prefix}// No user VBlank methods found.");
            }
            else
            {
                foreach (var vblank in VBlanks)
                {
                    vblankCodeBuilder.AppendLine($"{prefix}{ProgramType.FullName}.{vblank.Name}();");
                }
            }
            
// @TODO - Desperately need to prettify this file (indenting), and also open it in text editor on build (or expose option to).
            return $@"
// <auto-generated>
// This file was autogenerated as part of the CSharpTo2600 compilation process.
// </auto-generated>
using static VCSFramework.Registers;
using static VCSFramework.V2.AssemblyUtilities;

public static class {GeneratedTypeName}
{{
    public static void Main()
    {{
        while (true)
        {{
            // VBlank
            VSync = 0b10;
			WSync();
			WSync();
			WSync();
			Tim64T = 43;
			VSync = 0;

{vblankCodeBuilder}

            // @TODO - May want a debug flag that checks if 0 has already passed, to catch overrunning the available time.
            while (InTim != 0) ;

            WSync();
			VBlank = 0;

            // Call kernel
            byte lines = 191;
			while (lines != 0)
			{{
                lines--;
				WSync();
			}}

            // Overscan
            lines = 30;
			while (lines != 0)
			{{
                lines--;
				WSync();
			}}
        }}
    }}
}}";
        }
    }
}
