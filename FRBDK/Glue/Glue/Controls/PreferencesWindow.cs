using System;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using System.Drawing;

namespace FlatRedBall.Glue.Controls
{
	public partial class PreferencesWindow : Form
	{
		public PreferencesWindow()
		{
			InitializeComponent();


            StartPosition = FormStartPosition.Manual;
            Location = new Point(PreferencesWindow.MousePosition.X, PreferencesWindow.MousePosition.Y);
		}

		private void PreferencesWindowShown(object sender, EventArgs e)
		{
			cbChildExternalApplications.Checked = EditorData.PreferenceSettings.ChildExternalApps;
            GenerateTombstoningCodeCheckBox.Checked = EditorData.PreferenceSettings.GenerateTombstoningCode;
            ShowHiddenNodesCheckBox.Checked = EditorData.PreferenceSettings.ShowHiddenNodes;
		}

        private void OkButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void CbChildExternalApplicationsCheckedChanged(object sender, EventArgs e)
        {
            EditorData.PreferenceSettings.ChildExternalApps = cbChildExternalApplications.Checked;
            EditorData.PreferenceSettings.SaveSettings();
        }

        private void GenerateTombstoningCodeCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            EditorData.PreferenceSettings.GenerateTombstoningCode = GenerateTombstoningCodeCheckBox.Checked;
            EditorData.PreferenceSettings.SaveSettings();
        }

        private static bool CheckIfNodeNeedsUpdate(INamedObjectContainer save)
        {
            foreach (var noSave in save.NamedObjects)
            {
                if (noSave.IsNodeHidden)
                {
                    return true;
                }

                if (noSave.ContainedObjects.Any(coSave => coSave.IsNodeHidden))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowHiddenNodesCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            var changed = (EditorData.PreferenceSettings.ShowHiddenNodes != ShowHiddenNodesCheckBox.Checked);

            EditorData.PreferenceSettings.ShowHiddenNodes = ShowHiddenNodesCheckBox.Checked;
            EditorData.PreferenceSettings.SaveSettings();

            if (!changed) return;

            foreach (var entity in
                ElementViewWindow.AllEntities.Where(entity => CheckIfNodeNeedsUpdate(entity.SaveObject)))
            {
                entity.UpdateReferencedTreeNodes();
            }

            foreach (var screen in
                ElementViewWindow.AllScreens.Where(screen => CheckIfNodeNeedsUpdate(screen.SaveObject)))
            {
                screen.UpdateReferencedTreeNodes();
            }
        }
	}
}
