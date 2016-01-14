using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
	public class ParsedNamespace
    {
        #region Fields


        List<ParsedClass> mClasses = new List<ParsedClass>();
        List<ParsedEnum> mEnums = new List<ParsedEnum>();


        #endregion

        #region Properties

        public List<ParsedClass> Classes
		{
			get { return mClasses; }
		}

        public List<ParsedEnum> Enums
        {
            get { return mEnums; }
        }

        public string Name
        {
            get;
            set;
        }

        #endregion


        public ParsedNamespace(string namespaceContents) : this(namespaceContents, true, true)
		{

        }

        public ParsedNamespace(string namespaceContents, bool removeComments, bool trimParsedClassContents)
        {
            int indexIntoFile = 0;

            if (removeComments)
            {
                namespaceContents = ParsedClass.RemoveComments(namespaceContents);
            }

            namespaceContents = ParsedClass.RemovePreprocessoredOutCode(namespaceContents);


            #region Get the namespace name

            Name = FlatRedBall.Utilities.StringFunctions.GetWordAfter("namespace ", namespaceContents);


            #endregion


            while (true)
            {


                int startOfEnum = namespaceContents.IndexOf("enum ", indexIntoFile + 1);

                int startOfClass = namespaceContents.IndexOf("class ", indexIntoFile + 1);
                int startOfStruct = namespaceContents.IndexOf("struct ", indexIntoFile + 1);
                if (startOfStruct != -1 && startOfStruct < startOfClass || startOfClass == -1)
                {
                    startOfClass = startOfStruct;
                }


                int startOfInterface = namespaceContents.IndexOf("interface ", indexIntoFile + 1);

                if (startOfEnum == -1 && startOfClass == -1 && startOfInterface == -1)
                {
                    break;
                }

                if (startOfInterface != -1 && (startOfClass == -1 || startOfInterface < startOfClass))
                {
                    startOfClass = startOfInterface;
                }

                #region Get all enums


                if (startOfEnum != -1 && 
                    (startOfClass == -1 || startOfEnum < startOfClass))
                {

                    ParsedEnum parsedEnum = new ParsedEnum();

                    int startOfLine = 1 + namespaceContents.LastIndexOf('\n', startOfEnum - 1, startOfEnum);


                    parsedEnum.Parse(namespaceContents, ref startOfLine);
                    
                    startOfEnum = namespaceContents.IndexOf("enum ", startOfLine);

                    parsedEnum.Namespace = this.Name;
                    mEnums.Add(parsedEnum);
                    indexIntoFile = startOfLine;

                }

                #endregion

                #region Get all classes



                else if( startOfClass != -1)
                {
                    int i = GetEndOfClassIndex(namespaceContents, startOfClass);

                    indexIntoFile = i;

                    string substring = namespaceContents.Substring(startOfClass, i - startOfClass + 1);

                    ParsedClass parsedClass = new ParsedClass(substring, trimParsedClassContents);

                    parsedClass.Namespace = this.Name;
                    mClasses.Add(parsedClass);
                }
                #endregion

            }
		}

        private static int GetEndOfClassIndex(string namespaceContents, int startOfClass)
        {
            int bracketsDeep = 0;
            int i = 0;
            for (i = startOfClass; i < namespaceContents.Length; i++)
            {
                if (namespaceContents[i] == '{')
                {
                    bracketsDeep++;
                }
                else if (namespaceContents[i] == '}')
                {
                    bracketsDeep--;
                    if (bracketsDeep == 0)
                    {
                        break;
                    }
                }

            }

            if (i == namespaceContents.Length)
                i--;
            return i;
        }
	}
}
