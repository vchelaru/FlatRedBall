using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficialPlugins.EffectPlugin.Managers;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Controls;
using OfficialPlugins.PostProcessingPlugin.Views;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using OfficialPlugins.EffectPlugin.ViewModels;
using Npc.ViewModels;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System.IO;
using OfficialPlugins.EffectPlugin.CodeGenerators;
using OfficialPlugins.EffectPlugin.Data;
using FlatRedBall.Glue.IO;

namespace OfficialPlugins.EffectPlugin
{
    [Export(typeof(PluginBase))]
    public class MainEffectPlugin : PluginBase
    {
        public override string FriendlyName => "Effect Plugin";

        Dictionary<ShaderContentsType, ShaderContents> ShaderContentsDictionary = new Dictionary<ShaderContentsType, ShaderContents>();

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleLoadGlux;
            this.ReactToUnloadedGlux += HandleUnloadGlux;
            this.AddNewFileOptionsHandler += ShowFxFileOptions;
            this.ReactToNewFileHandler = ReactToNewFile;
            this.ReactToFileRemoved += HandleFileRemoved;

            AddEffectContentsToDictionary();

            AssetTypeInfoManager.Initialize();
        }

        private void HandleFileRemoved(GlueElement element, ReferencedFileSave save)
        {
            var extension = FileManager.GetExtension(save.Name);

            if (extension == "fx")
            {
                var destinationFile = PostProcessCodeGenerator.CodeDestinationFileFor(save.Name);

                if(destinationFile.Exists())
                {
                    var response = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
                        $"Would you like to delete the post processing file:\n{destinationFile}");

                    if(response == System.Windows.MessageBoxResult.Yes)
                    {
                        GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(destinationFile);
                        GlueCommands.Self.TryMultipleTimes(() => FileHelper.MoveToRecycleBin(destinationFile.FullPath));
                    }
                }
            }
        }

        private void AddEffectContentsToDictionary()
        {
            AddToDictionary(new GradientColorContents());
            AddToDictionary(new SaturationContents());
            AddToDictionary(new BloomContents());
            AddToDictionary(new CrtContents());
            void AddToDictionary(ShaderContents contents) => ShaderContentsDictionary[contents.ShaderContentsType] = contents;
        }

        private void ShowFxFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            var view = new NewFxOptionsView();
            var vm = new NewFxOptionsViewModel();
            view.DataContext = vm;

            newFileWindow.AddCustomUi(view);

            newFileWindow.SelectionChanged += (_, _) =>
            {
                var ati = newFileWindow.SelectedItem;
                view.Visibility = AssetTypeInfoManager.IsFx(ati).ToVisibility();
            };

            newFileWindow.FileNameChanged += (newName) =>
            {
                vm.FxFileName = newName;
            };

            newFileWindow.GetCreationOption += () =>
            {
                var ati = newFileWindow.SelectedItem;
                return AssetTypeInfoManager.IsFx(ati) ?
                    vm :
                    null;
            };
        }

        private void ReactToNewFile(ReferencedFileSave newFile, AssetTypeInfo assetTypeInfo)
        {
            var isFx = FileManager.GetExtension(newFile.Name) == "fx";

            if(isFx)
            {
                HandleNewFx(newFile);
            }
        }

        private void HandleNewFx(ReferencedFileSave newFile)
        {
            var creationOptions = newFile.GetProperty<string>("CreationOptions");
            if (!string.IsNullOrWhiteSpace(creationOptions))
            {
                var viewModel = JsonConvert.DeserializeObject<NewFxOptionsViewModel>(creationOptions);

                if(viewModel != null)
                {
                    HandleNewFxViewModel(newFile, viewModel);
                }
            }
        }

        private void HandleNewFxViewModel(ReferencedFileSave newFile, NewFxOptionsViewModel viewModel)
        {
            if (viewModel.ShaderType == ShaderType.PostProcessing)
            {
                if(viewModel.IsIncludePostProcessCsFileChecked)
                {
                    IncludeFullscreenEffectWrapper();

                    var contents = ShaderContentsDictionary[viewModel.ShaderContentsType];

                    PostProcessCodeGenerator.ReplaceContents(newFile.Name, contents);
                    FxContentsGenerator.ReplaceContents(newFile.Name, contents);
                }
            }
        }

        private void IncludeFullscreenEffectWrapper()
        {
            FilePath destinationFile = PostProcessCodeGenerator.CodeDestinationDirectory + $"FullscreenEffectWrapper.cs";

            var assemblyContainingResource = GetType().Assembly;

            var resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.FullscreenEffectWrapper.cs";

            using var stream =
                assemblyContainingResource.GetManifestResourceStream(resourceName);
            
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string fileContent = reader.ReadToEnd();

                fileContent = fileContent.Replace("ReplaceNamespace", $"{GlueState.Self.ProjectNamespace}.EffectWrappers");
                
                var directory = destinationFile.GetDirectoryContainingThis();
                if(!System.IO.Directory.Exists(directory.FullPath))
                {
                    System.IO.Directory.CreateDirectory(directory.FullPath);
                }

                System.IO.File.WriteAllText(destinationFile.FullPath, fileContent);

                GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(destinationFile.FullPath);
            }
        }

        private void HandleLoadGlux()
        {
            var project = GlueState.Self.CurrentGlueProject;

            var vsProject = GlueState.Self.CurrentMainProject;

            var isFna = vsProject is FnaDesktopProject;

            if(isFna)
            {
                this.AddAssetTypeInfo(AssetTypeInfoManager.FxbEffectAssetTypeInfo);
            }
        }

        private void HandleUnloadGlux()
        {
            this.UnregisterAssetTypeInfos();
        }
    }
}
