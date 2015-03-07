using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    internal abstract class VariableManager
    {
        private readonly ImmutableDictionary<string, VariableInfo> Variables;
        private readonly VariableManager Parent;

        protected VariableManager(VariableManager Parent)
        {
            Variables = ImmutableDictionary<string, VariableInfo>.Empty;
            this.Parent = Parent;
        }

        protected VariableManager(VariableManager OldThis, VariableInfo NewVariable)
        {
            this.Parent = OldThis.Parent;
            this.Variables = OldThis.Variables.Add(NewVariable.Name, NewVariable);
        }

        public VariableInfo GetVariable(string Name)
        {
            if (Variables.ContainsKey(Name))
            {
                return Variables[Name];
            }
            else
            {
                if (Parent != null)
                {
                    return Parent.GetVariable(Name);
                }
                else
                {
                    throw new FatalCompilationException($"Could not find accessible variable with name: {Name}");
                }
            }
        }

        /// <summary>
        /// Returns only variables in the local scope. Does not return variables in any parent scopes.
        /// </summary>
        public IEnumerable<VariableInfo> GetLocalScopeVariables()
        {
            return Variables.Values;
        }
    }

    internal sealed class GlobalVariableManager : VariableManager
    {
        private readonly Range RAMRange;
        private readonly int NextVariableStart;

        public GlobalVariableManager(Range RAMRange)
            : base(null)
        {
            this.RAMRange = RAMRange;
            NextVariableStart = RAMRange.Start;
        }

        private GlobalVariableManager(GlobalVariableManager Old, GlobalVariable NewVariable)
            : base(Old, NewVariable)
        {
            this.RAMRange = Old.RAMRange;
            // Increase NextVariableStart if this is a normal variable.
            if(NewVariable.EmitToFile)
            {
                this.NextVariableStart = Old.NextVariableStart + NewVariable.Size;
            }
            else
            {
                this.NextVariableStart = Old.NextVariableStart;
            }
        }

        public GlobalVariableManager AddVariable(string Name, Type Type)
        {
            Fragments.VerifyType(Type);
            var Address = new Range(NextVariableStart, NextVariableStart + Marshal.SizeOf(Type) - 1);
            if (!RAMRange.Contains(Address.End))
            {
                throw new FatalCompilationException($"Insufficient RAM to add var {Name} of type {Type}");
            }
            return AddVariable(Name, Type, Address, true);
        }

        public GlobalVariableManager AddVariable(string Name, Type Type, Range Address, bool EmitToFile)
        {
            Fragments.VerifyType(Type);
            var Variable = new GlobalVariable(Name, Type, Address, EmitToFile);
            return new GlobalVariableManager(this, Variable);
        }

		public GlobalVariableManager AddVariable(Symbol Symbol, Type Type)
		{
			Fragments.VerifyType(Type);
			var Address = new Range(Symbol.Value.Value, Symbol.Value.Value + Marshal.SizeOf(Type) - 1);
			return AddVariable(Symbol.Name, Type, Address, false);
		}
    }

    internal sealed class LocalVariableManager : VariableManager
    {
        public LocalVariableManager(VariableManager Parent)
            : base(Parent)
        {

        }

        private LocalVariableManager(LocalVariableManager Old, LocalVariable NewVariable)
            : base(Old, NewVariable)
        {

        }

        public LocalVariableManager AddVariable(string Name, Type Type, Range Address)
        {
            Fragments.VerifyType(Type);
            var Local = new LocalVariable(Name, Type, Address);
            return new LocalVariableManager(this, Local);
        }
    }
}
