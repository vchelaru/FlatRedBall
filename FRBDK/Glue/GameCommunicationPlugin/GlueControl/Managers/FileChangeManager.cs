using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.IO;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.Managers
{


    class FileChangeManager 
    {
        string[] copiedExtensions = new[]
        {
            "csv",
            "txt",
            "png",
            "tmx",
            "tsx",
            "bmp",
            "png",
            "achx",
            "emix",
            "json",
            "xnb"
        };
        private Action<string> _output;
        private RefreshManager _refreshManager;
        CompilerViewModel viewModel;

        string[] ignoredFilesForCopying = new[]
        {
            "gum_events.json",
            "GumLastChangeFilePath.txt"
        };


        public FileChangeManager(Action<string> output, CompilerViewModel viewModel, RefreshManager refreshManager)
        {
            this._output = output;
            _refreshManager = refreshManager;
            this.viewModel = viewModel;
        }

        public void HandleFileChanged(FilePath filePath, FileChangeType changeType)
        {
            if (changeType != FileChangeType.Modified &&
                // tiled renames when saving
                changeType != FileChangeType.Renamed &&
                // Some aps delete/recreate:
                changeType != FileChangeType.Created)
            {
                return;
            }
            // If a file changed, always copy it over - why only do so if we're in edit mode?

            var extension = filePath.Extension;

            ToolbarEntityViewModelManager.ReactToFileChanged(filePath);

            var shouldCopy = copiedExtensions.Contains(extension);

            if(shouldCopy)
            {
                shouldCopy = !IsFileIgnored(filePath);
            }

            if (shouldCopy)
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(filePath);

            }

            _refreshManager.HandleFileChanged(filePath);
        }

        private bool IsFileIgnored(FilePath fileName)
        {
            var settingsFolder = GlueState.Self.ProjectSpecificSettingsPath;

            if(settingsFolder.IsRootOf(fileName))
            {
                return true;
            }


            var strippedFileName = fileName.NoPath;

            return ignoredFilesForCopying.Contains(strippedFileName);
        }

        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                _output($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                _output($"{DateTime.Now.ToLongTimeString()} Build failed");
            }
        }
    }
}
