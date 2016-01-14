using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;

using FlatRedBall.Utilities;

namespace CodeTranslator.Parsers
{
    public class ParsedLine
    {
        #region Fields

        List<CodeItem> mCodeItems = new List<CodeItem>();



        #endregion

        #region Properties

        public CodeItem this[int i]
        {
            get { return mCodeItems[i]; }
            set { mCodeItems[i] = value; }
        }

        public int Count
        {
            get { return mCodeItems.Count; }
        }

        public List<CodeItem> CodeItems
        {
            get { return mCodeItems; }

        }

        public bool DeclaresNewVariable
        {
            get
            {
                foreach (CodeItem codeItem in mCodeItems)
                {
                    if (codeItem.CodeType == CodeType.VariableDeclaration)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion

        #region Methods

        public ParsedLine(string lineOfCode)
            : this(lineOfCode, null, null, null)
        { }

        List<ParsedField> existingFields = new List<ParsedField>();
        List<ParsedProperty> existingProperties = new List<ParsedProperty>();

        List<ParsedField> unchangedFields = new List<ParsedField>();
        List<ParsedProperty> unchangedProperties = new List<ParsedProperty>();

        float throwawayString;
        public ParsedLine(string lineOfCode,  ParsedClass parsedClass, ParsedClass unchangedClass,
            List<ParsedField> localVariables)
        {
            existingFields.Clear();
            existingProperties.Clear();
            unchangedFields.Clear();
            unchangedProperties.Clear();

            if (parsedClass != null)
            {
                parsedClass.FillWithThisAndInheitedFields(existingFields);
                parsedClass.FillWithThisAndInheitedProperties(existingProperties);
            }

            if (unchangedClass != null)
            {
                unchangedClass.FillWithThisAndInheitedFields(unchangedFields);
                unchangedClass.FillWithThisAndInheitedProperties(unchangedProperties);
            }

            int index = 0;

            string lastWord = "";
            string word = "";

            bool isInString = false;

            string remainderFromWord = null;

            CodeItem lastCodeItem = null;

            string wordWithoutThis = "";

            bool hadBase = false;

            int numberOfSquareBracketsDeep;

            Stack<CodeItem> bracketStack = new Stack<CodeItem>();

            char separator;

            #region Loop through all words and try to figure out what they are

            while (true)
            {
                #region Determine word, wordWithoutThis, and lastCodeItem

                if (mCodeItems.Count != 0)
                {
                    lastCodeItem = mCodeItems[mCodeItems.Count - 1];
                }

                word = ParsedClass.GetWord(lineOfCode, ref index, out separator);

                if (word.StartsWith("this."))
                {
                    wordWithoutThis = word.Substring("this.".Length);
                }
                else
                {
                    wordWithoutThis = "";
                }

                #endregion
                
                #region Perform tests that modify the CodeItem, but don't fully determine its type

                hadBase = word.StartsWith("base.");

                if (word.StartsWith("base."))
                {
                    word = word.Substring("base.".Length);
                }

                int openingBracketCount = word.CountOf('[');
                int closingBracketCount = word.CountOf(']');

                int netValue = openingBracketCount - closingBracketCount;



                #endregion


                if (string.IsNullOrEmpty(word) && index == lineOfCode.Length)
                {
                    break;
                }

 

        //todo:  Add ability to take something like (variableName and split that up into two CodeItems

                else if (word == "if" || word == "else" || word == "while" || word == "for" || word == "foreach" || 
                    word == "case")
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.ConditionalKeyword;

                    mCodeItems.Add(codeItem);
                }
                // Handle = separately because it's an assignment
                else if (word == "=" || word == "+" || word == "-")
                {
                    bool combine = lastCodeItem != null && lastCodeItem.CodeType == CodeType.Operation;

                    if(combine && (word == "-" || word == "+"))
                    {
                        if(lastCodeItem.Code == "=" || lastCodeItem.Code == ")")
                        {
                            combine = false;
                        }
                    }

                    if (combine)
                    {
                        // This is something like += or -= and we don't want those separated by a space
                        mCodeItems[mCodeItems.Count - 1].Code += word;

                        if (mCodeItems.Count - 2 >= 0)
                        {
                            mCodeItems[mCodeItems.Count - 2].IsAssigned = true;
                        }
                    }
                    else
                    {
                        CodeItem codeItem = new CodeItem();
                        codeItem.Code = word;
                        codeItem.CodeType = CodeType.Operation;

                        mCodeItems.Add(codeItem);

                        if (word == "=")
                        {
                            lastCodeItem.IsAssigned = true;
                        }
                    }

                }
                else if (
                    word == "("  || word == ")"  || word == "==" || word == ";"  || word == "}"  || word == "{" ||
                    word == "*"  || word == "/"  || word == ","  || word == "<"  || word == ">"  || word == "||" ||
                    word == "&&" || word == "|=" || word == "!=" || word == "<=" || word == ">=" || word == "&" ||
                    word == "|"  || word == "["  || word == "]"  || word == "!"  || word == "!=" )
                {



                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.Operation;

                    mCodeItems.Add(codeItem);

                    if (word == "==" || word == "!=" || word == "<=" || word == "<=")
                    {
                        lastCodeItem.IsCompared = true;
                    }
                }
                else if (word == "float" || word == "int" || word == "double" || word == "string" ||
                    word == "long" || word == "uint" || word == "byte" || word == "char")
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.Primitive;

                    mCodeItems.Add(codeItem);
                }
                else if (word == "null" || word == "return" || word == "ref" || word == "out" ||
                         word == "throw" || word == "new" || word == "try" || word == "in" ||
                         word == "const" || word == "base" || word == "is" || word == "as")
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.Keyword;

                    mCodeItems.Add(codeItem);
                }
                else if (word == "true" || word == "false" || float.TryParse(word, out throwawayString) ||
                    (word.EndsWith("f") && float.TryParse(word.Substring(0, word.Length - 1), out throwawayString)))
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.Constant;

                    mCodeItems.Add(codeItem);
                }
                else if (lastCodeItem != null && lastCodeItem.CodeType == CodeType.Keyword && lastCodeItem.Code == "new")
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    codeItem.CodeType = CodeType.Constructor;

                    mCodeItems.Add(codeItem);
                }
                //else if(FlatRedBall.Utilities.StringFunctions.

