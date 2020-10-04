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
            var vblankCodeBuilder = new StringBuilder();
            if (!VBlanks.Any())
            {
                vblankCodeBuilder.AppendLine("// No user VBlank methods found.");
            }
            else
            {
                vblankCodeBuilder.AppendLine($"// Invoke user-provided [{nameof(VBlankAttribute)}] methods:");
                foreach (var vblank in VBlanks)
                {
                    vblankCodeBuilder.AppendLine($"{ProgramType.FullName}.{vblank.Name}();");
                }
            }

            // @TODO - Need region param
            var kernelManager = new KernelManager(Region.NTSC, ProgramType);
            kernelManager.GenerateCode(out var kernelCode, out var kernelInitCode);

            if (kernelInitCode != null)
            {
                vblankCodeBuilder.AppendLine();
                vblankCodeBuilder.AppendLine("// Generated kernel initialization code:");
                vblankCodeBuilder.AppendLine(kernelInitCode);
            }
            
// @TODO - Desperately need to prettify this file (indenting), and also open it in text editor on build (or expose option to).
            return
$@"// <auto-generated>
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
{kernelCode}

            // Overscan
            byte lines = 30;
			while (lines != 0)
			{{
                lines--;
				WSync();
			}}
        }}
    }}
}}".PrettifyCSharp();
        }
    }
}
