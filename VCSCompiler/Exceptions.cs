using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    public class FatalCompilationException : Exception
    {
		public FatalCompilationException(string message) : base(message) { }
    }
}
