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
        public VariableNameAlreadyUsedException(VariableInfo AlreadyExists, VariableInfo NewVariable)
            : base($"Attempted to create variable [{NewVariable}] that conflicts with the name of an existing variable [{AlreadyExists}].")
        {

        }
    }

    internal class HeapOverflowException : FatalCompilationException
    {
        public HeapOverflowException(string Cause, Type CauseType, int OldSize, int NewSize)
            : base($"Attempted to allocate space for variable [{CauseType} {Cause}], causing heap to overflow from {OldSize.ToString("X4")} to {NewSize.ToString("X4")}")
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
