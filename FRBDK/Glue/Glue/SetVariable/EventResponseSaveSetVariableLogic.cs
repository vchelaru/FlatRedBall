using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Events;
using FlatRedBall.IO;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.SetVariable
{
    public class EventResponseSaveSetVariableLogic
    {

        public void ReactToChange(string changedMember, object oldValue, EventResponseSave ers, IElement container)
        {
            if (changedMember == nameof(EventResponseSave.EventName))
            {
                ReactToEventRename(oldValue, ers, container);
            }
        }

        private static void ReactToEventRename(object oldValue, EventResponseSave ers, IElement container)
        {
            string oldName = oldValue as string;
            string newName = ers.EventName;
            // The code
            // inside this
            // event handler
            // are saved in the
            // Element.Event.cs file
            // so that it can be edited
            // in Visual Studio.  If the
            // EventResponseSave changes then
            // it will use a new method.  We need
            // to take out the old method and move
            // the contents to the new method.

            // We'll "cheat" by setting the name to the old
            // one and getting the contents, then switching it
            // back to the new:
            string fullFileName = ers.GetCustomEventFullFileName();
            if (!System.IO.File.Exists(fullFileName))
            {
                PluginManager.ReceiveError("Could not find the file " + fullFileName);
            }
            else if (DetermineIfCodeFileIsValid(fullFileName) == false)
            {
                PluginManager.ReceiveError("Invalid code file " + fullFileName);
            }
            else
            {
                ers.EventName = oldName;
                string contents =
                    RemoveWhiteSpaceForCodeWindow(ers.GetEventContents());

                ers.EventName = newName;
                // Now save the contents into the new method:

                if (string.IsNullOrEmpty(contents) || HasMatchingBrackets(contents))
                {
                    EventCodeGenerator.InjectTextForEventAndSaveCustomFile(
                        container as GlueElement, ers, contents);
                    PluginManager.ReceiveOutput("Saved " + ers);
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                    DialogResult result = MessageBox.Show("Would you like to delete the old method On" + oldName + "?", "Delete old function?", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        int startIndex;
                        int endIndex;

                        contents = FileManager.FromFileText(fullFileName);

                        EventCodeGenerator.GetStartAndEndIndexForMethod(contents,
                            "On" + oldName, out startIndex, out endIndex);

                        contents = contents.Remove(startIndex, endIndex - startIndex);

                        FileManager.SaveText(contents, fullFileName);
                    }
                }
                else
                {
                    PluginManager.ReceiveError("Mismatch of } and { in event " + ers);

                }
            }
        }

        private static bool HasMatchingBrackets(string text)
        {
            string contentsWithoutComments = ParsedClass.RemoveComments(text);

            int numberOfOpening = contentsWithoutComments.CountOf('{');
            int numberOfClosing = contentsWithoutComments.CountOf('}');

            return numberOfOpening == numberOfClosing;
        }

        private static string RemoveWhiteSpaceForCodeWindow(string textToAssign)
        {
            if (!string.IsNullOrEmpty(textToAssign))
            {
                textToAssign = textToAssign.Replace("\r\r", "\r");
                textToAssign = textToAssign.Replace("\n\t\t", "\n");
                textToAssign = textToAssign.Replace("\r\n\t", "\r\n");
                textToAssign = textToAssign.Replace("\r\n\t\t", "\r\n");
                textToAssign = textToAssign.Replace("\r\n            ", "\r\n");
                textToAssign = textToAssign.Replace("\n            ", "\n");
                if (textToAssign.StartsWith("            "))
                {
                    textToAssign = textToAssign.Substring(12);
                }
            }
            return textToAssign;
        }

        /// <summary>
        /// Determines if a code file is valid based off of the number of opening and closing
        /// brackets it has.  This method counts { and }, but doesn't include comments or contsts like
        /// "{0}".
        /// </summary>
        /// <param name="fileName">The file name to open - this should be the .cs file for C# files.</param>
        /// <returns>Whether the file is valid.</returns>
        private static bool DetermineIfCodeFileIsValid(string fileName)
        {
            string contents = FileManager.FromFileText(fileName);

            contents = ParsedClass.RemoveComments(contents);


            int numberOfOpenBrackets = ParsedClass.NumberOfValid('{', contents);
            int numberOfClosedBrackets = ParsedClass.NumberOfValid('}', contents);

            return numberOfOpenBrackets == numberOfClosedBrackets;
        }
    }
}
