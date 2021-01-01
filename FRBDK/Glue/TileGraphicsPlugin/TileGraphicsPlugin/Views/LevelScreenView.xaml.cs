using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TiledPluginCore.ViewModels;

namespace TiledPluginCore.Views
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
            GlueCommands.Self.DialogCommands.ShowYesNoMessageBox("Are you sure you want to delete?",
                () =>
                {
                    System.IO.File.Delete(ViewModel.SelectedTmxFilePath.FullPath);
                    ViewModel.TmxFiles.Remove(ViewModel.SelectedTmxFile);
                });
        }

        private void DuplicateLevelClicked(object sender, RoutedEventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new TMX name";

            tiw.Result = StringFunctions.IncrementNumberAtEnd(FileManager.RemoveExtension(ViewModel.SelectedTmxFile)) 
                + ".tmx";

            var result = tiw.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath resultFilePath = GlueState.Self.ContentDirectory + tiw.Result;


                var doesFileAlreadyExist = ViewModel.TmxFilePaths.Contains(resultFilePath);

                if(doesFileAlreadyExist)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox($"The TMX name {tiw.Result} already exists");
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