                else
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = word;
                    bool justSetIsInString = false;

                    if (codeItem.Code.StartsWith("@\"") || codeItem.Code.StartsWith("\"") || isInString)
                    {
                        codeItem.CodeType = CodeType.Constant;

                        if (!isInString)
                        {
                            justSetIsInString = true;
                        }

                        if (!word.EndsWith("\""))
                        {
                            int indexToUse = index;

                            if (index > 0 && lineOfCode[index - 1] == ' ')
                            {
                                indexToUse--;
                            }

                            int nextQuote = lineOfCode.IndexOf('"', indexToUse) + 1;

                            string fullString = lineOfCode.Substring(indexToUse, nextQuote - indexToUse);

                            codeItem.Code += fullString;

                            isInString = false;

                            index = nextQuote;
                        }
                        else
                        {
                            isInString = true;
                        }
                    }
                    else if (lastCodeItem != null)
                    {
                        if (lastCodeItem.CodeType == CodeType.Primitive)
                        {
                            codeItem.CodeType = CodeType.VariableDeclaration;
                        }
                        else if (lastCodeItem.CodeType == CodeType.Unknown)
                        {
                            lastCodeItem.CodeType = CodeType.Type;
                            codeItem.CodeType = CodeType.VariableDeclaration;

                        }
                        else if (lastCodeItem.Code == "in")
                        {
                            codeItem.CodeType = CodeType.Variable;
                        }
                        else if (lastCodeItem.Code == "new")
                        {
                            codeItem.CodeType = CodeType.Type;
                        }
                    }
                    else
                    {
                        codeItem.CodeType = CodeType.Unknown;
                    }

                    if (codeItem.Code.EndsWith("\"") && (justSetIsInString == false || codeItem.Code.Length > 1))
                    {
                        isInString = false;
                    }

                    mCodeItems.Add(codeItem);

                }

                CodeItem lastCodeItemAdded = mCodeItems[mCodeItems.Count - 1];

                if (netValue > 0)
                {
                    // push this guy on the stack
                    for (int i = 0; i < netValue; i++)
                    {
                        bracketStack.Push(lastCodeItemAdded);
                    }
                }
                else if (netValue < 0)
                {
                    // pop off the stack and link the code items to this
                    for (int i = 0; i < (-netValue); i++)
                    {
                        CodeItem itemToPairWith = bracketStack.Pop();
                        itemToPairWith.PairedSquareBracket = lastCodeItemAdded;
                        lastCodeItemAdded.PairedSquareBracket = itemToPairWith;

                    }
                }

