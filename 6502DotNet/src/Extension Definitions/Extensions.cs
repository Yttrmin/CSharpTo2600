﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    public static class String_Extension
    {
        /// <summary>
        /// Tests whether the string is enclosed in single or double quotes.
        /// </summary>
        /// <param name="s">The string to evaluate.</param>
        /// <returns><c>True</c> if string is fully enclosed in quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInQuotes(this string s)
            => (s[0] == '"' && s[^1] == '"') ||
               (s[0] == '\'' && s[2] == '\'');

        /// <summary>
        /// Tests whether the string is enclosed in double quotes.
        /// </summary>
        /// <param name="s">The string to evaluate.</param>
        /// <returns><c>True</c> if string is fully enclosed in double quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInDoubleQuotes(this string s)
            => s[0] == '"' && s[^1] == '"';

        /// <summary>
        /// Trims one instance of the specified character at the start of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimStartOnce(this string str, char c)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.First().Equals(c))
                return str.Length > 1 ? str.Substring(1) : string.Empty;
            return str;
        }

        /// <summary>
        /// Trims one instance of the specified character at the end of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimEndOnce(this string str, char c)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.Last().Equals(c))
                return str.Length > 1 ? str.Substring(0, str.Length - 1) : string.Empty;
            return str;
        }

        /// <summary>
        /// Trims one instance of the specified character at the start and the end of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimOnce(this string str, char c) => str.TrimStartOnce(c).TrimEndOnce(c);
    }

    public static class Char_Extension
    {
        /// <summary>
        /// Indicates whether the specified Unicode character could be an operand or 
        /// mathematical operator per usage in an expression.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is of this type, <c>false</c> otherwise.</returns>
        public static bool IsSpecialOperator(this char c) => c == '*' || c == '-' || c == '+';

        /// <summary>
        /// Indicates whether the specified Unicode character is a group operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a group operator, <c>false</c> otherwise.</returns>
        public static bool IsOpenOperator(this char c) => c == '(' || c == '[' || c == '{';

        /// <summary>
        /// Indicates whether the specified Unicode character is a group enclosing operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a group enclosing operator, <c>false</c> otherwise.</returns>
        public static bool IsClosedOperator(this char c) => c == ')' || c == ']' || c == '}';

        /// <summary>
        /// Indicates whether the specified Unicode character is a unary operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a unary operator, <c>false</c> otherwise.</returns>
        public static bool IsUnaryOperator(this char c) => c == '-' || c == '+' || c == '<' || c == '>' || c == '^' || c == '!' || c == '&' || c == '~';// || c == '$' || c == '%';

        /// <summary>
        /// Indicates whether the specified Unicode character is a separator operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a separator, <c>false</c> otherwise.</returns>
        public static bool IsSeparator(this char c) => c == ',' || c == ':';

        /// <summary>
        /// Indicates whether the specified Unicode character is a radix operator.
        /// </summary>
        /// <returns><c>true</c>, if the character is a radix operator, <c>false</c> otherwise.</returns>
        /// <param name="c">The Unicode character.</param>
        public static bool IsRadixOperator(this char c) => c == '$' || c == '%';
    }

    public static class Int64_Extension
    {
        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this long value)
        {
            if (value < 0)
                value = (~value) << 1;

            if ((value & 0xFFFFFF00) == 0) return 1;
            if ((value & 0xFFFF0000) == 0) return 2;
            if ((value & 0xFF000000) == 0) return 3;
            return 4;
        }
    }

    public static class Double_Extension
    {
        /// <summary>
        /// Returns a value indicating whether this double is almost equal with great
        /// precision to a specified <see cref="double"/>. 
        /// </summary>
        /// <returns><c>true</c>, if the two values are almost equal, 
        /// <c>false</c> otherwise.</returns>
        /// <param name="d1">This double.</param>
        /// <param name="obj">A double-precision floating point object.</param>
        public static bool AlmostEquals(this double d1, double obj) =>
            Math.Abs(d1 - obj) <= Math.Max(Math.Abs(d1), Math.Abs(obj)) * 1E-15;

        /// <summary>
        /// Returns a value indicating whether this double is an integer.
        /// </summary>
        /// <param name="d1">This double.</param>
        /// <returns><c>true</c>, if the double is (almost) equal to its integral equivalent,
        /// <c>false</c> otherwise. </returns>
        public static bool IsInteger(this double d1) =>
            d1.AlmostEquals((long)d1);
    }

    public static class IEnumerableTExtension
    {
        /// <summary>
        /// Get the iterator for the <see cref="IEnumerable{T}"/> collection.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>An iterator.</returns>
        public static RandomAccessIterator<T> GetIterator<T>(this IEnumerable<T> collection)
            => new RandomAccessIterator<T>(collection);


        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc)
            => ToString(byteCollection, pc, '>', true);

        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <param name="startChar">The starting character.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc, char startChar)
            => ToString(byteCollection, pc, startChar, true);

        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <param name="appendStartPc">If true, the string will append the initial Program 
        /// Counter passed in the <paramref name="pc"/> parameter.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc, bool appendStartPc)
            => ToString(byteCollection, pc, '>', appendStartPc);

        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <param name="startChar">The starting character.</param>
        /// <param name="appendStartPc">If true, the string will append the initial Program 
        /// Counter passed in the <paramref name="pc"/> parameter.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc, char startChar, bool appendStartPc)
        {
            var sb = new StringBuilder();
            if (appendStartPc)
                sb.Append($"{startChar}{pc:x4}    ");
            var byteList = byteCollection.ToList();
            var rows = byteList.Count / 8;
            if (rows == 0)
                rows = 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int offset = y * 8 + x;
                    if (offset >= byteList.Count)
                        break;
                    sb.Append($" {byteList[offset]:x2}");
                }
                if (y < rows - 1)
                {
                    pc += 8;
                    sb.AppendLine();
                    sb.Append($"{startChar}{pc:x4}    ");
                }
            }
            return sb.ToString();
        }
    }
}
