using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.IO;

namespace FlatRedBall.Glue.Events
{
    public static class EventManager
    {
        #region Fields

        static Dictionary<string, EventSave> mAllEvents = new Dictionary<string, EventSave>();

        #endregion

        #region Properties

        public static Dictionary<string, EventSave> AllEvents
        {
            get { return mAllEvents; }
        }

        #endregion

        #region Methods

        public static void Initialize()
        {
            var directory = GlueCommands.Self.FileCommands.GetGlueExecutingFolder();

            FilePath fileToLoad = directory.FullPath + 
                @"Content/BuiltInEvents.csv";
            
            if(fileToLoad.Exists() == false)
            {
                System.Windows.Forms.MessageBox.Show("Could not find the file: " + fileToLoad);
            }
            else
            {
                try
                {
                    CsvFileManager.CsvDeserializeDictionary<string, EventSave>(fileToLoad.FullPath, mAllEvents);
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show("Could not initialize event manager.  Check to make sure " +
                        "the following file exists and is not corrupt: " + fileToLoad + "\n\n" + e.ToString());
                }
            }

        }

        public static string GetGeneratedEventFileNameForElement(GlueElement element)
        {
            return element.Name + ".Generated.Event.cs";
        }



        #endregion

        public static int GetLastLocationInClass(string fullFileContents, bool startOfLine, out bool hasBracketNamespace)
        {
            // Get the closing bracket for the namespace
            int indexToInsertAt = fullFileContents.Length;

            // Update - C# now allows for namespaces without {}'s. If we have that kind of syntax, then
            // we need to do one-fewer calls below:

            hasBracketNamespace = true;
            var projectNamespace = GlueState.Self.ProjectNamespace;
            var namespaceLine = fullFileContents.IndexOf($"namespace {projectNamespace}.");
            if (namespaceLine != -1)
            {
                var endOfLineIndex = fullFileContents.IndexOf('\n', namespaceLine);
                var nextSemicolon = fullFileContents.IndexOf(';', namespaceLine);
                if (nextSemicolon != -1 && nextSemicolon < endOfLineIndex)
                {
                    hasBracketNamespace = false;
                }
            }

            if(hasBracketNamespace)
            {
                indexToInsertAt = fullFileContents.LastIndexOf('}', indexToInsertAt - 1);
            }

            // Now get the closing bracket for the class
            indexToInsertAt = fullFileContents.LastIndexOf('}', indexToInsertAt - 1);

            // Now find the previous line:
            indexToInsertAt = fullFileContents.LastIndexOf('\n', indexToInsertAt - 1);

            if (fullFileContents[indexToInsertAt - 1] == '\r')
            {
                indexToInsertAt--;
            }

            while(startOfLine && indexToInsertAt > 0 &&  
                // don't do char.IsWhitespace because that causes it to climb up a line and append to the previous line. No good
                (fullFileContents[indexToInsertAt-1] == '\t' || fullFileContents[indexToInsertAt - 1] == ' '))
            {
                indexToInsertAt--;

            }
            return indexToInsertAt;
        }

        internal static bool IsValidMethod(string changedMember)
        {
            return mAllEvents.ContainsKey(changedMember);
        }
    }
}
