using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.TreeViewPlugin.Logic
{
    class FindManager : IFindManager
    {
        private MainTreeViewViewModel mainViewModel;

        public FindManager(MainTreeViewViewModel mainViewModel) => this.mainViewModel = mainViewModel;

        public ITreeNode GlobalContentTreeNode => mainViewModel.GlobalContentRootNode;

        public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave)
        {
            var container = referencedFileSave.GetContainer();

            bool isContained = false;
            if (container != null)
            {
                isContained = container.GetAllReferencedFileSavesRecursively().Contains(referencedFileSave);
            }
            else
            {
                isContained = GlueState.Self.CurrentGlueProject?.GlobalFiles.Contains(referencedFileSave) == true;

            }

            return isContained;
        }

        public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave) => TreeNodeByTag(namedObjectSave);

        public ITreeNode TreeNodeByTag(object tag) => mainViewModel.GetTreeNodeByTag(tag);
    }
}
