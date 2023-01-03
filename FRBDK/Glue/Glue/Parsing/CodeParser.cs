using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.SaveClasses;
using System.Collections;
//using FlatRedBall.Gui;

namespace FlatRedBall.Glue.Parsing
{
    public static class CodeParser
    {
        public static bool IsEntity(string fileName)
        {
            string expectedClassName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

            if (InheritsFrom(fileName, "PositionedObject", expectedClassName))
            {
                return true;
            }



            // but hold on, it might be a partial!
            string generatedFile = FileManager.RemoveExtension(fileName) + ".Generated.cs";

            // Okay, this is really cheap, but I'm in a hurry.  We should fix this for sure.
            if (File.Exists(FileManager.RelativeDirectory + generatedFile) &&
                FileManager.MakeRelative(FileManager.GetDirectory(fileName)).StartsWith("Entities/"))
            {
                return true;
            }

            // If we got here, it still might be an Entity, it just doesn't have its generated code yet

            string modifiedFileName = FileManager.RemoveExtension(fileName).ToLower();

            for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
            {
                EntitySave es = ProjectManager.GlueProjectSave.Entities[i];

                if (es.Name.ToLower() == modifiedFileName)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsScreen(string fileName)
        {
            if (InheritsFrom(fileName, "Screen"))
            {
                return true;
            }

            // but hold on, it might be a partial!
            string generatedFile = FileManager.RemoveExtension(fileName) + ".Generated.cs";

            // Okay, this is really cheap, but I'm in a hurry.  We should fix this for sure.
            if (File.Exists(FileManager.RelativeDirectory + generatedFile) &&
                FileManager.RemovePath(FileManager.GetDirectory(fileName)) == "Screens/")
            {
                return true;
            }

            string modifiedFileName = FileManager.RemoveExtension(fileName).ToLower();

            for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
            {
                ScreenSave ss = ProjectManager.GlueProjectSave.Screens[i];

                if (ss.Name.ToLower() == modifiedFileName)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool InheritsFrom(string fileName, string baseClass)
        {
            return InheritsFrom(fileName, baseClass, null);
        }

        public static bool InheritsFrom(string fileName, string baseClass, string nameToMatch)
        {
            fileName = FileManager.Standardize(fileName);

            if (FileManager.FileExists(fileName))
            {

                ParsedFile parsedFile = new ParsedFile(fileName);

                if (parsedFile.Namespaces.Count != 0)
                {
                    for (int i = 0; i < parsedFile.Namespaces[0].Classes.Count; i++)
                    {
                        ParsedClass parsedClass = parsedFile.Namespaces[0].Classes[i];

                        if (nameToMatch != null && parsedClass.Name != nameToMatch)
                        {
                            continue;
                        }

                        foreach (ParsedType parsedType in parsedClass.ParentClassesAndInterfaces)
                        {
                            if (parsedType.ToString() == baseClass)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Looking for the Game class.  The file  " + fileName + " is part of the project but couldn't find it on disk.");
                return false;
            }
        }

        public static string ConvertValueToCodeString<T>(T objectToParse)
        {

            string value = "";
            value = objectToParse?.ToString();

            if (objectToParse is bool || typeof(T) == typeof(bool?))
            {
                if (value == null)
                {
                    value = "null";
                }
                else
                {
                    value = value.ToLower();
                }
            }
            else if (objectToParse is float || typeof(T) == typeof(float?))
            {
                if (objectToParse == null)
                {
                    value = "null";
                }
                else if (float.IsPositiveInfinity((float)(object)objectToParse))
                {
                    value = "float.PositiveInfinity";
                }
                else if (float.IsNegativeInfinity((float)(object)objectToParse))
                {
                    value = "float.NegativeInfinity";
                }
                else
                {
                    string adjusted = ((float)(object)objectToParse).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    value = adjusted + "f";
                }
            }
            else if (objectToParse is double || typeof(T) == typeof(double?))
            {
                if (objectToParse == null)
                {
                    value = "null";
                }
                else
                {
                    value = ((double)(object)objectToParse).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                }
            }
            else if(objectToParse is decimal || typeof(T) == typeof(decimal?))
            {
                if (objectToParse == null)
                {
                    value = "null";
                }
                else
                {
                    value = ((decimal)(object)objectToParse).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    value = value + "m";
                }
            }
            else if (objectToParse is string || typeof(T) == typeof(string))
            {
                if (value == null)
                {
                    value = "null";
                }
                else
                {
                    value = "\"" + value?.ToString() + "\"";
                }
            }
            else if (objectToParse?.GetType().IsEnum == true)
            {
                value = objectToParse.GetType().FullName + "." + value.ToString();
                // This may be an enumeration contained inside a class.  If so, the ToString
                // will return a value with the '+' character separating the container class 
                // and the name of the Enum
                if (value.Contains("+"))
                {
                    value = value.Replace("+", ".");
                }
            }
            else if(objectToParse is IEnumerable)
            {
                if(objectToParse is List<string> stringList)
                {
                    string innerInstantiation = String.Empty;

                    var isFirst = true;
                    foreach(var item in stringList)
                    {
                        if(!isFirst)
                        {
                            innerInstantiation += ", ";
                        }
                        innerInstantiation += $"\"{item}\"";
                        isFirst = false;
                    }
                    value = "new System.Collections.Generic.List<string> { " + innerInstantiation + "}";
                }
            }

            return value;
        }


        public static int GetIndexAfterBaseInitialize(string contents)
        {
            // As of October 28, 2019
            // new templates have this:
            int index = contents.IndexOf("Type startScreenType = null;");

            if(index == -1)
            {
                index = contents.IndexOf("ScreenManager.Start(");
            }

            return index;
        }

        public static int GetIndexAfterFlatRedBallInitialize(string contents)
        {
            int index = contents.IndexOf("FlatRedBallServices.InitializeFlatRedBall");
            if (index == -1)
            {
                return -1;
            }
            else
            {
                index = contents.IndexOfAny(
                    new char[] { '\n', '\r' },
                    index) + 2;

                return index;
            }
        }

        
    }
}