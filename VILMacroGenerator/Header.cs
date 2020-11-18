#nullable enable
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VILMacroGenerator
{
    internal sealed class Header
    {
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
        public InstructionParamType InstructionParam { get; set; } = InstructionParamType.Single;

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
                    if (text.EndsWith(")"))
                    {
                        // @PUSH=someTypeFunc();someSizeFunc(foo)
                        // This probably doesn't support nested calls.
                        var funcName = text.Substring(0, text.IndexOf("("));
                        var paramsString = text.Substring(text.IndexOf("(") + 1);
                        paramsString = paramsString.Substring(0, paramsString.Length - 1);
                        var funcParams = paramsString.Split(',').Select(ParseParam).ToArray();
                        return new PushParam(new Function(funcName, funcParams));
                    }
                    else if (text.StartsWith("type["))
                        return new PushParam(new StackAccess(true, Convert.ToInt32(text[text.Length - 2].ToString())));
                    else if (text.StartsWith("size["))
                        return new PushParam(new StackAccess(false, Convert.ToInt32(text[text.Length - 2].ToString())));
                    else if (text.EndsWith("]"))
                        throw new InvalidOperationException($"Failed to parse param '{text}'. Stack array access must be in the form of 'type[n]' or 'size[n]'.");
                    else if (TryGetBuiltInType(text, out var typeLabel))
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
            return header;

            static bool TryGetBuiltInType(string value, out string typeLabel)
            {
                if (value.Equals("byte", StringComparison.CurrentCultureIgnoreCase))
                {
                    typeLabel = "new TypeLabel(BuiltInDefinitions.Byte)";
                    return true;
                }
                else if (value.Equals("bool", StringComparison.CurrentCultureIgnoreCase))
                {
                    typeLabel = "new TypeLabel(BuiltInDefinitions.Bool)";
                    return true;
                }
                typeLabel = "";
                return false;
            }
        }
    }
}