                if (separator != ' ')
                {
                    lastCodeItemAdded.HasSpaceAfter = false;
                }
                if (hadBase)
                {
                    CodeItem codeItem = new CodeItem();
                    codeItem.Code = "base";
                    codeItem.CodeType = CodeType.Keyword;
                    codeItem.ChildCodeItem = mCodeItems[mCodeItems.Count - 1];
                    mCodeItems[mCodeItems.Count - 1].ParentCodeItem = codeItem;


                    mCodeItems.Insert(mCodeItems.Count - 1, codeItem);
                }


            }

            #endregion


            CodeItem itemBefore = null;
            CodeItem itemAfter = null;
            // Do a final pass of all words and see if there are any unknowns and try to identify them
            for (int i = 0; i < mCodeItems.Count; i++)
            {
                CodeItem codeItem = mCodeItems[i];



                if (codeItem.CodeType == CodeType.Unknown)
                {
                    #region Determine information about this CodeItem

                    if (i > 0)
                    {
                        itemBefore = mCodeItems[i - 1];
                    }
                    if (i < mCodeItems.Count - 1)
                    {
                        itemAfter = mCodeItems[i + 1];
                    }
                    else
                    {
                        itemAfter = null;
                    }


                    bool canBeVariable = itemAfter == null || itemAfter.CodeType != CodeType.Unknown;

                    wordWithoutThis = "";

                    if (codeItem.Code.StartsWith("this."))
                    {
                        wordWithoutThis = codeItem.Code.Substring("this.".Length);
                    }

                    #endregion

                    if (itemAfter != null && (itemAfter.Code == "==" || itemAfter.Code == "!="))
                    {
                        CodeItem otherSideOfOperation = mCodeItems[i + 2];

                        if (otherSideOfOperation.CodeType == CodeType.Constant || otherSideOfOperation.CodeType == CodeType.Keyword)
                        {
                            codeItem.CodeType = CodeType.Variable;
                        }
                    }
                    
                    
                    if (itemAfter != null && itemAfter.Code == "(")
                    {
                        codeItem.CodeType = CodeType.MethodCall;
                    }
                    else if (canBeVariable &&
                        (DoesListOfFieldsContainsField(existingFields, codeItem.Code) || 
                        DoesListOfFieldsContainsField(existingFields, wordWithoutThis)
                    ))
                    {
                        codeItem.CodeType = CodeType.Variable;

                    }
                    else if (canBeVariable && 
                        (DoesListOfPropertiesContainProperty(existingProperties, codeItem.Code) ||
                        DoesListOfPropertiesContainProperty(existingProperties, wordWithoutThis)))
                    {
                        codeItem.CodeType = CodeType.Variable;
                    }


                    else if (canBeVariable && 
                        (DoesListOfFieldsContainsField(unchangedFields, codeItem.Code) ||
                        DoesListOfFieldsContainsField(unchangedFields, wordWithoutThis)))
                    {
                        codeItem.CodeType = CodeType.Variable;
                    }

                    else if(canBeVariable && 
                        (DoesListOfPropertiesContainProperty(unchangedProperties, codeItem.Code) ||
                        DoesListOfPropertiesContainProperty(unchangedProperties, wordWithoutThis)))
                    {
                        codeItem.CodeType = CodeType.Variable;
                    }

                    else if (canBeVariable && localVariables != null && DoesListOfFieldsContainsField(localVariables, codeItem.Code))
                    {
                        codeItem.CodeType = CodeType.Variable;
                    }

                }
            }

        }


        public static bool DoesListOfFieldsContainsField(List<ParsedField> parsedFields, string variableName)
        {
            ParsedType parsedType;
            return DoesListOfFieldsContainsField(parsedFields, variableName, out parsedType);
        }

        public static bool DoesListOfFieldsContainsField(List<ParsedField> parsedFields, string variableName, out ParsedType parsedType)
        {
            parsedType = null;
            if (parsedFields == null)
            {
                return false;
            }

            for (int i = 0; i < parsedFields.Count; i++)
            {
                if (parsedFields[i].Name == variableName)
                {
                    parsedType = parsedFields[i].Type;
                    return true;
                }
            }
            return false;
        }

        public static bool DoesListOfPropertiesContainProperty(List<ParsedProperty> parsedProperty, string variableName)
        {
            ParsedType parsedType;
            return DoesListOfPropertiesContainProperty(parsedProperty, variableName, out parsedType);
        }
        
        public static bool DoesListOfPropertiesContainProperty(List<ParsedProperty> parsedProperty, string variableName, out ParsedType parsedType)
        {

            for (int i = 0; i < parsedProperty.Count; i++)
            {
                if (parsedProperty[i].Name == variableName)
                {
                    parsedType = parsedProperty[i].Type;
                    return true;
                }
            }
            parsedType = null;
            return false;
        }

        public static bool DoesListOfPropertiesContainProperty(ParsedClass parsedClass, string variableName)
        {
            if (DoesListOfPropertiesContainProperty(parsedClass.ParsedProperties, variableName))
            {
                return true;
            }

            foreach (ParsedClass parentClass in parsedClass.ParentParsedClasses)
            {
                if (DoesListOfPropertiesContainProperty(parentClass, variableName))
                {
                    return true;
                }
            }
            return false;

        }

        public void SetFromParsedLine(ParsedLine otherParsedLine)
        {
            mCodeItems = otherParsedLine.mCodeItems;
        }


        public void ConsolidateMethodContents()
        {
            for (int i = 0; i < CodeItems.Count; i++)
            {
                if (CodeItems[i].CodeType == CodeType.MethodCall ||
                    CodeItems[i].CodeType == CodeType.Constructor
                    )
                {
                    int closingParen = GetMatchingBracketForBracketAtIndex(i + 1);



                    ConsolidateMethodContents(i + 1, closingParen - (i));
                }
            }

        }

        private void ConsolidateMethodContents(int startIndex, int count)
        {
            StringBuilder newCode = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                newCode.Append(CodeItems[startIndex].Code);
                CodeItems.RemoveAt(startIndex);

            }

            CodeItem codeItem = new CodeItem();
            codeItem.Code = newCode.ToString();
            codeItem.CodeType = CodeType.ConsolidatedMethodContents;
            CodeItems.Insert(startIndex, codeItem);
        }

        public int GetMatchingBracketForBracketAtIndex(int codeItemIndex)
        {
            if (CodeItems[codeItemIndex].Code != "(")
            {
                throw new ArgumentException("The item at index " + codeItemIndex + " must be a '('.");
            }

            int parenCount = 0;
            for (int i = codeItemIndex; i < this.CodeItems.Count; i++)
            {
                if (CodeItems[i].Code == "(")
                {
                    parenCount++;
                }
                if (CodeItems[i].Code == ")")
                {
                    parenCount--;
                    if (parenCount == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public string ToSubString(int start, int count)
        {
            mStringBuilder.Remove(0, mStringBuilder.Length);

            for(int i= start; i < start + count; i++)
            {
                CodeItem codeItem = mCodeItems[i];

                
                if (codeItem.ChildCodeItem != null)
                {
                    mStringBuilder.Append(codeItem.Code + ".");
                }
                else if (codeItem.HasSpaceAfter)
                {
                    mStringBuilder.Append(codeItem.Code + " ");
                }
                else
                {
                    mStringBuilder.Append(codeItem.Code);
                }
            }

            return mStringBuilder.ToString().Trim();
        }

        StringBuilder mStringBuilder = new StringBuilder();
        public override string ToString()
        {
            return ToSubString(0, mCodeItems.Count);
        }

        public void CombineToExpressions()
        {

            CombineToExpressions(0, false);
        }

        public void CombineToExpressions(int startIndex, bool stopOnBreak)
        {
            Dictionary<char, int> pairedCharacterDictionary = new Dictionary<char, int>();
            pairedCharacterDictionary['('] = 0;
            pairedCharacterDictionary['\''] = 0;
            pairedCharacterDictionary['\"'] = 0;
            for (int i = startIndex; i < this.Count - 1; i++)
            {
                CodeItem itemAtI = this[i];
                CodeItem nextItem = this[i + 1];

                if (IsPartOfExpression(itemAtI, nextItem, pairedCharacterDictionary))
                {
                    itemAtI.Code += nextItem.Code;
                    this.CodeItems.RemoveAt(i + 1);
                    i--;
                }
                else if(stopOnBreak)
                {
                    return;
                }
            }

        }

        bool IsPartOfExpression(CodeItem first, CodeItem second, Dictionary<char, int> pairedCharacterDictionary)
        {
            bool returnValue = false;

            if (second.Code.StartsWith("."))
            {
                returnValue = true;
            }

            else if (first.CodeType == CodeType.MethodCall && 
                (second.Code == "(" || second.CodeType == CodeType.ConsolidatedMethodContents)) 
            {
                returnValue = true;
            }
            else if (pairedCharacterDictionary['('] != 0)
            {
                returnValue = true;

            }

            if (second.Code == "(")
            {
                pairedCharacterDictionary['('] = pairedCharacterDictionary['('] + 1;
            }
            else if (second.Code == ")")
            {
                pairedCharacterDictionary['('] = pairedCharacterDictionary['('] - 1;
            }

            return returnValue;
        }

        #endregion

    }
}
