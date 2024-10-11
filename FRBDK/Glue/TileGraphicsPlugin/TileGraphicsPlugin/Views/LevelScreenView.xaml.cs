using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiledPlugin.ViewModels;

namespace TiledPlugin.Views
{
    /// <summary>
    /// Interaction logic for LevelScreenView.xaml
    /// </summary>
    public partial class LevelScreenView : UserControl
    {
        public event EventHandler RenameScreen;

        LevelScreenViewModel ViewModel => DataContext as LevelScreenViewModel;

        public LevelScreenView()
        {
            InitializeComponent();
        }

        private void DeleteLevelClicked(object sender, RoutedEventArgs e)
        {
            var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(Localization.Texts.DeleteQuestion);
            if(result == MessageBoxResult.Yes)
            {
                System.IO.File.Delete(ViewModel.SelectedTmxFilePath.FullPath);
                ViewModel.TmxFiles.Remove(ViewModel.SelectedTmxFile);
            }
        }

        private void DuplicateLevelClicked(object sender, RoutedEventArgs e)
        {
            CustomizableTextInputWindow tiw = new()
            {
                Message = Localization.Texts.EnterNewTmx,
                Result = StringFunctions.IncrementNumberAtEnd(FileManager.RemoveExtension(ViewModel.SelectedTmxFile))
                + ".tmx"
            };

            if (tiw.ShowDialog() is true)
            {
                FilePath resultFilePath = GlueState.Self.ContentDirectory + tiw.Result;


                var doesFileAlreadyExist = ViewModel.TmxFilePaths.Contains(resultFilePath);

                if(doesFileAlreadyExist)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format(Localization.Texts.TmxAlreadyExists, tiw.Result));
                }
                else
                {
                    System.IO.File.Copy(ViewModel.SelectedTmxFilePath.FullPath,
                        resultFilePath.FullPath);

                    ViewModel.TmxFiles.Add(tiw.Result);
                }

            }
        }

        private void RenameLevelClicked(object sender, RoutedEventArgs e)
        {
            RenameScreen?.Invoke(this, null);
        }

    }
}
