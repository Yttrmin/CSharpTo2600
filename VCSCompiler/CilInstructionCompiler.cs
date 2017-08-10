using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Reflection;
using System.Linq;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;
using Mono.Cecil;
using VCSFramework;

namespace VCSCompiler
{
	/// <summary>
	/// Contains the logic for compiling individual CIL instructions to 6502 instructions.
	/// </summary>
    internal class CilInstructionCompiler
    {
		private readonly IImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>> MethodMap;
		private readonly IImmutableDictionary<string, ProcessedType> Types;
	    private readonly MethodDefinition MethodDefinition;

		public CilInstructionCompiler(MethodDefinition methodDefinition, IImmutableDictionary<string, ProcessedType> types)
		{
			MethodMap = CreateMethodMap();
			MethodDefinition = methodDefinition;
			Types = types;
		}

		public IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction) => MethodMap[instruction.OpCode.Code](instruction);

		private IImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>> CreateMethodMap()
		{
			var dictionary = new Dictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>>();
			var typeInfo = typeof(CilInstructionCompiler).GetTypeInfo();
			var opCodes = Enum.GetValues(typeof(Code)).Cast<Code>();
			foreach (var opCode in opCodes)
			{
				var name = Enum.GetName(typeof(Code), opCode);
				if (opCode >= Code.Ldc_I4_0 && opCode <= Code.Ldc_I4_8)
				{
					name = "Ldc_I4";
				}
				else if (opCode >= Code.Ldarg_0 && opCode <= Code.Ldarg_3)
				{
					name = "Ldarg";
				}
				else if (opCode >= Code.Stloc_0 && opCode <= Code.Stloc_3)
				{
					name = "Stloc";
				}
				var method = typeInfo.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
				dictionary[opCode]
					= (Func<Instruction, IEnumerable<AssemblyLine>>)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<AssemblyLine>>), this)
					?? Unsupported;
			}
			return dictionary.ToImmutableDictionary();
		}

	    private bool TryInlineAssemblyCall(Instruction instruction, out IEnumerable<AssemblyLine> assembly)
	    {
		    var methodReference = instruction.Operand as MethodReference;
		    var isFactoryCall = methodReference?.FullName.StartsWith("VCSFramework.Assembly.AssemblyInstruction VCSFramework.Assembly.AssemblyFactory::");
		    if (methodReference != null && isFactoryCall.Value)
		    {
			    if (methodReference.Parameters.Count() == 0)
			    {
				    // Skip the pop that follows.
				    instruction.Next = instruction.Next.Next;
				    var assemblyMethod = typeof(AssemblyFactory).GetTypeInfo().DeclaredMethods.Single(m => m.Name == methodReference.Name);
				    assembly = new[] { (AssemblyInstruction)assemblyMethod.Invoke(null, null) };
				    Console.WriteLine($"{instruction} is an inline assembly call, emitting {assemblyMethod.Name} instead, erasing Pop");
				    return true;
			    }
			    else
			    {
				    throw new NotImplementedException($"Can't process inline assembly call '{methodReference.Name}', it must take 0 parameters.");
			    }
		    }
		    assembly = Enumerable.Empty<AssemblyLine>();
		    return false;
	    }

	    private IEnumerable<AssemblyLine> LoadArgument(Instruction instruction)
	    {
			//TODO - Ldarg, Ldarg_S
		    if (instruction.Operand != null)
		    {
			    throw new NotImplementedException();
		    }
		    switch (instruction.OpCode.Code)
		    {
				case Code.Ldarg_0:
					return LoadArgument(0);
				case Code.Ldarg_1:
					return LoadArgument(1);
				case Code.Ldarg_2:
					return LoadArgument(2);
				case Code.Ldarg_3:
					return LoadArgument(3);
				default:
					throw new NotImplementedException();
		    }
	    }

	    private IEnumerable<AssemblyLine> LoadArgument(int index)
	    {
		    var parameter = MethodDefinition.Parameters[index];
		    yield return LDA(LabelGenerator.GetFromParameter(parameter));
		    yield return PHA();
	    }

		private IEnumerable<AssemblyLine> LoadConstant(Instruction instruction)
		{
			byte value = 0;
			if (instruction.Operand != null)
			{
				try
				{
					value = Convert.ToByte(instruction.Operand);
				}
				catch (OverflowException)
				{
					throw new InvalidInstructionException(instruction, $"Constant value '{instruction.Operand}'must fit in a byte!");
				}
			}
			else
			{
				switch(instruction.OpCode.Code)
				{
					case Code.Ldc_I4_0:
						return LoadConstant(0);
					case Code.Ldc_I4_1:
						return LoadConstant(1);
					case Code.Ldc_I4_2:
						return LoadConstant(2);
					case Code.Ldc_I4_3:
						return LoadConstant(3);
					case Code.Ldc_I4_4:
						return LoadConstant(4);
					case Code.Ldc_I4_5:
						return LoadConstant(5);
					case Code.Ldc_I4_6:
						return LoadConstant(6);
					case Code.Ldc_I4_7:
						return LoadConstant(7);
					case Code.Ldc_I4_8:
						return LoadConstant(8);
				}
			}
			return LoadConstant(value);
		}

		private IEnumerable<AssemblyLine> LoadConstant(byte value)
		{
			yield return LDA(value);
			yield return PHA();
		}

	    private IEnumerable<AssemblyLine> StoreArgument(Instruction instruction)
	    {
		    // Either Starg or Starg_S.
		    var parameter = (ParameterDefinition)instruction.Operand;
			yield return PLA();
		    yield return STA(LabelGenerator.GetFromParameter(parameter));
		}

	    private IEnumerable<AssemblyLine> StoreLocal(Instruction instruction)
	    {
		    if (instruction.Operand != null)
		    {
			    throw new NotImplementedException();
		    }
		    switch (instruction.OpCode.Code)
		    {
				case Code.Stloc_0:
					return StoreLocal(0);
				case Code.Stloc_1:
					return StoreLocal(1);
				case Code.Stloc_2:
					return StoreLocal(2);
				case Code.Stloc_3:
					return StoreLocal(3);
				default:
					throw new NotImplementedException();
		    }
	    }

	    private IEnumerable<AssemblyLine> StoreLocal(int index)
	    {
		    var local = MethodDefinition.Body.Variables[index];
		    yield return PLA();
		    yield return STA(LabelGenerator.GetFromVariable(MethodDefinition, local));
	    }

		private IEnumerable<AssemblyLine> Add(Instruction instruction)
		{
			// TODO - Should probably just allocate a couple address locations instead of trying to use the stack operations.
			yield return PLA();
			yield return STA(LabelGenerator.TemporaryRegister1);
			yield return PLA();
			yield return CLC();
			yield return ADC(LabelGenerator.TemporaryRegister1);
			yield return PHA();
		}

		private IEnumerable<AssemblyLine> Br_S(Instruction instruction)
		{
			yield return JMP(LabelGenerator.GetFromInstruction((Instruction)instruction.Operand));
		}

		private IEnumerable<AssemblyLine> Call(Instruction instruction)
		{
			// Could be either a MethodDefinition or MethodReference.
			MethodReference method = (MethodReference)instruction.Operand;

			var methodDeclaringType = (string)method.DeclaringType.FullName;
			
			if (TryInlineAssemblyCall(instruction, out var inlineAssembly))
			{
				foreach(var assembly in inlineAssembly)
				{
					yield return assembly;
				}
				yield break;
			}

			var processedSubroutine = Types[methodDeclaringType].Subroutines.Single(s => s.FullName == method.FullName);

			// Check if this method should be replaced with a direct store to a symbol (generally a TIA register).
			// Don't directly compare types since we may have received a different Framework assembly than what this library was built against.
			dynamic overrideStore = processedSubroutine.FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(OverrideWithStoreToSymbolAttribute).FullName);
			if (overrideStore != null)
			{
				//TODO - We assume this is a 1-arg void method. Actually enforce this at the processing stage.
				if (method.Parameters.Count != 1)
				{
					throw new NotImplementedException($"{method.Name}, marked with {nameof(OverrideWithStoreToSymbolAttribute)}, must take 1 parameter for now.");
				}
				yield return PLA();
				yield return STA(overrideStore.Symbol);
				yield break;
			}

			// Check if this method should be replaced with a load to a 6502 register.
			dynamic overrideLoad = processedSubroutine.FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(OverrideWithLoadToRegisterAttribute).FullName);
			if (overrideLoad != null)
			{
				//TODO - We assume this is a 1-arg void method. Actually enforce this at the processing stage.
				if (method.Parameters.Count != 1)
				{
					throw new NotImplementedException($"{method.Name}, marked with {nameof(OverrideWithLoadToRegisterAttribute)} must take 1 parameter.");
				}
				yield return PLA();
				switch(overrideLoad.Register)
				{
					case "A":
						break;
					case "X":
						yield return TAX();
						break;
					case "Y":
						yield return TAY();
						break;
					default:
						throw new FatalCompilationException($"Attempted load to unknown register: {overrideLoad.Register}");
				}
				yield break;
			}

			var parameters = ((MethodReference) method).Parameters.ToImmutableArray();
			if (parameters.Any())
			{
				// PLA arguments in reverse off stack and assign to parameters.
				foreach (var parameter in parameters.Reverse())
				{
					yield return PLA();
					yield return STA(LabelGenerator.GetFromParameter(parameter));
				}
			}

			dynamic alwaysInline = processedSubroutine.FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(AlwaysInlineAttribute).FullName);
			if (alwaysInline != null)
			{
				//TODO - Assuming the subroutine is already compiled is dangerous.
				var compiledSubroutine = ((CompiledType)Types[method.DeclaringType.FullName]).Subroutines.Single(s => s.FullName == method.FullName);
				foreach (var assemblyLine in compiledSubroutine.Body)
				{
					//TODO - If the subroutine contains labels you can end up emitting duplicates if the inline subroutine is called more than once. Make them unique.
					//TODO - Once we have branching and multiple return statements this will explode.
					// In reality we probably want to replace RTS with JMP to a label inserted after this method body.
					if (!assemblyLine.Text.Contains("RTS"))
					{
						yield return assemblyLine;
					}
				}
				yield break;
			}

			yield return JSR(LabelGenerator.GetFromMethod(method));
		}

		/// <summary>
		/// Convert value on stack to int8, which it already should be.
		/// </summary>
		private IEnumerable<AssemblyLine> Conv_U1(Instruction instruction) => Enumerable.Empty<AssemblyLine>();

	    private IEnumerable<AssemblyLine> Ldarg(Instruction instruction) => LoadArgument(instruction);

	    private IEnumerable<AssemblyLine> Ldarg_S(Instruction instruction) => LoadArgument(instruction);

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private IEnumerable<AssemblyLine> Ldc_I4(Instruction instruction) => LoadConstant(instruction);

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private IEnumerable<AssemblyLine> Ldc_I4_S(Instruction instruction) => Ldc_I4(instruction);

		private IEnumerable<AssemblyLine> Ldsfld(Instruction instruction)
		{
			var fieldDefinition = (FieldDefinition)instruction.Operand;
			yield return LDA(LabelGenerator.GetFromField(fieldDefinition));
			yield return PHA();
		}

		private IEnumerable<AssemblyLine> Nop(Instruction instruction) => Enumerable.Empty<AssemblyLine>();

		private IEnumerable<AssemblyLine> Ret(Instruction instruction)
		{
			yield return RTS();
		}

	    private IEnumerable<AssemblyLine> Starg(Instruction instruction) => StoreArgument(instruction);

		private IEnumerable<AssemblyLine> Starg_S(Instruction instruction) => StoreArgument(instruction);

	    private IEnumerable<AssemblyLine> Stloc(Instruction instruction) => StoreLocal(instruction);

	    private IEnumerable<AssemblyLine> Stloc_S(Instruction instruction) => StoreLocal(instruction);

		private IEnumerable<AssemblyLine> Stsfld(Instruction instruction)
		{
			yield return PLA();
			var fieldDefinition = (FieldDefinition)instruction.Operand;
			yield return STA(LabelGenerator.GetFromField(fieldDefinition));
		}

		private IEnumerable<AssemblyLine> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);
	}
}
