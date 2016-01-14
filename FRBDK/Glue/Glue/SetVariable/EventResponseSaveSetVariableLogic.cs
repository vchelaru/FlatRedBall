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

namespace FlatRedBall.Glue.SetVariable
{
    public class EventResponseSaveSetVariableLogic
    {

        public void ReactToChange(string changedMember, object oldValue, EventResponseSave ers, IElement container)
        {
            if (changedMember == "EventName")
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
            string fullFileName = ers.GetSharedCodeFullFileName();
            if (!System.IO.File.Exists(fullFileName))
            {
                PluginManager.ReceiveError("Could not find the file " + fullFileName);
            }
            else if (CodeEditorControl.DetermineIfCodeFileIsValid(fullFileName) == false)
            {
                PluginManager.ReceiveError("Invalid code file " + fullFileName);

            }
            else
            {
                ers.EventName = oldName;
                string contents =
                    CodeEditorControl.RemoveWhiteSpaceForCodeWindow(ers.GetEventContents());

                ers.EventName = newName;
                // Now save the contents into the new method:

                if (string.IsNullOrEmpty(contents) || CodeEditorControl.HasMatchingBrackets(contents))
                {
                    EventCodeGenerator.InjectTextForEventAndSaveCustomFile(
                        container, ers, contents);
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
    }
}
