using System;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public class FatalCompilationException : Exception
    {
        public FatalCompilationException(string Message) : base(Message)
        {

        }

        public FatalCompilationException(string Message, SyntaxNode Node)
            : this($"(Line #{Node.SyntaxTree.GetLineSpan(Node.Span).StartLinePosition.Line}): {Message}")
        {

        }
    }

    internal class VariableNameAlreadyUsedException : FatalCompilationException
    {
        public VariableNameAlreadyUsedException(IVariableInfo AlreadyExists, IVariableInfo NewVariable)
            : base($"Attempted to create variable [{NewVariable.Name}] that conflicts with the name of an existing variable [{AlreadyExists.Name}].")
        {

        }
    }

    internal class GlobalMemoryOverflowException : FatalCompilationException
    {
        public GlobalMemoryOverflowException(int MemoryUsage, int GlobalUsageLimit)
            : base($"Too many globals, {MemoryUsage} bytes needed, but only {GlobalUsageLimit} bytes available.")
        {

        }
    }

    internal class GameClassNotFoundException : FatalCompilationException
    {
        public GameClassNotFoundException()
            : base("Could not find class marked with [Atari2600Game] attribute.")
        {

        }
    }

    internal class GameClassNotStaticException : FatalCompilationException
    {
        public GameClassNotStaticException(Type GameClass)
            : base($"Marked game class \"{GameClass}\" must be static.")
        {

        }
    }
}
