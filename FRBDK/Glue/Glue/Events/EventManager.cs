using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using System.Windows.Forms;

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
            string fileToLoad = FlatRedBall.IO.FileManager.MakeAbsolute(
                @"Content/BuiltInEvents.csv");
            
            try
            {

                CsvFileManager.CsvDeserializeDictionary<string, EventSave>(fileToLoad, mAllEvents);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Could not initialize event manager.  Check to make sure " +
                    "the following file exists and is not corrupt: " + fileToLoad);
            }

        }

        public static string GetGeneratedEventFileNameForElement(IElement element)
        {
            return element.Name + ".Generated.Event.cs";
        }



        #endregion




        public static int GetLastLocationInClass(string fullFileContents, bool startOfLine)
        {
            // Get the closing bracket for the namespace
            int indexToInsertAt = fullFileContents.LastIndexOf('}', fullFileContents.Length - 1);

            // Now get the closing bracket for the class
            indexToInsertAt = fullFileContents.LastIndexOf('}', indexToInsertAt - 1);

            // Now find the previous line:
            indexToInsertAt = fullFileContents.LastIndexOf('\n', indexToInsertAt - 1);

            if (fullFileContents[indexToInsertAt - 1] == '\r')
            {
                indexToInsertAt--;
            }

            while(startOfLine && indexToInsertAt > 0 && fullFileContents[indexToInsertAt-1] == '\t')
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
