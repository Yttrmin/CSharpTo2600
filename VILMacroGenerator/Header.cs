#nullable enable
using Microsoft.CodeAnalysis;
using System;
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
            private readonly object Value;
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
                header.TypeParam = ParseParam(values[0], true);
                header.SizeParam = ParseParam(values[1], false);

                static PushParam ParseParam(string text, bool? isType)
                {
                    if (text.EndsWith(")"))
                    {
                        // @PUSH=someTypeFunc();someSizeFunc(foo)
                        // This probably doesn't support nested calls.
                        var funcName = text.Substring(0, text.IndexOf("("));
                        var paramsString = text.Substring(text.IndexOf("(") + 1);
                        paramsString = paramsString.Substring(0, paramsString.Length - 1);
                        var funcParams = paramsString.Split(',').Select(s => ParseParam(s, null)).ToArray();
                        return new PushParam(new Function(funcName, funcParams));
                    }
                    else if (text.StartsWith("type["))
                        return new PushParam(new StackAccess(true, Convert.ToInt32(text[text.Length - 2].ToString())));
                    else if (text.StartsWith("size["))
                        return new PushParam(new StackAccess(false, Convert.ToInt32(text[text.Length - 2].ToString())));
                    else if (text.EndsWith("]"))
                        throw new InvalidOperationException($"Failed to parse param '{text}'. Stack array access must be in the form of 'type[n]' or 'size[n]'.");
                    else if (isType != null && TryGetBuiltInLabel(text, (bool)isType, out var typeLabel))
                        return new PushParam(typeLabel);
                    else
                        // @PUSH=type;size
                        return new PushParam(text.Capitalize());
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
