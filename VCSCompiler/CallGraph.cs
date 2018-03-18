using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace VCSCompiler
{
	internal sealed class CallGraph : Graph<MethodDefinition>
	{
		public static CallGraph CreateFromEntryMethod(ProcessedSubroutine entryPoint)
		{
			var graph = new CallGraph();
			graph.AddRootNode(entryPoint.MethodDefinition);
			AddCalleesToGraph(entryPoint.MethodDefinition);
			return graph;

			void AddCalleesToGraph(MethodDefinition method)
			{
				var callees = method.Body.Instructions
					.Where(i => i.OpCode == OpCodes.Call)
					.Select(i => i.Operand)
					.OfType<MethodDefinition>();

				foreach (var callee in callees)
				{
					graph.AddEdge(method, callee);
					AddCalleesToGraph(callee);
				}
			}
		}

		public string Print(MethodDefinition start)
		{
			var stringBuilder = new StringBuilder();
			var rootNode = AllNodes.Single(n => n.Value == start);
			PrintInternal(rootNode, stringBuilder, 0);
			return stringBuilder.ToString();

			void PrintInternal(Node<MethodDefinition> node, StringBuilder builder, int indentLevel)
			{
				builder.AppendLine($"{new string('-', indentLevel)}{node.Value.Name}");
				foreach (var child in node.Neighbors)
				{
					PrintInternal(child, builder, indentLevel + 1);
				}
			}
		}
	}
}