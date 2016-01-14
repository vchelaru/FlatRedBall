using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueView.Scripting
{
    public class CodeContext
    {
        public Type ContainerType
        {
            get;
            set;
        }

        public object ContainerInstance
        {
            get;
            set;
        }
        
        List<Dictionary<string, object>> mScopedVariables = new List<Dictionary<string, object>>();

        public List<Dictionary<string, object>> VariableStack
        {
            get { return mScopedVariables; }
        }


        public CodeContext(object containerInstance)
        {
            mScopedVariables.Add(new Dictionary<string, object>());
            ContainerInstance = containerInstance;
        }

        public void AddVariableStack()
        {
            mScopedVariables.Add(new Dictionary<string, object>());
        }

        public void RemoveVariableStack()
        {
            mScopedVariables.RemoveAt(mScopedVariables.Count - 1);
        }

        public void GetVariableInformation(string variableName, out int stackDepth)
        {
            stackDepth = -1;

            for (int i = mScopedVariables.Count - 1; i > -1; i--)
            {
                var dictionary = mScopedVariables[i];

                if (dictionary.ContainsKey(variableName))
                {
                    stackDepth = i;
                    break;
                }
            }
        }
    }
}
