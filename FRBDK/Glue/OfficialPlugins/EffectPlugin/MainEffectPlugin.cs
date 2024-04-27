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

namespace OfficialPlugins.EffectPlugin
{
    [Export(typeof(PluginBase))]
    public class MainEffectPlugin : PluginBase
    {
        public override string FriendlyName => "Effect Plugin"; 

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleLoadGlux;
            this.ReactToUnloadedGlux += HandleUnloadGlux;
            this.AddNewFileOptionsHandler += ShowFxFileOptions;
            this.ReactToNewFileHandler = ReactToNewFile;

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
                view.Visibility = IsFx(ati).ToVisibility();
            };

            newFileWindow.FileNameChanged += (newName) =>
            {
                vm.FxFileName = newName;
            };

            newFileWindow.GetCreationOption += () =>
            {
                var ati = newFileWindow.SelectedItem;
                return IsFx(ati) ?
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
            if(viewModel.IsIncludePostProcessCsFileChecked)
            {
                IncludeFullscreenEffectWrapper();
                IncludePostProcessCsFile(newFile.Name);
            }
        }

        private void IncludeFullscreenEffectWrapper()
        {
            FilePath destinationFile = GlueState.Self.CurrentGlueProjectDirectory + $"Graphics/FullscreenEffectWrapper.cs";

            var assemblyContainingResource = GetType().Assembly;

            var resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.FullscreenEffectWrapper.cs";

            using var stream =
                assemblyContainingResource.GetManifestResourceStream(resourceName);
            
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string fileContent = reader.ReadToEnd();

                fileContent = fileContent.Replace("ReplaceNamespace", $"{GlueState.Self.ProjectNamespace}.Graphics");
                
                var directory = destinationFile.GetDirectoryContainingThis();
                if(!System.IO.Directory.Exists(directory.FullPath))
                {
                    System.IO.Directory.CreateDirectory(directory.FullPath);
                }

                System.IO.File.WriteAllText(destinationFile.FullPath, fileContent);

                GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(destinationFile.FullPath);
            }
        }

        private void IncludePostProcessCsFile(string fxFileName)
        {
            string newDirectory = Path.Combine("Graphics", Path.GetDirectoryName(fxFileName) ?? "");
            string newFileName = Path.GetFileName(Path.ChangeExtension(fxFileName, "cs") ?? "");
            string newFileNameOnly = Path.GetFileNameWithoutExtension(Path.GetFileName(Path.ChangeExtension(fxFileName, "cs")) ?? "");
            string newFileRelativePath = Path.Combine(newDirectory, newFileName);
            FilePath destinationFile = GlueState.Self.CurrentGlueProjectDirectory + newFileRelativePath;

            var assemblyContainingResource = GetType().Assembly;

            var resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.PostProcessTemplate.cs";

            using var stream =
                assemblyContainingResource.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string fileContent = reader.ReadToEnd();

                fileContent = fileContent.Replace("ReplaceNamespace", $"{GlueState.Self.ProjectNamespace}.{newDirectory.Replace('\\', '.')}");

                if (newFileNameOnly.Length < 1)
                {
                    throw new ArgumentException("FX file name must have at least 1 character");
                }
                
                char firstLetter = char.ToUpper(newFileNameOnly[0]);
                fileContent = fileContent.Replace("ReplaceClassName", firstLetter + newFileNameOnly[1..]);
                
                fileContent = fileContent.Replace("ReplaceClassMembers",
                    "protected FullscreenEffectWrapper Wrapper { get; set; } = new FullscreenEffectWrapper();");

                fileContent = fileContent.Replace("ReplaceApplyBody", @"_effect.Parameters[""TexWeight""].SetValue(1.0f);
            _effect.Parameters[""PixelPosWeight""].SetValue(0.0f);
            _effect.Parameters[""ScreenPosWeight""].SetValue(1.0f);
            _effect.Parameters[""WorldPosWeight""].SetValue(0.0f);
            _effect.Parameters[""ColorWeight""].SetValue(0.0f);
            _effect.Parameters[""UvWeight""].SetValue(0.0f);
            
            Wrapper.Draw(Camera.Main, _effect, sourceTexture);");
                
                var directory = destinationFile.GetDirectoryContainingThis();
                if(!System.IO.Directory.Exists(directory.FullPath))
                {
                    System.IO.Directory.CreateDirectory(directory.FullPath);
                }

                System.IO.File.WriteAllText(destinationFile.FullPath, fileContent);

                GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(destinationFile.FullPath);
            }
        }

        bool IsFx(AssetTypeInfo ati) => ati?.Extension == "fx";

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
