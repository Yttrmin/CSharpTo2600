using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("VCSCompiler")]

namespace VCSFramework
{
	[AttributeUsage(AttributeTargets.Method)]
    internal class CompilerImplementedAttribute : Attribute
	{

	}
}
