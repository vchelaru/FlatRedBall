using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.ComponentModel.Composition;
using System.IO;
using TutorialPlugin.Controls;
using TutorialPlugin.ViewModels;

namespace TutorialPlugin
{
    [Export(typeof(PluginBase))]
    public class MainTutorialPlugin : PluginBase
    {
        private MainControl _mainControl;
        private PluginTab _tab;
        private MainControlViewModel _viewModel;

        public override string FriendlyName => "Tutorial Plugin";

        public override Version Version => new(1, 0);

        public override void StartUp()
        {
            ReactToItemSelectHandler += HandleItemSelected;
            ReactToFileChange += HandleFileChanged;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            var currentFile = GlueState.Self.CurrentReferencedFileSave;

            if (currentFile == null)
            {
                _tab?.Hide();
            }
            else
            {
                if (_tab == null)
                {
                    _viewModel = new MainControlViewModel();
                    _mainControl = new MainControl() { DataContext = _viewModel };
                    _tab = CreateAndAddTab(_mainControl, "Tutorial Plugin");
                }

                _tab.Show();

                UpdateToFullFile(currentFile.FilePath);
            }
        }

        private void HandleFileChanged(FilePath filePath, FileChangeType fileChangeType)
        {
            var currentFile = GlueState.Self.CurrentReferencedFileSave;

            if (currentFile != null && currentFile.FilePath == filePath)
            {
                UpdateToFullFile(currentFile.FilePath);
            }
        }

        private void UpdateToFullFile(FilePath filePath)
        {
            _viewModel.FileNameDisplay = $"File Name: {filePath.FullPath}";

            var writeTime = File.GetLastWriteTime(filePath.FullPath);
            _viewModel.WriteTimeDisplay = $"Last write time: {writeTime}";
        }
    }
}