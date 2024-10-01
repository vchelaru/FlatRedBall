using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.FormHelpers;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.RfsSorting
{
    [Export(typeof(PluginBase))]
    public class RfsSorter : EmbeddedPlugin
    {

        public override void StartUp()
        {
            this.ReactToTreeViewRightClickHandler += HandleTreeNodeRightClick;


        }

        void HandleTreeNodeRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            if (rightClickedTreeNode != null && rightClickedTreeNode.IsFilesContainerNode())
            {
                menuToModify.Add("Sort files alphabetically", SortFilesAlphabetically);
            }
        }

        void SortFilesAlphabetically(object sender, EventArgs args)
        {
            GlueElement element = GlueState.Self.CurrentElement;

            if (element != null)
            {
                element.ReferencedFiles.Sort(delegate(ReferencedFileSave first, ReferencedFileSave second)
                {
                    return first.Name.CompareTo(second.Name);

                });

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }
    }
}
