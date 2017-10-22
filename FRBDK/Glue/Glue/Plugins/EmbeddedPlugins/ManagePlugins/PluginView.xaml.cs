using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    /// <summary>
    /// Interaction logic for PluginView.xaml
    /// </summary>
    public partial class PluginView : UserControl
    {
        PluginContainer pluginContainer
        {
            get
            {
                return (DataContext as PluginViewModel)?.BackingData;
            }
        }

        static string UninstallPluginFile
        {
            get
            {
                return FileManager.UserApplicationData + "FRBDK/Plugins/Uninstall.txt";
            }
        }


        public PluginView()
        {
            InitializeComponent();
        }

        private void HandleExportPluginClicked(object sender, RoutedEventArgs e)
        {
            if(DataContext != null)
            {

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();



                dlg.FileName = pluginContainer.Name.Replace(" ", ""); // Default file name
                dlg.DefaultExt = ".plug"; // Default file extension
                dlg.Filter = "Plugin (.plug)|*.plug"; // Filter files by extension

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;

                    ExportPluginLogic exportPluginLogic = new ExportPluginLogic();

                    string pluginFolder = FileManager.GetDirectory(pluginContainer.AssemblyLocation);

                    string response = exportPluginLogic.CreatePluginFromDirectory(
                        sourceDirectory: pluginFolder, destinationFileName: filename,
                        includeAllFiles: true);

                    MessageBox.Show(response);

                    System.Diagnostics.Process.Start(FileManager.GetDirectory(filename));

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
                    EditorObjects.IoC.Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox("Success - Glue must be restarted to finish removing the plugin.");

                    using (StreamWriter w = File.AppendText(UninstallPluginFile))
                    {
                        w.WriteLine(directoryToDelete);
                    }
                }
            }
        }

    }
}
