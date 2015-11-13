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

    internal class VariableNameReservedException : FatalCompilationException
    {
        public VariableNameReservedException(IFieldSymbol Symbol)
            : base($"Attempted to create variable [{Symbol.ToDisplayString()}] that uses a reserved name (see ReservedSymbols).")
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
        public GameClassNotStaticException(INamedTypeSymbol GameClass)
            : base($"Marked game class \"{GameClass}\" must be static.")
        {

        }
    }

    internal class AttemptedToInvokeSpecialMethodException : FatalCompilationException
    {
        public AttemptedToInvokeSpecialMethodException(Subroutine SubroutineInfo, string From)
            : base($"Attempted to invoke the special ({SubroutineInfo.Type}) method \"{SubroutineInfo.Name}\" from method \"{From}\".")
        {

        }
    }

    internal class RecursionException : FatalCompilationException
    {
        public RecursionException(IMethodSymbol RecursiveMethod)
            : base($"{RecursiveMethod.Name} is possibly recursive. Recursion is not allowed.")
        {

        }
    }
}
