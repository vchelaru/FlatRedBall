using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Events
{
    class EventManagerExtensionMethods
    {

        internal static void InsertMethodCallInElementIfNecessary(IElement iElement, string methodName)
        {
            string currentFileName = FileManager.RelativeDirectory + iElement.Name + ".Events.cs";

            string className = FileManager.RemovePath(iElement.Name);

            string fullFileContents = null;

            if (!FileManager.FileExists(currentFileName))
            {
                string startingTemplate = Resources.Resource1.EventTemplate;

                startingTemplate = startingTemplate.Replace("CLASS___NAME", className);

                string namespaceName =
                    ProjectManager.ProjectNamespace + '.' +
                    FileManager.GetDirectoryKeepRelative(iElement.Name).Replace("\\", ".");
                // remove the ending slash:
                namespaceName = namespaceName.Substring(0, namespaceName.Length - 1);

                startingTemplate = startingTemplate.Replace("NAMESPACE___NAME", namespaceName);

                FileManager.SaveText(startingTemplate, currentFileName);

                ProjectManager.CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile(currentFileName, false);
                fullFileContents = startingTemplate;
            }
            else
            {
                fullFileContents = FileManager.FromFileText(currentFileName);
            }

            #region Find the parsedClassToUse

            ParsedNamespace parsedNamespace = new ParsedNamespace(fullFileContents);

            ParsedClass parsedClassToUse = null;

            foreach (ParsedClass parsedClass in parsedNamespace.Classes)
            {
                if (parsedClass.Name == className)
                {
                    parsedClassToUse = parsedClass;
                    break;
                }
            }

            #endregion

            if (parsedClassToUse != null)
            {
                // See if there is already a method here
                if (parsedClassToUse.GetMethod(methodName) == null)
                {
                    DialogResult result =
                        MessageBox.Show("The method\n\n" + methodName + "\n\ndoes not exist.  Create this " +
                            "method in your .Event.cs code file?", "Create method?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        int indexToInsertAt = EventManager.GetLastLocationInClass(fullFileContents, startOfLine:true, out bool hasBracketNamespace);

                        var tabPrefix = hasBracketNamespace ? "\t\t" : "\t";

                        string newMethodCode =
                            $"\r\n{tabPrefix}void " + methodName + "(object sender, EventArgs e)\r\n" +
                            $"{tabPrefix}{{\r\n" +
                            $"{tabPrefix}\t\r\n" +
                            $"{tabPrefix}}}\r\n";


                        fullFileContents = fullFileContents.Insert(indexToInsertAt, newMethodCode);

                        FileManager.SaveText(fullFileContents, currentFileName);


                    }
                }
            }
        }
    }
}
