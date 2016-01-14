using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueView.Scripting
{
    public class StackVariableReference : IAssignableReference
    {
        Type mExplicitlySetType;

        public int StackIndex
        {
            get;
            set;
        }

        public string VariableName
        {
            get;
            set;
        }

        public CodeContext CodeContext
        {
            get;
            set;
        }

        public object CurrentValue
        {
            get
            {
                return CodeContext.VariableStack[StackIndex][VariableName];
            }
            set
            {
                CodeContext.VariableStack[StackIndex][VariableName] = value;

            }
        }
        public Type TypeOfReference
        {
            get
            {
                if (mExplicitlySetType != null)
                {
                    return mExplicitlySetType;
                }
                else
                {
                    object fromDict = CodeContext.VariableStack[StackIndex][VariableName];
                    if (fromDict != null)
                    {
                        return fromDict.GetType();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            set
            {
                mExplicitlySetType = value;
            }
        }
    }
}
