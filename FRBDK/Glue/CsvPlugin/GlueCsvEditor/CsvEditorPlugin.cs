using System;
using System.ComponentModel.Composition;
using System.Reflection;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using GlueCsvEditor.Controls;
using System.Windows.Forms;
using EditorObjects.IoC;
using GlueCsvEditor.Styling;

namespace GlueCsvEditor
{
    [Export(typeof(PluginBase))]
    public class CsvEditorPlugin : PluginBase
    {
        #region Fields

        private EditorMain _editor;

        private string _currentCsv;

        PluginTab pluginTab;

        #endregion

        [Import("GlueProjectSave")]
        public GlueProjectSave GlueProjectSave
        {
            get;
            set;
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }
		
		[Import("GlueState")]
		public IGlueState GlueState
		{
		    get;
		    set;
        }

        public override Version Version
        {
            get 
            { 
                return Assembly.GetAssembly(typeof (CsvEditorPlugin))
                               .GetName()
                               .Version; 
            }
        }

        public override string FriendlyName { get { return "Csv Editor"; } }

        public override bool ShutDown(PluginShutDownReason reason)
        {
            //base.RemoveTab();
            pluginTab?.Hide();

            return true;
        }

        public override void StartUp()
        {
            // Initialize the handlers I need
            ReactToItemSelectHandler = ReactToItemSelect;
            ReactToFileChangeHandler = ReactToFileChange;

            Container.Set<ColoringLogic>(new ColoringLogic());
        }

        private void ReactToItemSelect(TreeNode selectedTreeNode)
        {
            if (_editor != null && _editor.Parent != null)
            {
                _editor.SaveEditorSettings();
            }

            // Determine if a csv was selected
            if (selectedTreeNode != null && IsCsv(selectedTreeNode.Tag))
            {
                if(_editor == null )
                {
                    CreateNewCsvControl(selectedTreeNode);

                    pluginTab = base.CreateAndAddTab(_editor, "CSV", TabLocation.Center);

                }
                else
                {
                    pluginTab.Show();
                }

                LoadFile(selectedTreeNode);
            }
            else
            {
                pluginTab?.Hide();

                _editor = null;
            }
        }

        private void CreateNewCsvControl(TreeNode selectedTreeNode)
        {
            bool succeeded = false;

            try
            {
                _editor = new EditorMain();
                succeeded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create a CSV runtime representation: " + ex.Message, "Error");
            }

        }

        private void LoadFile(TreeNode selectedTreeNode)
        {
            var csvRfs = selectedTreeNode.Tag as ReferencedFileSave;
            if (csvRfs == null)
                throw new InvalidOperationException("Node is CSV but did not cast as a ReferencedFileSave");



            char delimiter;
            _currentCsv = ProjectManager.MakeAbsolute(csvRfs.Name, true);


            switch (csvRfs.CsvDelimiter)
            {
                case AvailableDelimiters.Pipe:
                    delimiter = '|';
                    break;

                case AvailableDelimiters.Tab:
                    delimiter = '\t';
                    break;

                default:
                    delimiter = ',';
                    break;
            }


            _editor.LoadCsv(_currentCsv, delimiter);

        }

        private void ReactToFileChange(string filename)
        {
            if (filename.Equals(_currentCsv, StringComparison.OrdinalIgnoreCase))
            {
                PluginManager.ReceiveOutput("CSV Editor: Loading file because of external change " + filename);
                if (_editor != null)
                {
                    _editor.SaveEditorSettings();
                    _editor.NotifyOfCsvUpdate();
                }
            }
        }

        private bool IsCsv(object obj)
        {
            var csv = obj as ReferencedFileSave;
            if (csv == null)
                return false;

            return csv.IsCsvOrTreatedAsCsv;
        }
    }
}
