using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
    public class DefineLayer
    {
        public List<string> CurrentDefines = new List<string>();
        public List<string> CurrentExcludes = new List<string>();

        public List<string> LastDefines = new List<string>();
        public List<string> LastExcludes = new List<string>();
    }

    public static class PreProcessorDefineParser
    {
        static Stack<DefineLayer> mDefineStack = new Stack<DefineLayer>();

        static DefineLayer Top
        {
            get { return mDefineStack.Peek(); }
        }

        public static void ParseLine(string line)
        {
            if (line.StartsWith("#if "))
            {
                string define = line.Substring("#if ".Length);

                mDefineStack.Push(new DefineLayer());
                IncludeAndExcludeFromIf(define);
            }
            else if (line.StartsWith("#else"))
            {
                List<string> temporaryDefine = new List<string>();
                List<string> temporaryExclude = new List<string>();


                foreach (string s in Top.LastDefines)
                {
                    temporaryExclude.Add(s);
                }
                foreach (string s in Top.LastExcludes)
                {
                    temporaryDefine.Add(s);
                }

                Top.CurrentDefines.Clear();
                Top.CurrentDefines.AddRange(temporaryDefine);

                Top.CurrentExcludes.Clear();
                Top.CurrentExcludes.AddRange(temporaryExclude);
            }
            else if (line.StartsWith("#elif "))
            {
                Top.CurrentExcludes.AddRange(Top.LastDefines);
                Top.CurrentDefines.AddRange(Top.LastExcludes);

                Top.LastDefines.Clear();
                Top.LastExcludes.Clear();

                string define = line.Substring("#elif ".Length);

                IncludeAndExcludeFromIf(define);
            }
            else if (line.StartsWith("#endif"))
            {
                Top.CurrentExcludes.Clear();
                Top.CurrentDefines.Clear();

                Top.LastDefines.Clear();
                Top.LastExcludes.Clear();

                mDefineStack.Pop();
            }

        }

        private static void IncludeAndExcludeFromIf(string define)
        {
            string[] defineItems = define.Split(' ');

            foreach (string defineItem in defineItems)
            {
                if (defineItem == "&&")
                {
                    // continue;
                }
                else if (defineItem.StartsWith("!"))
                {
                    string whatToAdd = defineItem.Substring(1);

                    Top.CurrentExcludes.Add(whatToAdd);

                    Top.LastExcludes.Add(whatToAdd);
                }
                else
                {
                    Top.CurrentDefines.Add(defineItem);

                    Top.LastDefines.Add(defineItem);
                }

            }


        }

        public static bool ShouldLineBeSkipped(List<string> projectDefines)
        {
            foreach (DefineLayer defineLayer in mDefineStack)
            {
                foreach (string define in defineLayer.CurrentDefines)
                {
                    if (!projectDefines.Contains(define))
                    {
                        return true;
                    }
                }
                foreach (string define in defineLayer.CurrentExcludes)
                {
                    if (projectDefines.Contains(define))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Clear()
        {
            mDefineStack.Clear();
        }
    }
}
