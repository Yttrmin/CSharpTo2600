#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Text;

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

    internal static class StringExtensions
    {
        public static string PrettifyCSharp(this string @this)
        {
            var builder = new StringBuilder(@this.Length);
            var indent = 0;
            const int deltaIndex = 4;
            foreach (var line in @this.Split(Environment.NewLine, StringSplitOptions.TrimEntries))
            {
                if (line.Contains('}'))
                    indent -= deltaIndex;
                builder.AppendLine($"{new string(' ', indent)}{line}");
                if (line.Contains('{'))
                    indent += deltaIndex;
            }
            return builder.ToString();
        }
    }
}
