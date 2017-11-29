using EditorObjects.IoC;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
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
        }

        private void HandleReactToRfsValueChanged(string variableName, object oldValue)
        {
            var glueState = Container.Get<IGlueState>();

            if(glueState.CurrentReferencedFileSave?.IsDatabaseForLocalizing == true)
            {
                TryGenerateStringConsts();
            }
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
                var contents = GetStringsGeneratedCodeFileContents(referencedFileSave);

                string fileName = $"DataTypes/Strings.Generated.cs";

                TaskManager.Self.AddSync(() =>
                {
                    glueCommands.ProjectCommands.CreateAndAddCodeFile(fileName);

                    try
                    {
                        glueCommands.TryMultipleTimes(() => System.IO.File.WriteAllText(glueState.CurrentGlueProjectDirectory +  fileName, contents), 5);
                    }
                    catch (Exception e)
                    {
                        glueCommands.PrintError(e.ToString());
                    }
                }, "Adding localization string consts");

            }
        }

        private string GetStringsGeneratedCodeFileContents(ReferencedFileSave referencedFileSave)
        {
            var glueState = Container.Get<IGlueState>();
            var glueCommands = Container.Get<IGlueCommands>();

            string toReturn = null;

            if(referencedFileSave != null)
            {
                var namespaceName = $"{glueState.ProjectNamespace}.DataTypes";

                ICodeBlock document = new CodeDocument(0);
                ICodeBlock codeBlock = document.Namespace(namespaceName);
                codeBlock = codeBlock.Class("Strings");

                string fileName = glueCommands.GetAbsoluteFileName(referencedFileSave);
                var runtime = CsvFileManager.CsvDeserializeToRuntime(fileName);


                foreach(var row in runtime.Records)
                {
                    if(row.Length > 1)
                    {
                        var stringId = row[0];
                        // assume english is row 1
                        var value = row[1];

                        codeBlock.Line($"/// <summary>{value}</summary>");
                        codeBlock.Line($"public const string {stringId} = \"{stringId}\";");
                    }
                }

                toReturn = document.ToString();
            }

            return toReturn;
        }
    }
}
