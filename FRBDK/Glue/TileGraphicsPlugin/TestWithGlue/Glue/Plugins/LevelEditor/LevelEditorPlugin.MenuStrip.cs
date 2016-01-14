using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;

namespace PluginTestbed.LevelEditor
{
    [Export(typeof(IMenuStripPlugin))]
    public partial class LevelEditorPlugin : IMenuStripPlugin
    {
        MenuStrip _menuStrip;
        ToolStripMenuItem _menuItem;

        public void InitializeMenu(MenuStrip menuStrip)
        {
            _menuStrip = menuStrip;

            var menuItem = new ToolStripMenuItem("Level Editor");

            menuStrip.Items.Add(menuItem);
            _menuItem = menuItem;

            var launchItem = new ToolStripMenuItem("Launch Level Editor");

            menuItem.DropDownItems.Add(launchItem);
            launchItem.Click += LaunchItemClick;

            var connectItem = new ToolStripMenuItem("Connect to Level Editor");

            menuItem.DropDownItems.Add(connectItem);
            connectItem.Click += ConnectItemClick;
        }

        void ConnectItemClick(object sender, EventArgs e)
        {
            _selectionInterface.AttemptConnection();
            _interactiveInterface.AttemptConnection();
            _selectionInterface.SetGlueProjectFile(ProjectManager.GlueProjectFileName, true);
        }

        void LaunchItemClick(object sender, EventArgs e)
        {
            string levelEditorLocation = Application.StartupPath + @"\Plugins\LevelEditor\LevelEditor.exe";

            if (FileManager.FileExists(FileManager.MakeRelative(levelEditorLocation)))
            {
                GlueCommands.OpenCommands.OpenExternalApplication(levelEditorLocation, null);

                const int maxTries = 3;
                int numberOfTries = 0;
                while (numberOfTries < maxTries)
                {
                    numberOfTries++;

                    try
                    {
                        _selectionInterface.AttemptConnection();
                        _interactiveInterface.AttemptConnection();
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(120);
                    }
                }
                _selectionInterface.SetGlueProjectFile(ProjectManager.GlueProjectFileName, true);
            }
            else
            {
                MessageBox.Show(@"LevelEditor.exe was not found at " + levelEditorLocation);
            }
        }
    }
}
