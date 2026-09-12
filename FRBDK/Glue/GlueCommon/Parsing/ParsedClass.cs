using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using System.Reflection;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedClass
    {
        #region Fields

		string mName;
        
        List<ParsedField> mParsedFields = new List<ParsedField>();
        List<ParsedProperty> mParsedProperties = new List<ParsedProperty>();
        List<ParsedMethod> mParsedMethods = new List<ParsedMethod>();
        List<ParsedEnum> mParsedEnums = new List<ParsedEnum>();
        List<string> mCurrentBlock = new List<string>();
        int mIndexOfCurrentBlock = -1;

        List<ParsedType> mParentClassesAndInterfaces = new List<ParsedType>();
        List<ParsedClass> mParentParsedClasses = new List<ParsedClass>();

        List<ParsedType> mGenericTypes = new List<ParsedType>();
        // Adding any lists or reference objects here?  be sure to modify the Clone method!

        string mGetterContents;
        string mSetterContents;

        static List<string> mAddedDefines;

        #endregion

        #region Properties

        public string Contents
        {
            get;
            private set;
        }

        public List<ParsedType> GenericTypes
        {
            get { return mGenericTypes; }
        }

        public string Namespace
        {
            get;
            set;
        }

        public string Name
		{
			get { return mName; }
		}

		public List<ParsedType> ParentClassesAndInterfaces
		{
			get { return mParentClassesAndInterfaces; }
        }

        public List<ParsedField> ParsedFields
        {
            get { return mParsedFields; }
        }

        public List<ParsedProperty> ParsedProperties
        {
            get { return mParsedProperties; }
        }

        public List<ParsedMethod> ParsedMethods
        {
            get { return mParsedMethods; }

        }

        public List<ParsedEnum> ParsedEnums
        {
            get { return mParsedEnums; }
        }

        public bool IsInterface
        {
            get;
            set;
        }

        public bool IsPartial
        {
            get;
            set;
        }

        public List<ParsedClass> ParentParsedClasses
        {
            get { return mParentParsedClasses; }
        }

        public Type RuntimeType
        {
            get;
            set;
        }
        public List<string> CurrentAttributes { get; private set; } = new List<string>();

        #endregion

        #region Methods

        private ParsedClass()
        {

        }

        public ParsedClass(string classContents, bool trimContents)
		{
            Contents = classContents;
            if (mAddedDefines == null)
            {
                mAddedDefines = new List<string>();
            }

            ParseHeader(classContents);

            ParseContents(classContents, trimContents);


		}

        public static ParsedClass FromType(Type type)
        {
            ParsedClass toReturn = new ParsedClass();

            var fieldInfos = type.GetFields();
            foreach (var field in fieldInfos)
            {
                ParsedField parsedField = ParsedField.FromFieldInfo(field);

                toReturn.ParsedFields.Add(parsedField);
            }

            var propertyInfos = type.GetProperties();
            foreach (var property in propertyInfos)
            {
                ParsedProperty parsedProperty = ParsedProperty.FromPropertyInfo(property);

                toReturn.ParsedProperties.Add(parsedProperty);
            }

            return toReturn;
        }

        private void ParseHeader(string classContents)
        {
            int lineIndex = 0;
            int wordIndex = 0;

            if (classContents.StartsWith("\r") || classContents.StartsWith("\t"))
            {
                classContents = classContents.TrimStart();
            }

            string line = GetLine(classContents, ref lineIndex);

            while (line.Trim().EndsWith(":"))
            {
                string additionalLine = GetLine(classContents, ref lineIndex);
                line = line.Trim() + " " + additionalLine.Trim();
            }

            string genericWord = "";

            bool isInsideGenericName = false;
            bool isInsideGenericInheritedType = false;

            bool justFoundWhere = false;
            ParsedType genericTypeRestraining = null;

            mName = "";

            #region Loop through the words to get the header structure

            while (true)
            {
                string word = GetWord(line, ref wordIndex).Trim();

                if (word == "")
                {
                    break;
                }
                else if (word == "class" || word == "struct")
                {
                    IsInterface = false;
                }
                else if (word == "interface")
                {
                    IsInterface = true;
                }
                else if (word == "partial")
                {
                    IsPartial = true;
                }
                else if (justFoundWhere)
                {
                    for (int i = 0; i < mGenericTypes.Count; i++)
                    {
                        if (mGenericTypes[i].Name == word)
                        {
                            genericTypeRestraining = mGenericTypes[i];
                            break;
                        }
                    }
                    justFoundWhere = false;

                }
                else if (word == "where")
                {
                    genericTypeRestraining = null;
                    justFoundWhere = true;
                }
                else if (word == "," && !isInsideGenericName && !isInsideGenericInheritedType)
                {
                    continue;
                }
                else if (string.IsNullOrEmpty(mName) || isInsideGenericName)
                {
                    mName += word;

                    if (word.Contains('<') && !word.Contains('>'))
                    {
                        isInsideGenericName = true;
                    }
                    else if (isInsideGenericName && word.Contains('>'))
                    {
                        isInsideGenericName = false;
                    }
                    else if (word.Contains('<') && word.Contains('>'))
                    {
                        int indexOfLessThan = word.IndexOf("<");

                        mName = word.Substring(0, indexOfLessThan);

                        string remainderOfStuff =
                            word.Substring(indexOfLessThan, word.Length - indexOfLessThan);

                        // get rid of < and >
                        remainderOfStuff = remainderOfStuff.Substring(1, remainderOfStuff.Length - 2);

                        string[] generics = remainderOfStuff.Split(',');

                        foreach (string generic in generics)
                        {
                            mGenericTypes.Add(new ParsedType(generic));
                            //GenericTypes.Add(new ParsedField(

                        }
                    }
                }
                else if (word == ":" || word == "{")
                {
                    // do nothing
                }
                else
                {

                    if (isInsideGenericInheritedType)
                    {
                        genericWord += word;
                    }

                    if (word.Contains('<') && !word.Contains('>'))
                    {
                        isInsideGenericInheritedType = true;
                        genericWord = word;
                    }
                    else if (isInsideGenericInheritedType && word.Contains('>'))
                    {
                        isInsideGenericInheritedType = false;
                        mParentClassesAndInterfaces.Add(new ParsedType(genericWord));

                    }
                    else if (genericTypeRestraining != null)
                    {
                        genericTypeRestraining.GenericRestrictions.Add(word);
                    }
                    else if (!isInsideGenericInheritedType)
                    {
                        mParentClassesAndInterfaces.Add(new ParsedType(word));
                    }
                }

            }

            #endregion


            //int firstOpenBracket = classContents.IndexOf('{');
            //int firstColon = classContents.IndexOf(':');

            //if (firstColon != -1 && firstColon < firstOpenBracket)
            //{
            //    int indexAt = 0;
            //    string interfaceToAdd = StringFunctions.GetWordAfter(": ", classContents, indexAt);



            //    while (true)
            //    {
            //        mParentClassesAndInterfaces.Add(interfaceToAdd);

            //        indexAt = classContents.IndexOf(interfaceToAdd, indexAt) + interfaceToAdd.Length;

            //        // Don't add one for the first one
            //        if (classContents[indexAt] == ' ')
            //        {
            //            interfaceToAdd = classContents[indexAt].ToString();
            //        }
            //        while (classContents[indexAt + 1] == ' ')
            //        {
            //            indexAt++;
            //            interfaceToAdd = classContents[indexAt].ToString();
            //        }


            //        interfaceToAdd = StringFunctions.GetWordAfter(interfaceToAdd, classContents, indexAt);



            //    }
            //}
        }

        void ParseContents(string classContents, bool trim)
        {
            int index = 0;
            int bracketsDeep = 0;

            PreProcessorDefineParser.Clear();

            string remainderOfLine = null;

            while (true)
            {
                string line = "";
                string untrimmedLine = "";

                if (remainderOfLine == null)
                {
                    untrimmedLine = GetLine(classContents, ref index);
                    line = untrimmedLine.Trim();
                }
                else
                {
                    line = remainderOfLine;
                }

                bool isComment = line.Trim().StartsWith("//");

                remainderOfLine = null;                

                #region If it's empty, continue or end depending on how far we are in the file
                if (line == "")
                {
                    if (index != classContents.Length)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                #endregion

                #region It's a define code like #if or #elif or #else

                else if (line.StartsWith("#if ") ||
                    line.StartsWith("#endif") || line.StartsWith("#elif") || line.StartsWith("#else"))
                {

                    PreProcessorDefineParser.ParseLine(line);
                }

                #endregion

                else if (PreProcessorDefineParser.ShouldLineBeSkipped(mAddedDefines) || isComment)
                {
                    continue;
                }

                #region It's just an empty line ( "\r" )

                else if (line == "\r")
                {
                    // do nothing, continue
                }

                #endregion

                #region It's an open bracket ( "{" )
                else if (line.StartsWith("{"))
                {
                    bracketsDeep++;

                    if (mCurrentBlock.Count != 0)
                    {
                        if (trim)
                        {
                            AddToCurrentBlock(line, index);
                        }
                        else
                        {
                            AddToCurrentBlock(untrimmedLine, index);
                        }
                    }

                    if (line != "{")
                    {
                        remainderOfLine = line.Substring(1);
                    }
                }
                #endregion

                #region It's a close bracket ( "}" )

                else if (line.StartsWith("}"))
                {
                    bracketsDeep--;

                    if (mCurrentBlock.Count != 0)
                    {
                        if (trim)
                        {
                            AddToCurrentBlock(line, index);
                        }
                        else
                        {
                            AddToCurrentBlock(untrimmedLine, index);
                        }
                    }

                    if (bracketsDeep == 1)
                    {
                        ProcessCurrentBlock(index, trim);
                    }

                    if (line != "}")
                    {
                        remainderOfLine = line.Substring(1);
                    }
                }

                #endregion

                #region If it's a field

                else if (bracketsDeep == 1 && line.EndsWith(";") && 
                    // C# 6 introduces assigning values like public float Something { get; set; } = 3;
                    !line.Contains("{"))
                {
                    if (line.Contains('(') && line.EndsWith(");") && !line.Contains("="))
                    {
                        // likely a single-line method in an interface
                        // void ClearRelationships();
                        if (trim)
                        {
                            AddToCurrentBlock(line, index);
                        }
                        else
                        {
                            AddToCurrentBlock(untrimmedLine, index);
                        }
                        ProcessCurrentBlock(index, trim);
                    }
                    else
                    {
                        ParseField(line);
                    }
                }

                #endregion

                #region Attributes

                else if(bracketsDeep == 1 && line.StartsWith("[") && line.EndsWith("]"))
                {
                    AddAttribute(line);
                }

                #endregion

                #region If there is a bracket inside a line "}"

                else if (bracketsDeep == 1 && NumberOfValid('{', line) != 0)
                {
                    // Could be a single-liner
                    // let's parse it as a property
                    if (trim)
                    {
                        AddToCurrentBlock(line, index);
                    }
                    else
                    {
                        AddToCurrentBlock(untrimmedLine, index);
                    }

                    if (line.Contains('}'))
                    {
                        ProcessCurrentBlock(index, trim);
                    }
                    else
                    {
                        bracketsDeep++;
                    }
                }

                #endregion

                else if (mCurrentBlock.Count == 0 && line.StartsWith("[") && line.EndsWith("]"))
                {
                    // It's an attribute like [XmlIgnore].  Don't do anything with this for now
                }

                else if (line.StartsWith("#region ") || line.StartsWith("#endregion"))
                {
                    // do nothing
                }

                #region Else, save off this line - it may be part of a method or property

                else
                {
                    bool containsCloseBracket = false;

                    if (bracketsDeep > 0)
                    {
                        if (trim)
                        {
                            AddToCurrentBlock(line, index);
                        }
                        else
                        {
                            AddToCurrentBlock(untrimmedLine, index);
                        }
                    }

                    // The following methods will properly handle { and } inside quotes, like
                    // string classFormat = "Level{0}";
                    if (NumberOfValid('{', line) > 0)
                    {
                        bracketsDeep++;
                    }
                    if (NumberOfValid('}', line) > 0)
                    {
                        bracketsDeep--;
                        containsCloseBracket = true;
                    }
                    // I think we want to check this *before* increasing the bracketsDeep value
                    //if (bracketsDeep > 0)
                    //{
                    //    if (trim)
                    //    {
                    //        AddToCurrentBlock(line, index);
                    //    }
                    //    else
                    //    {
                    //        AddToCurrentBlock(untrimmedLine, index);
                    //    }
                    //}

                    if (containsCloseBracket)
                    {
                        if (bracketsDeep == 1)
                        {
                            ProcessCurrentBlock(index, trim);
                        }
                    }
                }


                #endregion

            }

            // Mark any overloaded methods as such

            List<string> methodNamesSoFar = new List<string>();

            for(int i = 0; i < mParsedMethods.Count - 1; i++)
            {
                ParsedMethod method = mParsedMethods[i];

                for(int j = i + 1; j < mParsedMethods.Count; j++)
                {
                    ParsedMethod otherMethod = mParsedMethods[j];

                    // Normally we use the Method.ToString to identify methods, but here we're looking for overloads, so we'll use name
                    // ...but is this okay?  Or do we want to use ToString to get exact matches?
                    if (method.Name == otherMethod.Name)
                    {
                        method.IsOverload = true;
                        otherMethod.IsOverload = true;
                    }
                }
            }

        }

        private void AddAttribute(string line)
        {
            CurrentAttributes.Add(line);
        }

        private void AddToCurrentBlock(string line, int index)
        {
            if (mCurrentBlock.Count == 0)
            {
                mIndexOfCurrentBlock = index - line.Length;

                if (index > 1 && Contents[index - 1] == '\n' && Contents[index - 2] == '\r')
                {
                    mIndexOfCurrentBlock -= 2;
                }
            }

            mCurrentBlock.Add(line);
        }

        public static int NumberOfValid(char charToSearchFor, string lineOfCode)
        {
            bool isInQuotes = false;
            bool isInComments = false;
            int count = 0;

            for (int i = 0; i < lineOfCode.Length; i++)
            {
                char charAtI = lineOfCode[i];

                if (charAtI == '"')
                {
                    isInQuotes = !isInQuotes;
                }
                else if (charAtI == '/' && i < lineOfCode.Length - 1 && lineOfCode[i + 1] == '/')
                {
                    isInComments = true;
                }
                else if (charAtI == '\n' || charAtI == '\r')
                {
                    isInComments = false;
                }
                else if (!isInQuotes && !isInComments && charAtI == charToSearchFor)
                {
                    count++;
                }
            }


            return count;
        }

        public ParsedClass Clone()
        {
            ParsedClass newParsedClass = (ParsedClass)MemberwiseClone();

            newParsedClass.mParsedFields = new List<ParsedField>();
            newParsedClass.mParsedProperties = new List<ParsedProperty>();
            newParsedClass.mParsedMethods = new List<ParsedMethod>();
            newParsedClass.mParentClassesAndInterfaces = new List<ParsedType>();
            newParsedClass.mCurrentBlock = new List<string>();
            newParsedClass.mParsedEnums = new List<ParsedEnum>();
            newParsedClass.mParentParsedClasses = new List<ParsedClass>();

            newParsedClass.Namespace = Namespace;
            newParsedClass.mName = mName;
            newParsedClass.mGetterContents = mGetterContents;
            newParsedClass.mSetterContents = mSetterContents;
            newParsedClass.mGenericTypes = new List<ParsedType>();

            // do nothing with mCurrentBlock
            foreach (ParsedType parsedType in mParentClassesAndInterfaces)
            {
                newParsedClass.mParentClassesAndInterfaces.Add(parsedType.Clone());
            }

            foreach (ParsedField parsedField in mParsedFields)
            {
                newParsedClass.ParsedFields.Add(parsedField.Clone());
            }

            foreach (ParsedProperty parsedProperty in mParsedProperties)
            {
                newParsedClass.ParsedProperties.Add(parsedProperty.Clone());
            }

            foreach (ParsedMethod parsedMethod in mParsedMethods)
            {
                newParsedClass.ParsedMethods.Add(parsedMethod.Clone());
            }

            foreach (ParsedEnum parsedEnum in mParsedEnums)
            {
                newParsedClass.ParsedEnums.Add(parsedEnum.Clone());
            }

            foreach (ParsedClass parsedClass in mParentParsedClasses)
            {
                newParsedClass.mParentParsedClasses.Add(parsedClass.Clone());
            }

            foreach (ParsedType parsedType in mGenericTypes)
            {
                newParsedClass.mGenericTypes.Add(parsedType.Clone());
            }

            return newParsedClass;
        }


        private void ProcessCurrentBlock(int currentIndex, bool trimContents)
        {
            if (mCurrentBlock[0].Contains(" enum ") || mCurrentBlock[0].Contains("\tenum "))
            {
                CreateParsedEnum();
            }
            else if (!mCurrentBlock[0].Contains('('))
            {
                CreateParsedProperty();

            }
            else
            {
                CreateParsedMethod(mIndexOfCurrentBlock, currentIndex, trimContents);
            }


            mCurrentBlock.Clear();
            CurrentAttributes.Clear();
            // Finally, clear the block
        }

        private void CreateParsedEnum()
        {
            StringBuilder temporaryStringBuilder = new StringBuilder();

            foreach (string s in mCurrentBlock)
            {
                temporaryStringBuilder.AppendLine(s);
            }

            int startingIndex = 0;

            ParsedEnum parsedEnum = new ParsedEnum();
            parsedEnum.Parse(temporaryStringBuilder.ToString(), ref startingIndex);
            parsedEnum.ClassDefinedIn = this.Name;
            mParsedEnums.Add(parsedEnum);
        }

        private void CreateParsedProperty()
        {
            // For now we'll assume that all properties are on 
            // the same line.  Eventually we may want to combine
            // all lines before the opening bracket

            string headerLine = mCurrentBlock[0];

            #region Get header information

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

            ParsedMethod.GetLineInformation(headerLine, out scope, out type, out variableName, out isConst, out isVirtual, 
                out isOverride, out isStatic, out isNew, out isAsync, out valueToAssignTo );

            ParsedProperty parsedProperty = new ParsedProperty();
            parsedProperty.Scope = scope;
            parsedProperty.Type = type;
            parsedProperty.Name = variableName;
            parsedProperty.IsVirtual = isVirtual;
            parsedProperty.IsStatic = isStatic;

            #endregion

            StringBuilder getterLines = new StringBuilder();

            StringBuilder setterLines = new StringBuilder();

            bool hasGetter;
            bool hasSetter;

            bool hasAutomaticGetter;
            bool hasAutomaticSetter;

            FillGettersAndSetters(getterLines, setterLines, true, false, out hasGetter, out hasSetter, out hasAutomaticGetter, out hasAutomaticSetter);

            parsedProperty.HasAutomaticGetter = hasAutomaticGetter;
            parsedProperty.HasAutomaticSetter = hasAutomaticSetter;

            if (hasGetter)
            {
                parsedProperty.GetContents = getterLines.ToString();
            }
            else
            {
                parsedProperty.GetContents = null;
            }

            if (hasSetter)
            {
                parsedProperty.SetContents = setterLines.ToString();
            }
            else
            {
                parsedProperty.SetContents = null;
            }

            parsedProperty.Attributes.AddRange(CurrentAttributes);

            mParsedProperties.Add(parsedProperty);
        }

        private void FillGettersAndSetters(StringBuilder getterLines, StringBuilder setterLines, bool requireGetters, bool trimContents,
            out bool hasGetter, out bool hasSetter, out bool hasAutomaticGetter, out bool hasAutomaticSetter)
        {
            hasGetter = false;
            hasSetter = false;

            hasAutomaticGetter = false;
            hasAutomaticSetter = false;

            StringBuilder currentStringBuilder = null;

            int bracketsDeep = 0;
            string remainderFromLine = null;

            int bracketsDeepForContents = 1;


            if (!requireGetters)
            {
                bracketsDeepForContents = 0;
            }

            int i = 1;

            if ( NumberOfValid('{', mCurrentBlock[0]) > 0)
            {
                remainderFromLine = mCurrentBlock[0].Substring(mCurrentBlock[0].IndexOf('{'));
            }

            for ( ; i < mCurrentBlock.Count || remainderFromLine != null; i++)
            {
                string untrimmedLine = remainderFromLine;

                if (remainderFromLine != null)
                {
                    remainderFromLine = remainderFromLine.Trim();
                }
                string line = remainderFromLine;
                
                if (line == null)
                {
                    line = mCurrentBlock[i].Trim();
                    untrimmedLine = mCurrentBlock[i];
                }
                else
                {
                    i--;
                    remainderFromLine = null;
                }

                if (line.StartsWith("{"))
                {
                    if (bracketsDeep > bracketsDeepForContents)
                    {
                        if (currentStringBuilder != null)
                        {
                            if (trimContents)
                            {
                                currentStringBuilder.AppendLine(line);
                            }
                            else
                            {
                                currentStringBuilder.AppendLine(untrimmedLine);
                            }
                        }
                    }
                    bracketsDeep++;

                    if (!requireGetters)
                    {
                        currentStringBuilder = getterLines;
                    }

                    if (line.Trim() != "{")
                    {
                        remainderFromLine = line.Substring(1);
                    }

                }
                else if (line.StartsWith("}"))
                {
                    bracketsDeep--;

                    if (bracketsDeep > bracketsDeepForContents)
                    {
                        if (currentStringBuilder != null)
                        {
                            if (trimContents)
                            {
                                currentStringBuilder.AppendLine(line);
                            }
                            else
                            {
                                currentStringBuilder.AppendLine(untrimmedLine);
                            }
                        }
                    }

                    if (bracketsDeep == bracketsDeepForContents)
                    {
                        currentStringBuilder = null;
                    }

                    if (line.Trim() != "}")
                    {
                        remainderFromLine = line.Substring(1);
                    }
                }
                else if (requireGetters && bracketsDeep == bracketsDeepForContents && LineHasGetter(line))
                {
                    hasGetter = true;

                    currentStringBuilder = getterLines;

                    if (line == "get;")
                    {
                        hasAutomaticGetter = true;
                    }
                    else if (line.Trim() != "get")
                    {
                        
                        remainderFromLine = line.Substring("get".Length);
                    }
                }
                else if (requireGetters && bracketsDeep == bracketsDeepForContents && LineHasSetter(line))
                {
                    hasSetter = true;

                    currentStringBuilder = setterLines;

                    if (line == "set;")
                    {
                        hasAutomaticSetter = true;
                    }
                    else if (line.Trim() != "set")
                    {
                        remainderFromLine = line.Substring("set".Length);
                    }
                }
                    // See if this is an automatic property
                else if (currentStringBuilder != null && bracketsDeep == 1 &&
                    line.StartsWith(";"))
                {
                    currentStringBuilder.Append(";");
                    // And that ends it!
                    currentStringBuilder = null;

                    if (line != ";")
                    {
                        remainderFromLine = line.Substring(1);
                    }

                }
                else
                {
                    if (bracketsDeep > bracketsDeepForContents)
                    {
                        if ( NumberOfValid('}', line) > 0)
                        {
                            remainderFromLine = line.Substring(line.IndexOf("}"));
                            line = line.Substring(0, line.IndexOf("}"));
                        }

                        if (currentStringBuilder != null)
                        {
                            if (trimContents)
                            {
                                currentStringBuilder.AppendLine(line);
                            }
                            else
                            {
                                currentStringBuilder.AppendLine(untrimmedLine);
                            }
                        }
                    }
                }
            }
        }

        public bool LineHasSetter(string line)
        {
            return line.StartsWith("set") || 
                (line.Contains(" ") && line.Substring(line.IndexOf(" ") + 1).StartsWith("set"));

        }

        public bool LineHasGetter(string line)
        {
            return line.StartsWith("get") ||
                (line.Contains(" ") && line.Substring(line.IndexOf(" ") + 1).StartsWith("get"));
        }

        private void CreateParsedMethod(int startIndex, int endIndex, bool trimContents)
        {
            // For now we'll assume that all properties are on 
            // the same line.  Eventually we may want to combine
            // all lines before the opening bracket


            // todo: add attributes:
            CurrentAttributes.Clear();


            #region Get header information

            int lineIndexForHeader = 0;

            string headerLine = mCurrentBlock[lineIndexForHeader].Trim() ;

            int numberOfParens = mCurrentBlock[lineIndexForHeader].CountOf('(') - mCurrentBlock[lineIndexForHeader].CountOf(')');
            

            while (numberOfParens > 0)
            {
                lineIndexForHeader++;
                headerLine += " " + mCurrentBlock[lineIndexForHeader].Trim();

                numberOfParens += mCurrentBlock[lineIndexForHeader].CountOf('(') - mCurrentBlock[lineIndexForHeader].CountOf(')');
            }
            
            ParsedMethod parsedMethod = new ParsedMethod();

            parsedMethod.FillHeaderInformation(headerLine);

            #endregion

            parsedMethod.StartIndex = startIndex;
            parsedMethod.EndIndex = endIndex;

            parsedMethod.FillArgumentList(parsedMethod, mCurrentBlock, 0, true);

            // Fill generic restrictions after getting all arguments
            parsedMethod.FillGenericRestrictions(headerLine);

            parsedMethod.FillBaseCall(mCurrentBlock);

            StringBuilder methodContents = new StringBuilder();

            bool hasGetter;
            bool hasSetter;

            bool hasAutomaticGetter;
            bool hasAutomaticSetter;

            FillGettersAndSetters(methodContents, methodContents, false, trimContents, out hasGetter, out hasSetter, out hasAutomaticGetter, out hasAutomaticSetter);

            parsedMethod.MethodContents = methodContents.ToString();
            parsedMethod.StoreOffOldName();

            mParsedMethods.Add(parsedMethod);
        }

       
        private void ParseField(string line)
        {
            ParsedField parsedField = GetParsedField(line);
            // todo: parsedField add attributes
            this.CurrentAttributes.Clear();
            mParsedFields.Add(parsedField);
        }

        public static ParsedField GetParsedField(string line)
        {
            Scope scope;
            ParsedType type;
            string variableName;
            bool isConst;
            string valueToAssignTo;
            bool isVirtual;
            bool isOverride;
            bool isStatic;
            bool isNew;
            bool isAsync;


            ParsedMethod.GetLineInformation(line, out scope, out type, out variableName, out isConst, out isVirtual,
                out isOverride, out isStatic, out isNew, out isAsync, out valueToAssignTo);

            ParsedField parsedField = new ParsedField();
            parsedField.Scope = scope;
            parsedField.Type = type;
            parsedField.Name = variableName;
            parsedField.IsConst = isConst;
            parsedField.IsStatic = isStatic;
            parsedField.ValueToAssignTo = valueToAssignTo;

            return parsedField;
        }


        public static string RemoveComments(string classContents)
        {
            StringBuilder stringBuilder = new StringBuilder(classContents);
            int index = 0;

            int amountRemovedSoFar = 0;

            while (true)
            {
                string line = GetLine(classContents, ref index);

                if (string.IsNullOrEmpty(line))
                {
                    if (index > classContents.Length - 1)
                    {
                        return stringBuilder.ToString();
                    }
                }
                else
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("//") || 
                        trimmedLine.StartsWith("#region") ||
                        trimmedLine.StartsWith("#endregion"))
                    {
                        int startOfLine = index - amountRemovedSoFar - line.Length - 1;
                        if (startOfLine == -1)
                        {
                            startOfLine = 0;
                        }
                        int amountToRemove = line.Length + 1;

                        if(amountToRemove + startOfLine > stringBuilder.Length)
                        {
                            amountToRemove = stringBuilder.Length - startOfLine;
                        }

                        stringBuilder.Remove(startOfLine, amountToRemove);

                        amountRemovedSoFar += amountToRemove;
                    }

                    else if (line.Contains("//"))
                    {
                        // Is this comment part of a string, like "http://www.flatredball.com"?
                        int indexOfComment = line.IndexOf("//", 0);

                        while (indexOfComment != -1)
                        {
                            int numberOfQuotesBeforeComment = line.CountOf('"', 0, indexOfComment);

                            if (numberOfQuotesBeforeComment % 2 == 0)
                            {



                                int amountToRemove = line.Length - indexOfComment - 1;

                                int startOfLine = index - amountRemovedSoFar - line.Length - 1;

                                int whereToRemoveFrom = startOfLine + indexOfComment;

                                stringBuilder.Remove(whereToRemoveFrom, amountToRemove);

                                amountRemovedSoFar += amountToRemove;
                                break;
                            }
                            else
                            {
                                indexOfComment = trimmedLine.IndexOf("//", indexOfComment + 1);
                            }

                        }
                    }


                }
                
            }

        }

        public static string GetLine(string entireString, ref int startingIndex)
        {
            int newLine = entireString.IndexOf('\n', startingIndex);

            if (newLine == -1)
            {
                int oldIndex = startingIndex;
                startingIndex = entireString.Length;
                return entireString.Substring(oldIndex);
            }
            else
            {
                int oldStartingIndex = startingIndex;
                startingIndex = newLine + 1;

                string toReturn = entireString.Substring(oldStartingIndex, newLine - oldStartingIndex);

                return toReturn;
            }

        }

        public static char[] mWordSeparators = new char[]{
            ' ',
            '(',
            ')',
            ';',
            ',',
            '[',
            ']',
            '+',
            '-',
            '/',
            '*'
        };

        public static string GetWord(string entireString, ref int startingIndex)
        {
            char throwaway;

            return GetWord(entireString, ref startingIndex, out throwaway);
        }

        public static string GetWord(string entireString, ref int startingIndex, out char separator)
        {
            int separatorIndex = entireString.IndexOfAny(mWordSeparators, startingIndex);
            separator = (char)0;

            if (separatorIndex == -1)
            {
                if (startingIndex == entireString.Length)
                {
                    return "";
                }
                else
                {
                    int oldStartingIndex = startingIndex;
                    startingIndex = entireString.Length;
                    return entireString.Substring(oldStartingIndex);
                }
            }
            else
            {
                separator = entireString[separatorIndex];

                if (separatorIndex == startingIndex)
                {
                    if (entireString[startingIndex] != ' ')
                    {
                        int oldStartingIndex = startingIndex;
                        startingIndex++;
                        return entireString.Substring(oldStartingIndex, 1);
                    }
                    else
                    {
                        startingIndex++;
                        // It's a space, so let's just skip this line, then move onward
                        return GetWord(entireString, ref startingIndex);
                    }
                }
                else
                {
                    int oldStartingIndex = startingIndex;

                    if (entireString[startingIndex] == '-')
                    {
                        startingIndex++;
                        return "-";
                    }
                    else if (entireString[startingIndex] == '!' && startingIndex < entireString.Length - 1 &&
                        entireString[startingIndex + 1] != '=')
                    {
                        startingIndex++;
                        return "!";
                    }
                    else
                    {
                        if (entireString[separatorIndex] == ' ')
                        {
                            startingIndex = separatorIndex + 1;
                        }
                        else
                        {
                            startingIndex = separatorIndex;
                        }
                        return entireString.Substring(oldStartingIndex, separatorIndex - oldStartingIndex);
                    }
                }
            }

        }

        public ParsedField GetField(string fieldName)
        {
            foreach (ParsedField parsedField in mParsedFields)
            {
                if (parsedField.Name == fieldName)
                {
                    return parsedField;
                }
            }

            foreach (ParsedClass parentClass in mParentParsedClasses)
            {

                ParsedField parsedField = parentClass.GetField(fieldName);

                if (parsedField != null)
                {
                    return parsedField;
                }
            }

            return null;
        }

        public ParsedProperty GetProperty(string propertyName)
        {

            foreach (ParsedProperty parsedProperty in mParsedProperties)
            {
                if (parsedProperty.Name == propertyName)
                {
                    return parsedProperty;
                }
            }

            foreach (ParsedClass parentClass in mParentParsedClasses)
            {
                ParsedProperty parsedProperty = parentClass.GetProperty(propertyName);

                if (parsedProperty != null)
                {
                    return parsedProperty;
                }
            }

            if (this.RuntimeType != null)
            {
                PropertyInfo propertyInfo = RuntimeType.GetProperty(propertyName);

                if (propertyInfo != null)
                {
                    ParsedProperty property = new ParsedProperty(Scope.Public, propertyInfo.PropertyType.Name, propertyName);

                    return property;
                }
                    
            }

            return null;
        }

        public ParsedMethod GetMethod(string methodName)
        {
            foreach (ParsedMethod parsedMethod in mParsedMethods)
            {
                if (parsedMethod.Name == methodName)
                {
                    return parsedMethod;
                }
            }

            foreach (ParsedClass parentClass in mParentParsedClasses)
            {
                ParsedMethod parsedMethods = parentClass.GetMethod(methodName);

                if (parsedMethods != null)
                {
                    return parsedMethods;
                }
            }

            return null;
        }

        public void FillWithThisAndInheitedFields(List<ParsedField> parsedFields)
        {
            parsedFields.AddRange(mParsedFields);

            foreach (ParsedClass parsedClass in mParentParsedClasses)
            {
                parsedClass.FillWithThisAndInheitedFields(parsedFields);
            }
        }

        public void FillWithThisAndInheitedProperties(List<ParsedProperty> parsedProperties)
        {
            parsedProperties.AddRange(mParsedProperties);

            foreach (ParsedClass parsedClass in mParentParsedClasses)
            {
                parsedClass.FillWithThisAndInheitedProperties(parsedProperties);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion



        internal static string RemovePreprocessoredOutCode(string namespaceContents)
        {
            if (mAddedDefines == null)
            {
                mAddedDefines = new List<string>();
            }

            StringBuilder returnStringBuilder = new StringBuilder();
            int index = 0;
            int bracketsDeep = 0;

            PreProcessorDefineParser.Clear();
            string untrimmed;
            while (true)
            {
                string line = "";

                untrimmed = GetLine(namespaceContents, ref index);
                if (untrimmed.EndsWith("\r"))
                {
                    untrimmed = untrimmed.Substring(0, untrimmed.Length - 1); // we do this to get rid of the \r at the end
                }
                line = untrimmed.Trim();

                if (line == "")
                {
                    if (index != namespaceContents.Length)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                else if (line.StartsWith("#if ") ||
                    line.StartsWith("#endif") || line.StartsWith("#elif") || line.StartsWith("#else"))
                {

                    PreProcessorDefineParser.ParseLine(line);
                }

                else if (PreProcessorDefineParser.ShouldLineBeSkipped(mAddedDefines))
                {
                    continue;
                }
                else
                {
                    returnStringBuilder.AppendLine(untrimmed);
                }
            }

            return returnStringBuilder.ToString();
        }
    }
}
