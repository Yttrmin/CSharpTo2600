#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace VCSFramework.V2
{
    internal static class ReflectionExtensions
    {
        public static MethodInfo? GetEntryPoint(this System.Reflection.Assembly assembly)
        {
            if (assembly.EntryPoint != null)
            {
                return assembly.EntryPoint;
            }
            var allMethods = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            return allMethods.SingleOrDefault(m => m.Name.Equals("Main", StringComparison.InvariantCultureIgnoreCase) && !m.GetParameters().Any() && m.ReturnType == typeof(void));
        }
    }
}
