#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VILMacroGenerator
{
    internal static class Extensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> @this, T toAppend)
        {
            foreach (var element in @this)
                yield return element;
            yield return toAppend;
        }

        public static string BuildString(this IEnumerable<char> @this)
            => new string(@this.ToArray());

        public static string Capitalize(this string @this)
            => $"{@this.First().ToString().ToUpper()}{@this.Substring(1)}";

        public static string DelimitIfAny(this string @this, string delimiter = ", ")
            => @this.Any() ? delimiter : string.Empty;

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> @this, T toPrepend)
        {
            yield return toPrepend;
            foreach (var element in @this)
                yield return element;
        }

        /// <summary>Returns the substring between 2 exclusive indices.</summary>
        public static string Slice(this string @this, int start, int end)
            => @this.Substring(start + 1, end - start - 1);
    }
}
