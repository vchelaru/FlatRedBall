using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.GlueView;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FlatRedBall.Glue.GlueView
{
    /// <summary>
    /// Interaction logic for GlueViewToolbar.xaml
    /// </summary>
    public partial class GlueViewToolbar : UserControl
    {
        Process glueViewProcess;

        public GlueViewRemotingSelectionInterfaceManager SelectionInterface
        {
            get;
            set;
        }


        public GlueViewToolbar()
        {
            InitializeComponent();
        }

        private void HandleCheckboxChecked(object sender, RoutedEventArgs e)
        {
            LaunchGlueView();
        }

        private void HandleCheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            ShutDownGlueView();
        }

        private void ShutDownGlueView()
        {
            if (glueViewProcess != null)
            {
                try
                {
                    glueViewProcess.Kill();
                }
                catch
                {
                    // do nothing
                }
                glueViewProcess = null;
            }
        }

        private void LaunchGlueView()
        {
            string glueViewLocation = System.Windows.Forms.Application.StartupPath + @"\Plugins\GlueView\GlueView.exe";

            if (FileManager.FileExists(FileManager.MakeRelative(glueViewLocation)))
            {
                glueViewProcess = Process.Start(glueViewLocation);
                glueViewProcess.EnableRaisingEvents = true;
                glueViewProcess.Exited += HandleExit;

                // give it a few MS to load:
                System.Threading.Thread.Sleep(100);


                const int maxTries = 3;
                int numberOfTries = 0;
                while (numberOfTries < maxTries)
                {
                    numberOfTries++;

                    try
                    {
                        SelectionInterface.AttemptConnection();
                    }
                    catch
                    {
                        int msToSleep = 160;
                        System.Threading.Thread.Sleep(msToSleep);
                    }
                }
                SelectionInterface.SetGlueProjectFile(ProjectManager.GlueProjectFileName, true);
            }
            else
            {
                MessageBox.Show("Could not find GlueView at the following location:\n\n" + glueViewLocation);
            }
        }

        private void HandleExit(object sender, EventArgs e)
        {
            glueViewProcess = null;

            global::Glue.MainGlueWindow.Self.Invoke( ()=> CheckBox.IsChecked = false);

        }
    }
}
