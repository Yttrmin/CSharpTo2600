#nullable enable
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VILMacroGenerator
{
    internal sealed class Header
    {
        private readonly static Regex DoubleQuoteRegex = new Regex(@""".* """);

        private struct Function
        {
            public string Name { get; }
            public PushParam[] Parameters { get; }

            public Function(string name, PushParam[] parameters)
            {
                Name = name;
                Parameters = parameters;
            }
        }

        private struct StackAccess
        {
            public bool IsType { get; }
            public int Index { get; }

            public StackAccess(bool isType, int index)
            {
                IsType = isType;
                Index = index;
            }
        }

        public class PushParam
        {
            public readonly object Value;
            public string CSharpCode => Value switch
            {
                Function function => $"new {function.Name.Capitalize()}({string.Join(", ", function.Parameters.Select(p => p.CSharpCode))})",
                string param => param,
                StackAccess stackAccess => stackAccess.IsType ? $"new StackTypeArrayAccess({stackAccess.Index})" : $"new StackSizeArrayAccess({stackAccess.Index})",
                _ => throw new InvalidOperationException($"Unknown param: {Value}")
            };

            public PushParam(object value)
            {
                Value = value;
            }
        }

        public enum InstructionParamType
        {
            None,
            Single,
            Multiple
        }

        public PushParam? TypeParam { get; private set; }
        public PushParam? SizeParam { get; private set; }
        public int PopCount { get; set; }
        public int ReservedBytes { get; private set; }
        public InstructionParamType InstructionParam { get; private set; } = InstructionParamType.Single;
        public string? DeprecatedString { get; private set; }
        public bool TypeFirst { get; private set; } = true;

        public static Header? Parse(string generateLine, GeneratorExecutionContext context)
        {
            var header = new Header();
            var parts = generateLine.Split(' ');

            var pushStr = parts.SingleOrDefault(p => p.StartsWith("@PUSH=", StringComparison.CurrentCultureIgnoreCase));
            if (pushStr != null && char.IsDigit(pushStr.Last()))
            {
                var desc = new DiagnosticDescriptor(MacroGenerator.OldPushFormatId, "Old @PUSH format", "Specifying a number for @PUSH isn't supported anymore, line: {0}", MacroGenerator.VilCategory, DiagnosticSeverity.Error, true);
                context.ReportDiagnostic(Diagnostic.Create(desc, null, generateLine));
                return null;
            }
            if (pushStr != null)
            {
                var values = pushStr.Substring(6).Split(';');
                header.TypeParam = ParseParam(values[0]);
                header.SizeParam = ParseParam(values[1]);

                static PushParam ParseParam(string text)
                {
                    if (text.Contains('(') && text.Contains(')'))
                    {
                        // @PUSH=someTypeFunc();someSizeFunc(foo)
                        var funcName = text.Slice(-1, text.IndexOf('('));
                        var paramsString = text.Slice(text.IndexOf('('), text.LastIndexOf(')'));
                        
                        // Pair up parentheses, in order to deal with nested function calls.
                        var openingParenthesesStack = new Stack<int>();
                        var parenthesesPairs = new List<(int Open, int Close)>();
                        for (var i = 0; i < paramsString.Length; i++)
                        {
                            if (paramsString[i] == '(')
                                openingParenthesesStack.Push(i);
                            else if (paramsString[i] == ')')
                                parenthesesPairs.Add((openingParenthesesStack.Pop(), i));
                        }
                        if (openingParenthesesStack.Count != 0)
                            throw new InvalidOperationException($"Parentheses mismatch in parameter string: {paramsString}");

                        // Find all split points: commas that don't belong to nested functions, and first/last chars of the whole string.
                        var commaSeparators = paramsString.Select((c, i) => (c, i))
                            .Where(t => t.c == ',' && !parenthesesPairs.Any(p => p.Open < t.i && t.i < p.Close))
                            .Select(t => t.i)
                            .Prepend(-1).Append(paramsString.Length);

                        // Parse each param.
                        var funcParams = commaSeparators.Zip(commaSeparators.Skip(1), (a, b) => (a, b))
                            .Select(t => paramsString.Slice(t.a, t.b))
                            .Select(ParseParam).ToArray();
                        return new PushParam(new Function(funcName, funcParams));
                    }
                    else if (text.StartsWith("type["))
                        return TypeSizeLabelLookup(true, text.Slice(text.IndexOf('['), text.IndexOf(']')));
                    else if (text.StartsWith("size["))
                        return TypeSizeLabelLookup(false, text.Slice(text.IndexOf('['), text.IndexOf(']')));
                    else
                        // @PUSH=type;size
                        return new PushParam(text.Capitalize());

                    static PushParam TypeSizeLabelLookup(bool isType, string contents)
                    {
                        if (int.TryParse(contents, out var index))
                            return new PushParam(new StackAccess(isType, index));
                        else if (TryGetBuiltInLabel(contents, isType, out var label))
                            return new PushParam(label);
                        else
                            throw new ArgumentException($"Type/Size lookup should be a stack index or built-in type, instead got: {contents}");
                    }
                }
            }

            var popStr = parts.SingleOrDefault(p => p.StartsWith("@POP=", StringComparison.CurrentCultureIgnoreCase));
            if (popStr != null)
            {
                header.PopCount = Convert.ToInt32(popStr.Last().ToString());
            }

            if (parts.Any(p => p.Equals("@NOINSTPARAM", StringComparison.CurrentCultureIgnoreCase)))
                header.InstructionParam = InstructionParamType.None;
            else if (parts.Any(p => p.Equals("@MULTIINSTPARAM", StringComparison.CurrentCultureIgnoreCase))
                || parts.Any(p => p.Equals("@COMPOSITE", StringComparison.CurrentCultureIgnoreCase)))
                header.InstructionParam = InstructionParamType.Multiple;
            else if (parts.Any(p => p.Equals("@SIZEFIRST")))
                header.TypeFirst = false;

            var reserveString = parts.SingleOrDefault(p => p.StartsWith("@RESERVED=", StringComparison.CurrentCultureIgnoreCase));
            header.ReservedBytes = reserveString != null ? Convert.ToInt32(reserveString.Last().ToString()) : 0;

            var deprecatedPart = parts.SingleOrDefault(p => p.StartsWith("@DEPRECATED", StringComparison.CurrentCultureIgnoreCase));
            var deprecatedMatch = deprecatedPart != null ? DoubleQuoteRegex.Match(deprecatedPart) : null;
            header.DeprecatedString = deprecatedMatch?.Success == true ? deprecatedMatch.Value : (deprecatedPart != null ? "" : null);
            return header;

            static bool TryGetBuiltInLabel(string value, bool isType, out string label)
            {
                const string longPointerName = "longPtr";
                if (value.Equals("byte", StringComparison.CurrentCultureIgnoreCase))
                {
                    label = $"new {(isType ? "TypeLabel" : "TypeSizeLabel")}(BuiltInDefinitions.Byte)";
                    return true;
                }
                else if (value.Equals("bool", StringComparison.CurrentCultureIgnoreCase))
                {
                    label = $"new {(isType ? "TypeLabel" : "TypeSizeLabel")}(BuiltInDefinitions.Bool)";
                    return true;
                }
                else if (value.Equals(longPointerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (isType)
                        throw new InvalidOperationException($"'{longPointerName}' can only be used for the size param of a @PUSH. Use 'GetPointerFromType()' for the type param.");
                    label = "new PointerSizeLabel(false)";
                    return true;
                }
                label = "";
                return false;
            }
        }
    }
}
