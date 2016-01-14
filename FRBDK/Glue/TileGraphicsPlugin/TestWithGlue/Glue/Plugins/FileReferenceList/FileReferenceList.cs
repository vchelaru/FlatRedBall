using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using EditorObjects.Parsing;
using System.Windows.Forms;
using System.ComponentModel.Composition;

namespace PluginTestbed.FileReference
{
    [Export(typeof(ITreeViewRightClick))]
    public class FileReferenceList : ITreeViewRightClick
    {
        #region ITreeViewRightClick Members

        string mLastFileName;

        public void ReactToRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            if (rightClickedTreeNode.IsReferencedFile())
            {
                ReferencedFileSave rfs = rightClickedTreeNode.Tag as ReferencedFileSave;
                mLastFileName = ProjectManager.MakeAbsolute(rfs.Name);

                menuToModify.Items.Add("View Referenced Files").Click += new EventHandler(OnViewReferencedFilesClick);
            }
        }

        void OnViewReferencedFilesClick(object sender, EventArgs e)
        {
            List<string> allFiles = 
                ContentParser.GetFilesReferencedByAsset(mLastFileName, TopLevelOrRecursive.Recursive);

            string message = "Referenced files:\n";

            foreach (string file in allFiles)
            {
                message += file + "\n";
            }

            MessageBox.Show(message);
        }

        #endregion

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Reference Lister"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        #endregion
    }
}
