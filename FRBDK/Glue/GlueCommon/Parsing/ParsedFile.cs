using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Parsing
{
	public class ParsedFile
	{
		List<ParsedNamespace> mNamespaces = new List<ParsedNamespace>();

		public List<ParsedNamespace> Namespaces
		{
			get { return mNamespaces; }
		}

        public ParsedFile()
        {
            // Do nothing - the user better call SetFromContents
        }

		public ParsedFile(string fileToParse) : this(fileToParse, true, true)
		{

        }

        public ParsedFile(string fileToParse, bool removeComments, bool trimClassContents)
        {
			string fileContents = FileManager.FromFileText(fileToParse);

            SetFromContents(fileContents, removeComments, trimClassContents);
		}

        public void SetFromContents(string contents, bool removeComments, bool trimClassContents)
        {
            if (removeComments)
            {
                contents = ParsedClass.RemoveComments(contents);
            }

            int startOfNamespace = contents.IndexOf("namespace ");
            if (startOfNamespace != -1)
            {
                int i = GetClosingBracketOfNamespace(contents);

                try
                {

                    string parsedNamespaceString = contents.Substring(startOfNamespace, i - startOfNamespace + 1);

                    ParsedNamespace parsedNamespace = new ParsedNamespace(parsedNamespaceString, removeComments, trimClassContents);

                    mNamespaces.Add(parsedNamespace);
                }
                catch
                {
                    int m = 3;
                }
            }
        }

        private static int GetClosingBracketOfNamespace(string fileContents)
        {
            int bracketsDeep = 0;
            int i = 0;

            for (i = 0; i < fileContents.Length; i++)
            {
                if (fileContents[i] == '{')
                {
                    bracketsDeep++;
                }
                else if (fileContents[i] == '}')
                {
                    bracketsDeep--;
                    if (bracketsDeep == 0)
                    {
                        break;
                    }
                }

            }
            return i;
        }
	}
}
