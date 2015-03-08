﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CSharpTo2600.Compiler
{
    // Could be public, but will lead to a bunch of other classes becoming public
    // by neccessity.
    internal partial class ROMBuilder
    {
        public class ROMInfo
        {
            private readonly ROMBuilder ROMBuilder;
            public IEnumerable<GlobalVariable> GlobalVariables
            {
                get
                {
                    return ROMBuilder.VariableManager.GetLocalScopeVariables()
                        .Cast<GlobalVariable>().Where(v => v.EmitToFile);
                }
            }

            public IEnumerable<Subroutine> Subroutines { get; }

            internal ROMInfo(ROMBuilder ROMBuilder)
            {
                this.ROMBuilder = ROMBuilder;
                this.Subroutines = ROMBuilder.Subroutines.ToImmutableArray();
            }
        }
    }
}