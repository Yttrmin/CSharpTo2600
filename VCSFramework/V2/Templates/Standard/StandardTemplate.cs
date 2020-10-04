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
                vblankCodeBuilder.AppendLine($"// Invoke user-provided [{nameof(VBlankAttribute)}] methods, in order of declaration:");
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
            // @TODO - There's actually 2 flags in TimInt. One is the timer interrupt flag, the other is the PA7 edge-detect flag. I have
            // no idea if or when the latter would be set. Should probably look into it more to find out, but at least works for now.
            while (TimInt == 0) ;

            WSync();
            // @TODO - This technically eats into 3 cycles for the first scanline. 
            // Could we somehow ditch the above WSync() and strategically burn just enough cycles to set VBlank=0 when it won't show, then immediately execute kernel?
			VBlank = 0;

            // Call kernel
{kernelCode}

            // Overscan. We're assuming the kernel code ends with `STA WSYNC` `BNE -`. If it's significantly heavier instead, we might have timing issues...
            VBlank = 2;
            // Cycles to kill = 30 * 76 - 14 (LDA/STA timer, WSYNC after timer expire, time to check loop) - 5 (LDA/STA VBLANK) - 2 (BNE not taken) = 2259
            // 2259 / 64 = 35.297, round down for timer and a WSYNC afterwards will clean up the remainder.
            Tim64T = 35;
            
            // @TODO - User overscan code goes here.
            while (TimInt == 0) ;
            WSync();
        }}
    }}
}}".PrettifyCSharp();
        }
    }
}
