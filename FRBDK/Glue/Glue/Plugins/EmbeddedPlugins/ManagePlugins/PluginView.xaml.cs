using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    /// <summary>
    /// Interaction logic for PluginView.xaml
    /// </summary>
    public partial class PluginView : UserControl
    {
        PluginContainer pluginContainer => (DataContext as PluginViewModel)?.BackingData;

        static string UninstallPluginFile => FileManager.UserApplicationData + "FRBDK/Plugins/Uninstall.txt";

        public PluginView()
        {
            InitializeComponent();
        }

        private void HandleExportPluginClicked(object sender, RoutedEventArgs e)
        {
            if(DataContext != null)
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = pluginContainer.Name.Replace(" ", ""), // Default file name
                    DefaultExt = ".plug", // Default file extension
                    Filter = "Plugin (.plug)|*.plug" // Filter files by extension
                };

                // Show save file dialog box
                var result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;

                    ExportPluginLogic exportPluginLogic = new ExportPluginLogic();

                    string pluginFolder = FileManager.GetDirectory(pluginContainer.AssemblyLocation);

                    exportPluginLogic.CreatePluginFromDirectory(
                        sourceDirectory: pluginFolder, destinationFileName: filename,
                        includeAllFiles: true);

                    var startInfo = new ProcessStartInfo();
                    startInfo.FileName = FileManager.GetDirectory(filename);
                    startInfo.UseShellExecute = true;

                    System.Diagnostics.Process.Start(startInfo);

                }
            }
        }

        private void HandleUninstallPlugin(object sender, RoutedEventArgs e)
        {
            if(DataContext != null)
            {
                var directoryToDelete = FileManager.GetDirectory( pluginContainer.AssemblyLocation);

                // try deleteing it, probably won't be able to because the plugin is in-use
                try
                {
                    FileManager.DeleteDirectory(directoryToDelete);
                }
                catch(UnauthorizedAccessException ex)
                {
                    EditorObjects.IoC.Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox(L.Texts.PluginRestartToDelete);

                    using StreamWriter w = File.AppendText(UninstallPluginFile);
                    w.WriteLine(directoryToDelete);
                }
            }
        }

        private void HandleOpenPluginFolderClicked(object sender, RoutedEventArgs e)
        {
            if(pluginContainer != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = FileManager.GetDirectory(pluginContainer.AssemblyLocation)
                };

                Process.Start(psi);
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
            });
        }
    }
}
