using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.GuiDisplay.Facades;
using System.IO;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Events
{
    public static class EventResponseSaveExtensionMethodsGlue
    {
        public static string GetEventContents(this EventResponseSave instance)
        {
            GlueElement element = instance.GetContainer();

            string textToAssign = null;

            if (FacadeContainer.Self.GlueState.CurrentEventResponseSave != null)
            {
                if (!string.IsNullOrEmpty(instance.Contents))
                {
                    textToAssign = instance.Contents;
                }
                else
                {
                    // Is there a non-Generated.Event.cs file?
                    string fileToLookFor = FileManager.RelativeDirectory +
                        EventResponseSave.GetCustomEventFileNameForElement(element);

                    if (File.Exists(fileToLookFor))
                    {
                        ParsedMethod parsedMethod =
                            instance.GetParsedMethodFromAssociatedFile();

                        if (parsedMethod != null)
                        {
                            textToAssign =
                                parsedMethod.MethodContents;
                        }
                    }
                }
            }
            else
            {
                textToAssign = null;
            }
            return textToAssign;
        }

        public static string GetCustomEventFullFileName(this EventResponseSave instance)
        {
            var container = instance.GetContainer();
            if (container != null)
            {
                return GlueState.Self.CurrentGlueProjectDirectory + EventResponseSave.GetCustomEventFileNameForElement(instance.GetContainer());
            }
            else
            {
                return null;
            }
        }

        public static ParsedMethod GetParsedMethodFromAssociatedFile(this EventResponseSave instance)
        {
            string fullFileName = instance.GetCustomEventFullFileName();

            return EventResponseSaveExtensionMethods.GetParsedMethodFromAssociatedFile(fullFileName, instance);
        }
    }
}
