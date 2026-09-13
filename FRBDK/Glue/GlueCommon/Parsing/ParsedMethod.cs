using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedMethod
    {
        #region Fields

        string mName;

        string mOldName;

        public Scope Scope;
        public ParsedType Type;




        public bool IsStatic;

        public bool IsOverload;

        public List<ParsedField> ArgumentList = new List<ParsedField>();
        public List<ParsedType> GenericTypes = new List<ParsedType>();
        public string MethodContents;

        public ParsedMethod BaseCall;

        #endregion

        #region Properties

        public string Name
        {
            get
            {
                return mName;
            }
            set
            {
                if (value != null && value.Contains("<"))
                {
                    int indexOfLessThan = value.IndexOf("<");

                    mName = value.Substring(0, indexOfLessThan);

                    string remainderOfStuff =
                        value.Substring(indexOfLessThan, value.Length - indexOfLessThan);

                    // get rid of < and >
                    remainderOfStuff = remainderOfStuff.Substring(1, remainderOfStuff.Length - 2);

                    string[] generics = remainderOfStuff.Split(',');

                    foreach (string generic in generics)
                    {
                        GenericTypes.Add(new ParsedType(generic));
                        //GenericTypes.Add(new ParsedField(

                    }
                }
                else
                {


                    mName = value;
                }
            }
        }

        public string OldName
        {
            get { return mOldName; }
        }

        public int StartIndex
        {
            get;
            set;
        }

        public int EndIndex
        {
            get;
            set;
        }

        #endregion

        public void FillGenericRestrictions(string lineOfCode)
        {
            FillGenericRestrictions(lineOfCode, GenericTypes, ArgumentList);
        }

        static char[] splitChars = new char[] { ',' };
        static List<string> constraintsToUse = new List<string>();
        public static void FillGenericRestrictions(string lineOfCode, List<ParsedType> GenericTypes, List<ParsedField> ArgumentList)
        {
            // Vic says - this only handles one constraint on one generic type.
            // If it gets more complicated than that, this needs to be modified.

            if (GenericTypes.Count != 0 && lineOfCode.Contains("where "))
            {
                int m = 3;

                string constraintsString = lineOfCode.Substring(lineOfCode.IndexOf("where "));

                string constraint = constraintsString.Substring(constraintsString.IndexOf(": ") + 2);

                // Let's separate em and build em back because we gotta get rid of things like "new()"
                if (constraint.Contains(","))
                {
                    string[] splitConstraints = constraint.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                    constraintsToUse.Clear();

                    foreach (string individualConstraint in splitConstraints)
                    {
                        string trimmed = individualConstraint.Trim();

                        if (trimmed != "new()")
                        {
                            constraintsToUse.Add(trimmed);
                        }
                    }
                    constraint = "";
                    for (int i = 0; i < constraintsToUse.Count; i++)
                    {
                        if (i == constraintsToUse.Count - 1)
                        {
                            constraint += constraintsToUse[i];
                        }
                        else
                        {
                            constraint += constraintsToUse[i] + ", ";
                        }
                    }
                }

                GenericTypes[0].GenericRestrictions.Add(constraint);

            }

            for(int i = 0; i < GenericTypes.Count; i++)
            {
                ParsedType genericType = GenericTypes[i];
                foreach (ParsedField parsedField in ArgumentList)
                {
                    if (parsedField.Type.GenericType != null && parsedField.Type.GenericType.Name == genericType.Name)
                    {
                        parsedField.Type.GenericRestrictions.AddRange(genericType.GenericRestrictions);

                    }
                }
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                int m = 3;
            }
            string staticString = "";

            if (IsStatic)
            {
                staticString = "static ";
            }

            string arguments = "";

            for (int i = 0; i < ArgumentList.Count; i++)
            {
                string stringToUse = ArgumentList[i].Type + " " + ArgumentList[i].Name;

                if (i == ArgumentList.Count - 1)
                {
                    arguments += stringToUse;
                }
                else
                {
                    arguments += stringToUse + ", ";
                }
            }

            return Scope + " " + staticString + Type + " " + Name + "(" + arguments + ");";
        }

        public ParsedMethod Clone()
        {
            ParsedMethod parsedMethod = (ParsedMethod)this.MemberwiseClone();

            parsedMethod.Type = Type.Clone();

            parsedMethod.ArgumentList = new List<ParsedField>();
            parsedMethod.GenericTypes = new List<ParsedType>();

            foreach (ParsedField argument in ArgumentList)
            {
                parsedMethod.ArgumentList.Add(argument.Clone());
            }

            foreach (ParsedType genericType in GenericTypes)
            {
                parsedMethod.GenericTypes.Add(genericType.Clone());
            }

            return parsedMethod;
        }

        public void StoreOffOldName()
        {
            mOldName = ToString();
        }


        internal void FillBaseCall(List<string> currentBlock)
        {
            int lineOn = 1;

            string baseCall = "";

            if (currentBlock.Count == 1)
            {
                return;
            }

            while (true)
            {
                // We need to trim here because we may not have
                // trimmed the content already
                if (lineOn >= currentBlock.Count)
                {
                    break;
                }
                string line = currentBlock[lineOn].Trim();
                
                if (line.StartsWith("{"))
                {
                    break;
                }
                else if( (line.Contains("base") || line.Contains("this")) && !line.EndsWith(";") )
                {
                    baseCall += line;
                }
                lineOn++;

            }

            if (baseCall != "")
            {
                BaseCall = new ParsedMethod();

                if (baseCall.StartsWith(":"))
                {
                    baseCall = baseCall.Substring(1).Trim();
                }

                BaseCall.FillHeaderInformation(baseCall);

                BaseCall.FillArgumentList(BaseCall, currentBlock, lineOn - 1, false);
            }
        }

        public static void GetLineInformation(
            string line,
            out Scope scope,
            out ParsedType type,
            out string variableName,
            out bool isConst,
            out bool isVirtual,
            out bool isOverride,
            out bool isStatic,
            out bool isNew,
            out bool isAsync,
            out string valueToAssignTo
            )
        {
            int index = 0;
            scope = Scope.Private;
            type = null;
            variableName = null;
            isConst = false;
            valueToAssignTo = null;
            isVirtual = false;
            isOverride = false;
            isStatic = false;
            isNew = false;
            isAsync = false;

            bool hasHadOpenParenthesis = false;
            bool hasHadOpenQuotes = false;
            bool hasEqualsBeenUsed = false;

            string currentType = "";

            while (true)
            {
                string word = ParsedClass.GetWord(line, ref index);

                const string public1 = " public ";
                //const string startWithPublic = "public ";

                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                else if (word == ";")
                {
                    continue;
                }
                else if (word == "const")
                {
                    isConst = true;
                }
                else if (word == "public")
                {
                    scope = Scope.Public;
                }
                else if (word == "private")
                {
                    scope = Scope.Private;
                }
                else if (word == "protected")
                {
                    scope = Scope.Protected;
                }
                else if (word == "internal")
                {
                    scope = Scope.Internal;
                }
                else if (word == "virtual")
                {
                    isVirtual = true;
                }
                else if (word == "override")
                {
                    isOverride = true;
                }
                else if (word == "static")
                {
                    isStatic = true;
                }
                else if (word == "new")
                {
                    isNew = true;
                }
                else if (word == "async")
                {
                    isAsync = true;
                }
                else if (type == null)
                {
                    if (word.Contains("<") && !word.Contains(">"))
                    {
                        currentType += word;
                    }
                    else if (currentType != "")
                    {
                        currentType += word;
                        if (word.Contains(">"))
                        {
                            type = new ParsedType(currentType);

                            currentType = "";
                        }
                    }
                    else
                    {

                        // check for []
                        int tempIndex = index;
                        string nextWord = ParsedClass.GetWord(line, ref tempIndex);
                        string wordAfterThat = ParsedClass.GetWord(line, ref tempIndex);

                        if (nextWord == "[" && wordAfterThat == "]")
                        {
                            type = new ParsedType(word + "[]");
                            index = tempIndex;
                        }
                        else
                        {
                            type = new ParsedType(word);
                        }
                    }
                }
                else if (!hasEqualsBeenUsed && word == "(")
                {
                    hasHadOpenParenthesis = true;
                }
                else if (variableName == null && !hasHadOpenParenthesis)
                {
                    if (word.EndsWith(";"))
                    {
                        variableName = word.Substring(0, word.Length - 1);
                    }
                    else
                    {
                        variableName = word;
                    }
                }
                else if (word == "=")
                {
                    hasEqualsBeenUsed = true;
                }
                else if (hasEqualsBeenUsed)
                {
                    if (valueToAssignTo == null)
                    {
                        valueToAssignTo = word;

                        if (valueToAssignTo.StartsWith("\"") && !hasHadOpenQuotes)
                        {
                            if (!valueToAssignTo.EndsWith("\""))
                            {
                                hasHadOpenQuotes = true;

                                int indexOfClosingQuotes = line.IndexOf("\"", index) + 1; // add 1 to capture the quote

                                string extraStuffToAdd = line.Substring(index, indexOfClosingQuotes - index);

                                valueToAssignTo += extraStuffToAdd;
                                index = indexOfClosingQuotes;
                            }
                        }
                    }
                    else
                    {

                        valueToAssignTo += " " + word;
                    }
                }

            }
        }

        internal void FillHeaderInformation(string headerLine)
        {
            Scope scope;
            ParsedType type;
            string variableName;
            bool isConst; // will always be false for properties
            string valueToAssignTo;
            bool isVirtual;
            bool isOverride;
            bool isStatic;
            bool isNew;
            bool isAsync;

            GetLineInformation(headerLine, out scope, out type, out variableName, out isConst, out isVirtual,
                out isOverride, out isStatic, out isNew, out isAsync, out valueToAssignTo);

            Scope = scope;
            Type = type;
            Name = variableName;
            IsStatic = isStatic;

            // Unqualify the type
            type.Unqualify();


        }

        internal void FillArgumentList(ParsedMethod parsedMethod, List<string> currentBlock, int lineIndex, bool requireTypes)
        {
            int wordIndex = 0;
            int  parenthesisDeep = 0;
            bool hasFoundClosedParenthesis = false;

            ParsedField argumentToAddTo = null;

            while (!hasFoundClosedParenthesis)
            {
                string line = currentBlock[lineIndex];

                wordIndex = 0;

                string currentType = null;

                while (true)
                {
                    string word = ParsedClass.GetWord(line, ref wordIndex);

                    if (word == "(")
                    {
                        if (argumentToAddTo != null)
                        {
                            argumentToAddTo.Name += word;
                        }

                        parenthesisDeep++;
                    }
                    else if (word == ")")
                    {
                        parenthesisDeep--;


                        if (parenthesisDeep == 0)
                        {
                            hasFoundClosedParenthesis = true;
                            break;
                        }
                        else if (argumentToAddTo != null)
                        {
                            argumentToAddTo.Name += word;
                        }

                        
                    }
                    else if (word == "")
                    {
                        lineIndex++;
                        break;
                    }
                    else if (parenthesisDeep == 0)
                    {
                        continue;
                    }
                    else if (word == ",")
                    {
                        argumentToAddTo = null;
                        continue;
                    }
                    else if (currentType == null && requireTypes)
                    {
                        currentType = word;
                    }
                    else
                    {
                        ParsedField parsedField = new ParsedField(Scope.Public, currentType, word);
                        currentType = null;

                        parsedMethod.ArgumentList.Add(parsedField);

                        argumentToAddTo = parsedField;
                    }

                }
            }
        }
    }
}
