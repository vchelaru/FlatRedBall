using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.IO;
using System.IO;

namespace CodeTranslator
{

    public enum Language
    {
        ActionScript,
        Java
    }

    /*
    public static class TranslationHelpers
    {



        public static void PerformTranslation(bool translateClasses)
        {
            TranslatedFileSave translatedFileSave = EditorLogic.CurrentTranslatedFileSave;

            ParsedClass selectedClass = null;

            if (translatedFileSave.ParsedNamespace != null && translatedFileSave.ParsedNamespace.Classes.Count != 0)
            {
                selectedClass = translatedFileSave.ParsedNamespace.Classes[0];
            }

            if (translatedFileSave.ParsedNamespace == null)
            {
                translatedFileSave.ParseAndSetValues(translatedFileSave.FileName);
                selectedClass = translatedFileSave.ParsedNamespace.Classes[0];
                ParseAndSetInheritanceChain(selectedClass);
            }
            else if (translatedFileSave.ParsedNamespace.Classes.Count != 0 &&
                selectedClass.ParentClassesAndInterfaces.Count != 0 &&
                selectedClass.ParentParsedClasses.Count == 0)
            {
                // This means that the class knows about interfaces, but hasn't yet parsed them
                ParseAndSetInheritanceChain(selectedClass);
            }

            if (selectedClass.RuntimeType == null)
            {
                selectedClass.RuntimeType = TypeManager.GetTypeFromString(selectedClass.Name);
            }

            #region If a file is not selected, display a message

            if (translatedFileSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You must first select a class to translate");
            }

            #endregion

            else
            {
                #region Get current extension
                string extension = "";

                Language currentLanguage = EditorLogic.CurrentLanguage;

                if (currentLanguage == Language.ActionScript)
                {
                    extension = ".as";
                }
                else if (currentLanguage == Language.Java)
                {
                    extension = ".java";
                }
                else
                {
                    throw new NotImplementedException();
                }
                #endregion

                List<string> filesTranslated = new List<string>();

                #region Translate Classes

                if (translateClasses)
                {
                    for (int i = 0; i < translatedFileSave.ParsedNamespace.Classes.Count; i++)
                    {
                        string absoluteTargetFile = "";

                        absoluteTargetFile = TranslateAndSaveClass(translatedFileSave, extension, i);
                        filesTranslated.Add(absoluteTargetFile);
                    }
                }

                #endregion

                TranslateAndSaveEnum(translatedFileSave, extension, filesTranslated);

                ProjectManager.Save();
#if CODE_TRANSLATOR
                #region Display the successfully translated files

                string successString = "Successfully translated:\n";

                if (filesTranslated.Count != 0)
                {
                    foreach (string file in filesTranslated)
                    {
                        successString += file + "\n";
                    }
                    
                    System.Windows.Forms.MessageBox.Show(successString);
                }

                #endregion
#endif
            }
        }

        public static string TranslateAndSaveClass(TranslatedFileSave translatedFileSave, string extension, int index)
        {

            ParsedClass parsedClass = translatedFileSave.ParsedNamespace.Classes[index];

            string tempString = "";
#if CODE_TRANSLATOR
            if (Form1.Self.IsTesting)
            {
                tempString = "Testing";
            }
#endif

            // We can't just move the file over.  Java complains if the namespace and the
            // directory aren't the same.  So, we gotta use the namespace to determine its location
            string name = parsedClass.Namespace + "." + parsedClass.Name;

            string relativeFileName = name.Substring(ProjectManager.Namespace.Length + 1).Replace('.', '\\');

            string absoluteTargetFile =
                ProjectManager.TargetProjectDirectory[EditorLogic.CurrentLanguage] + relativeFileName + tempString + extension;

            if (absoluteTargetFile.Contains('<'))
            {
                // This is a generic, so let's eliminate the contents of the generic block
                int indexOfOpen = absoluteTargetFile.IndexOf('<');
                int indexOfClose = absoluteTargetFile.IndexOf('>');

                string stringBefore = absoluteTargetFile.Substring(0, indexOfOpen);

                string stringAfter = absoluteTargetFile.Substring(indexOfClose + 1, absoluteTargetFile.Length - indexOfClose - 1);

                absoluteTargetFile = stringBefore + stringAfter;

            }

            TranslationManager.TranslateClass(
                translatedFileSave,
                index,
                absoluteTargetFile,
                EditorLogic.CurrentLanguage);
            return absoluteTargetFile;
        }


        private static void ParseAndSetInheritanceChain(ParsedClass baseClass)
        {
            string currentClassName;
            TranslatedFileSave currentFileSave;
            string currentFileName;
            string qualifiedName;

            for (int i = 0; i < baseClass.ParentClassesAndInterfaces.Count; ++i)
            {
                currentClassName = baseClass.ParentClassesAndInterfaces[i].NameWithGenericNotation;
                currentFileSave = CodeTranslator.ProjectManager.TranslatedProjectSave.GetUnqualifiedTranslatedFileSaveFromName(currentClassName);
                qualifiedName = TranslationManager.GetQualifiedFromUnqualifiedName(currentClassName, Language.Java);

                if (string.IsNullOrEmpty(qualifiedName) || !qualifiedName.StartsWith("com." + CodeTranslator.ProjectManager.Namespace))
                {
                    continue;
                }

                currentFileName = TranslationManager.GetPredictedFileNameFromJavaQualifiedName(qualifiedName);

                if (FileManager.FileExists(FileManager.RemoveExtension(currentFileName) + ".generated.cs"))
                {
                    currentFileName = FileManager.RemoveExtension(currentFileName) + ".generated.cs";
                }


                if (currentFileSave == null)
                {

                    if (!File.Exists(currentFileName))
                    {
                        int m = 9001;
                    }

                    CodeTranslator.ProjectManager.AddTranslatedClassSave(currentFileName);
                    currentFileSave = CodeTranslator.ProjectManager.TranslatedProjectSave.GetTranslatedFileSave(currentFileName);

                }


                if (currentFileSave.ParsedNamespace == null)
                {
                    currentFileSave.ParseAndSetValues(currentFileName);
                }

                ParseAndSetInheritanceChain(currentFileSave.ParsedNamespace.Classes[0]);

                baseClass.ParentParsedClasses.Add(currentFileSave.ParsedNamespace.Classes[0]);

            }
        }

        public static void TranslateAndSaveEnum(TranslatedFileSave translatedFileSave, string extension, List<string> filesCreated)
        {
            if (translatedFileSave.ParsedNamespace.Enums.Count == 0)
            {
                return;
            }

            for (int i = 0; i < translatedFileSave.ParsedNamespace.Enums.Count; i++)
            {
                ParsedEnum parsedEnum = translatedFileSave.ParsedNamespace.Enums[i];


                string tempString = "";
#if CODE_TRANSLATOR
                if (Form1.Self.IsTesting)
                {
                    tempString = "Testing";
                }
#endif
                // We can't just move the file over.  Java complains if the namespace and the
                // directory aren't the same.  So, we gotta use the namespace to determine its location
                string name = parsedEnum.Namespace + "." + parsedEnum.Name;

                string relativeFileName = name.Substring(ProjectManager.Namespace.Length + 1).Replace('.', '\\');

                string absoluteTargetFile =
                    ProjectManager.TargetProjectDirectory[EditorLogic.CurrentLanguage] + relativeFileName + tempString + extension;

                //if (absoluteTargetFile.Contains('<'))
                //{
                //    // This is a generic, so let's eliminate the contents of the generic block
                //    int indexOfOpen = absoluteTargetFile.IndexOf('<');
                //    int indexOfClose = absoluteTargetFile.IndexOf('>');

                //    string stringBefore = absoluteTargetFile.Substring(0, indexOfOpen);

                //    string stringAfter = absoluteTargetFile.Substring(indexOfClose + 1, absoluteTargetFile.Length - indexOfClose - 1);

                //    absoluteTargetFile = stringBefore + stringAfter;

                //}

                TranslationManager.TranslateEnum(
                    translatedFileSave,
                    absoluteTargetFile,
                    EditorLogic.CurrentLanguage, parsedEnum);

                filesCreated.Add(absoluteTargetFile);
            }

        }
    }
     */
}
