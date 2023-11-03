using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedEnum
    {

        #region Fields

        List<string> mValues = new List<string>();
        bool mHasAssignment = true;
        string mClassDefinedIn;

        #endregion

        public string ClassDefinedIn
        {
            get { return mClassDefinedIn; }
            set { mClassDefinedIn = value; }
        }

        public string Name
        {
            get;
            set;
        }

        public string Namespace
        {
            get;
            set;
        }

        public bool IsPublic
        {
            get;
            set;
        }

        public List<string> Values
        {
            get { return mValues; }
        }

        public bool HasAssignment
        {
            get { return mHasAssignment; }
        }

        internal void Parse(string entireString, ref int startOfEnum)
        {

            int startBeforeParsing = startOfEnum;

            string line = ParsedClass.GetLine(entireString, ref startOfEnum);

            ParseHeader(line, out startOfEnum);

            startOfEnum += startBeforeParsing;

            int bracketsDeep = 0;
            string untrimmed;
            while (true)
            {            
                int indexBeforeParsing = startOfEnum;
                untrimmed = ParsedClass.GetLine(entireString, ref startOfEnum);
                line = untrimmed.Trim();

                if (line.StartsWith("["))
                {
                    continue;
                }
                else if (line.StartsWith("{"))
                {
                    bracketsDeep++;
                    startOfEnum = indexBeforeParsing + 1 + untrimmed.IndexOf('{');
                }
                else if (line == "")
                {
                    // do nothing, keep going
                }
                else if (line == "}" || line == "};" || line.StartsWith("}")) // the }; handles more cases.  Maybe we should be more robust for single-line enums
                {
                    bracketsDeep--;

                    if (bracketsDeep == 0)
                    {
                        break;
                    }
                }
                else
                {
                    if (line.EndsWith(","))
                    {
                        line = line.Substring(0, line.Length - 1);
                    }

                    if (line.Contains("="))
                    {
                        // it's got assignment
                        mHasAssignment = true;
                    }

                    if (line.Contains(","))
                    {

                        string[] values = line.Split(',');

                        foreach (string value in values)
                        {
                            if (value.Contains("}"))
                            {
                                mValues.Add(value.Substring(0, value.IndexOf("}")).Trim());
                            }
                            else
                            {
                                mValues.Add(value.Trim());
                            }
                        }

                        if( line.EndsWith("}") || line.EndsWith(";"))
                        {
                            bracketsDeep--;

                            if (bracketsDeep == 0)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        mValues.Add(line);
                    }
                }
            }
        }

        private void ParseHeader(string line, out int index)
        {
            index = 0;
            while (true)
            {
                string word = ParsedClass.GetWord(line, ref index).Trim();

                if (word == "public")
                {
                    IsPublic = true;
                }
                else if (word == "private" || word == "internal" || word == "protected")
                {
                    // toss it
                }
                else if (word == "enum")
                {
                    // toss it
                }
                else
                {
                    this.Name = word;
                    return;
                }

            }
        }

        public ParsedEnum Clone()
        {
            ParsedEnum cloneToReturn = (ParsedEnum)this.MemberwiseClone();


            cloneToReturn.mValues = new List<string>();

            cloneToReturn.mValues.AddRange(this.mValues);

            return cloneToReturn;

        }
    }
}
