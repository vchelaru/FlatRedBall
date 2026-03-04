namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    partial class MainTreeViewViewModel
    {
        #region Collapse

        internal void CollapseAll()
        {
            foreach (var node in VisibleRoot)
            {
                node.CollapseRecursively();
            }

        }

        internal void CollapseToDefinitions()
        {
            foreach (var node in VisibleRoot)
            {
                node.CollapseToDefinitions();
            }

            // make sure the top level tree nodes are expanded
            ScreenRootNode.IsExpanded = true;
            EntityRootNode.IsExpanded = true;
            GlobalContentRootNode.IsExpanded = true;
        }

        #endregion
    }
}
