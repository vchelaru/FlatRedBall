using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;

namespace CodeTranslator.Parsers
{

    public enum CodeType
    {
        Unknown,
        Type,
        Variable,
        Operation,
        ConditionalKeyword,
        Keyword,
        Constant,
        Primitive,
        MethodCall,
        Namespace,
        VariableDeclaration,
        Delegate,
        ConsolidatedMethodContents,
        Constructor
    }

    public enum TypeReturn
    {
        ListType,
        ElementType
    }
    
    public class CodeItem
    {
        public string Code;
        public CodeType CodeType;

        public CodeItem ParentCodeItem;
        public CodeItem ChildCodeItem;

        public CodeItem PairedSquareBracket;

        public bool IsAssigned = false;
        public bool IsCompared = false;
        public bool IsProperty = false;

        public ParsedClass ParsedClass;
        public ParsedType ParsedType;

        public bool HasSpaceAfter = true;

        public bool IsArray
        {
            get
            {
                if (ParsedType == null)
                {
                    return false;
                }
                if (ParsedType.RuntimeType != null)
                {
                    return ParsedType.RuntimeType.IsArray;
                }
                else
                {
                    return ParsedType.ToString().Contains("[") && ParsedType.ToString().Contains("]");
                }
            }
        }

        public bool IsListOrArray
        {
            get
            {
                if (ParsedType != null)
                {
                    return ParsedType.IsListOrArray;
                }
                else
                {
                    return Code.Contains("get(") || Code.Contains("[");
                }
            }

        }

        public bool IsList
        {
            get
            {
                if (ParsedType != null)
                {
                    return ParsedType.IsList;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsListButNotFrb
        {
            get
            {
                if (ParsedType != null)
                {
                    return ParsedType.IsListButNotFrb;
                }
                else
                {
                    return Code.Contains("get(") || Code.Contains("[");
                }
            }
        }

        public CodeItem Clone()
        {
            CodeItem newItem = this.MemberwiseClone() as CodeItem;

            return newItem;

        }

        public Type GetEvaluatedTypeOrElementInList(TypeReturn typeReturn)
        {

            if (ParsedType.OldParsedType != null)
            {
                if (IsListOrArray && typeReturn == TypeReturn.ElementType)
                {
                    return TypeManager.GetTypeInListFromParsedType(ParsedType.OldParsedType);

                }
                else
                {
                    return TypeManager.GetTypeFromParsedType(ParsedType.OldParsedType);
                }
            }
            else
            {
                if (IsListOrArray && typeReturn == TypeReturn.ElementType)
                {
                    return TypeManager.GetTypeInListFromParsedType(ParsedType);
                }
                else
                {
                    return TypeManager.GetTypeFromParsedType(ParsedType);

                }
            }
        }

        public Type GetEvaluatedType()
        {
            if (ParsedType.OldParsedType != null)
            {

                    return TypeManager.GetTypeFromParsedType(ParsedType.OldParsedType);
            }
            else
            {

                    return TypeManager.GetTypeFromParsedType(ParsedType);
            }

        }

        public string GetCodeItemChain()
        {
            string returnValue = this.Code;

            if (ParentCodeItem != null)
            {
                returnValue = ParentCodeItem.GetCodeItemChain() + "." + returnValue;
            }

            return returnValue;
        }

        public CodeItem TopParentCodeItem
        {
            get
            {
                if (ParentCodeItem != null)
                {
                    return ParentCodeItem.TopParentCodeItem;
                }
                else
                {
                    return this;
                }
            }
        }

        public override string ToString()
        {
            return Code + " <" + CodeType.ToString() + ">";
        }
    }
}
