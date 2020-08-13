#nullable enable
using System;
using VCSFramework;

namespace VCSCompiler.V2
{
    internal static class AttributeReconstructor
    {
        public static T ReconstructFrom<T>(object[] arguments) where T: Attribute
        {
            // Why can't I just cast from the new attribute directly to T...?
            return (T)Reconstruct();

            Attribute Reconstruct()
            {
                switch (typeof(T).Name)
                {
                    case nameof(OverrideWithStoreToSymbolAttribute):
                        return new OverrideWithStoreToSymbolAttribute((string)arguments[0], (bool)arguments[1]);
                    default:
                        throw new InvalidOperationException($"Attempted to reconstruct unknown attribute '{typeof(T)}'");
                }
            }
        }
    }
}
