using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
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
    }
}
