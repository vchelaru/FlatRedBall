using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedType
    {
        #region Fields

        public string Name;

        public Type RuntimeType;

        public ParsedType GenericType;

        public bool IsInterface;

        ParsedType mOldParsedType;

        public int NumberOfElements = 0;

        public List<string> GenericRestrictions = new List<string>();

        public bool IsListOrArray
        {
            get
            {
                if (RuntimeType != null)
                {
                    Type[] interfaces = RuntimeType.GetInterfaces();

                    foreach (Type type in interfaces)
                    {
                        if (type.Name.Contains("IList"))
                        {
                            return true;
                        }
                    }

                    return RuntimeType.IsArray;
                        
                }
                else
                {
                    return IsList ||
                        Name.Contains("[") || Name.Contains("<");

                }
            }
        }

        public bool IsListButNotFrb
        {
            get
            {
                return this.Name == "ArrayList" ||
                    this.Name == "List" ||
                    this.Name == "List`1" ||
                    this.Name == "IAttachableRemovable" ||
                    this.Name == "IAttachableRemovable`1"
                    ;
            }
        }

        public bool IsList
        {
            get
            {
                return IsListButNotFrb ||
                    this.Name == "FlatRedBall.Math.PositionedObjectList<T>" ||
                    this.Name == "FlatRedBall.Math.PositionedObjectList`1" ||
                    this.Name == "PositionedObjectList" ||
                    this.Name == "PositionedObjectList`1" ||
                    this.Name == "AttachableList" ||
                    this.Name == "SpriteList" ||
                    this.Name == "PositionedModelList" ||
                    this.Name == "InstructionList" ||
                    this.Name == "AnimationChain" ||
                    this.Name == "AnimationChainList"
                    ;
            }
        }

        #endregion

        #region Properties

        public string NameWithGenericNotation
        {
            get
            {
                if (GenericType == null)
                {
                    return Name;
                }
                else
                {


                    return Name + "`" + GenericType.NumberOfElements;
                }

            }
        }

        public ParsedType OldParsedType
        {
            get { return mOldParsedType; }
            set { mOldParsedType = value; }
        }

        public bool IsPrimitive
        {
            get
            {
                return IsPrimitiveType(Name);
            }
        }

        public static bool IsPrimitiveType(string typeString)
        {
            return typeString == "int" || typeString == "float"  ||
                typeString == "double" || typeString == "string" ||
                typeString == "byte"   || typeString == "bool"   ||
                typeString == "char" || typeString == "decimal";
        }

        public bool IsPrimitiveArray
        {
            get
            {
                if (!Name.Contains("["))
                {
                    return false;
                }

                string withoutBrackets = Name.Substring(0, Name.IndexOf("["));


                return IsPrimitiveType(withoutBrackets);

            }
        }


        #endregion

        #region Methods

        private ParsedType()
        {

        }

        public ParsedType(string entireString)
        {
            if (entireString == null)
            {
                return;
            }

            if (entireString.Contains("<") && entireString.Contains(">"))
            {
                int genericOpenIndex = entireString.IndexOf('<');
                int genericCloseIndex = entireString.LastIndexOf('>');

                NumberOfElements = 1;

                Name = entireString.Substring(0, genericOpenIndex);
                string genericContents = entireString.Substring(genericOpenIndex + 1, genericCloseIndex - genericOpenIndex - 1);

                GenericType = new ParsedType(genericContents);
            }
            else
            {
                if (entireString.Contains(","))
                {
                    NumberOfElements = entireString.Split(',').Length;
                }
                else
                {
                    NumberOfElements = 1;
                }

                Name = entireString;
            }
            if (Name.StartsWith("I") && Name.Length > 1 && Char.IsUpper(Name[1]))
            {
                IsInterface = true;
            }        
        
        }

        public ParsedType Clone()
        {
            ParsedType typeToReturn = new ParsedType();
            typeToReturn.Name = Name;
            typeToReturn.IsInterface = IsInterface;
            typeToReturn.NumberOfElements = NumberOfElements;

            if (GenericType != null)
            {
                typeToReturn.GenericType = GenericType.Clone();
            }

            typeToReturn.GenericRestrictions = new List<string>();
            typeToReturn.GenericRestrictions.AddRange(GenericRestrictions);

            return typeToReturn;
        }

        public void GetAllTypesAsStrings(List<string> listToFill)
        {
            GetAllTypesAsStrings(listToFill, false);
        }

        public void GetAllTypesAsStrings(List<string> listToFill, bool useGenericRotation)
        {
            if (useGenericRotation)
            {
                listToFill.Add(NameWithGenericNotation);
            }
            else
            {
                listToFill.Add(Name);
            }

            if (GenericType != null)
            {
                GenericType.GetAllTypesAsStrings(listToFill, useGenericRotation);
            }

        }

        public string ToStringNoGeneric()
        {
            return Name;
        }

        public override string ToString()
        {
            if (GenericType == null)
            {
                return Name;
            }
            else
            {
                return Name + "<" + GenericType.ToString() + ">";
            }
        }

        #endregion

        internal void Unqualify()
        {
            if (this.Name.Contains(','))
            {
                string[] split = Name.Split(',');
                this.Name = "";


                for (int i = 0; i < split.Length; i++)
                {
                    string typeString = split[i];

                    if (typeString.Contains('.'))
                    {
                        int startIndex = typeString.LastIndexOf('.') + 1;
                        typeString = typeString.Substring(startIndex, typeString.Length - startIndex);
                    }

                    Name += typeString;

                    if (i != split.Length - 1)
                    {
                        Name += ',';
                    }
                }
            }
            else if(Name.Contains('.'))
            {
                
                int startIndex = Name.LastIndexOf('.') + 1;
                Name = Name.Substring(startIndex, Name.Length - startIndex);
            }
            

            if (GenericType != null)
            {
                GenericType.Unqualify();
            }
        }
    }
}
