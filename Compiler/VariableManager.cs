using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTo2600.Compiler
{
    internal abstract class VariableManager
    {
        private readonly Dictionary<string, VariableInfo> Variables;
        private readonly VariableManager Parent;

        protected VariableManager()
        {
            Variables = new Dictionary<string, VariableInfo>();
        }

        public abstract void AddVariable(string Name, Type Type);

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
                    throw new FatalCompilationException("Could not find accessible variable with name: \{Name}");
                }
            }
        }

        public IEnumerable<VariableInfo> AllVariables()
        {
            return Variables.Values;
        }

        //@TODO - I'd love this to be immutable.
        protected void Add(VariableInfo Variable)
        {
            Variables[Variable.Name] = Variable;
        }
    }

    internal sealed class GlobalVariableManager : VariableManager
    {
        private readonly Range RAMRange;
        private int NextVariableStart;

        public GlobalVariableManager(Range RAMRange)
        {
            this.RAMRange = RAMRange;
            NextVariableStart = RAMRange.Start;
        }

        public override void AddVariable(string Name, Type Type)
        {
            Fragments.VerifyType(Type);
            var Address = new Range(NextVariableStart, NextVariableStart + Marshal.SizeOf(Type) - 1);
            if (!RAMRange.Contains(Address.End))
            {
                throw new FatalCompilationException("Insufficient RAM to add var \{Name} of type \{Type}");
            }
            AddVariable(Name, Type, Address, true);
            NextVariableStart = Address.End + 1;
        }

        public void AddVariable(string Name, Type Type, Range Address, bool EmitToFile)
        {
            Fragments.VerifyType(Type);
            var Variable = new GlobalVariable(Name, Type, Address, EmitToFile);
            Add(Variable);
        }
    }

    internal sealed class LocalVariableManager : VariableManager
    {
        public override void AddVariable(string Name, Type Type)
        {
            throw new NotImplementedException();
        }
    }
}
