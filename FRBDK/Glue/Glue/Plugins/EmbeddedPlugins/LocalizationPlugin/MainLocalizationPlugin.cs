using EditorObjects.IoC;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LocalizationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainLocalizationPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleGluxLoad;
            this.ReactToReferencedFileChangedValueHandler += HandleReactToRfsValueChanged;
            // This is if the user modifies the file while Glue is open
            this.ReactToFileChange += HandleFileChanged;
        }

        private void HandleFileChanged(FilePath filePath, FileChangeType fileChangeType)
        {
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath);

            if (rfs?.IsDatabaseForLocalizing == true)
            {
                TryGenerateStringConsts(rfs);
            }
        }

        private void HandleReactToRfsValueChanged(string variableName, object oldValue)
        {
            TryGenerateStringConsts();
        }

        private void HandleGluxLoad()
        {
            TryGenerateStringConsts();
        }

        private void TryGenerateStringConsts()
        {
            var glueCommands = Container.Get<IGlueCommands>();
            var glueState = Container.Get<IGlueState>();

            var referencedFileSave = glueState.GetAllReferencedFiles().FirstOrDefault(item => item.IsDatabaseForLocalizing);
            if (referencedFileSave != null)
            {
                TryGenerateStringConsts(referencedFileSave);

            }
        }

        private void TryGenerateStringConsts(ReferencedFileSave referencedFileSave)
        {


            TaskManager.Self.Add(() =>
            {
                var glueCommands = Container.Get<IGlueCommands>();
                var glueState = Container.Get<IGlueState>();

                var contents = GetStringsGeneratedCodeFileContents(referencedFileSave);
                string fileName = $"DataTypes/Strings.Generated.cs";
                glueCommands.ProjectCommands.CreateAndAddCodeFile(fileName);

                try
                {
                    glueCommands.TryMultipleTimes(() =>
                    {
                        //System.IO.File.WriteAllText(glueState.CurrentGlueProjectDirectory + fileName, contents);
                        GlueCommands.Self.FileCommands.SaveIfDiffers(glueState.CurrentGlueProjectDirectory + fileName, contents);
                    });
                }
                catch (Exception e)
                {
                    glueCommands.PrintError(e.ToString());
                }
            }, "Adding localization string consts");
        }

        private string GetStringsGeneratedCodeFileContents(ReferencedFileSave referencedFileSave)
        {
            var glueState = Container.Get<IGlueState>();
            var glueCommands = Container.Get<IGlueCommands>();

            string toReturn = null;

            if (referencedFileSave != null)
            {
                var namespaceName = $"{glueState.ProjectNamespace}.DataTypes";

                ICodeBlock document = new CodeDocument(0);
                ICodeBlock codeBlock = document.Namespace(namespaceName);
                codeBlock = codeBlock.Class("Strings");

                string fileName = glueCommands.GetAbsoluteFileName(referencedFileSave);

                var doesFileExist =
                    System.IO.File.Exists(fileName);


                if (System.IO.File.Exists(fileName))
                {
                    var runtime = CsvFileManager.CsvDeserializeToRuntime(fileName);


                    foreach (var row in runtime.Records)
                    {
                        TryAddMemberForRow(codeBlock, row);
                    }

                    toReturn = document.ToString();
                }
            }

            return toReturn;
        }

        private static void TryAddMemberForRow(ICodeBlock codeBlock, string[] row)
        {
            bool shouldProcess = row.Length > 1 &&
                !string.IsNullOrWhiteSpace(row[0]) &&
                row[0].StartsWith("//") == false;


            if (shouldProcess)
            {
                var stringId = row[0];

                string memberName = GetMemberNameFor(stringId);

                // assume english is row 1
                var comments = row[1].Split('\n');
                if (comments.Length == 1)
                {
                    codeBlock.Line($"/// <summary>{row[1]}</summary>");
                }
                else
                {
                    codeBlock.Line($"/// <summary>");
                    foreach (var line in comments)
                    {
                        codeBlock.Line($"/// {line?.Trim()}");
                    }
                    codeBlock.Line($"/// </summary>");

                }
                codeBlock.Line($"public const string {memberName} = \"{stringId}\";");
            }
        }

        private static string GetMemberNameFor(string stringId)
        {
            var toReturn = stringId;
            if (char.IsDigit(toReturn[0]))
            {
                toReturn = "_" + toReturn;
            }

            if (toReturn.Contains(' '))
            {
                toReturn = toReturn.Replace(' ', '_');
            }

            foreach (var invalidCharacter in NameVerifier.InvalidCharacters)
            {
                if (toReturn.Contains(invalidCharacter))
                {
                    toReturn = toReturn.Replace(invalidCharacter, '_');
                }
            }

            return toReturn;
        }
    }
}
