using OfficialPlugins.TreeViewPlugin.ViewModels;

namespace OfficialPlugins.TreeViewPlugin.Views;

internal interface ITreeViewDisplay
{
    void RefreshRightClickMenu();
    void ScrollNodeIntoView(NodeViewModel node);
    void UpdateTreeViewLayout();
    void FocusNode(NodeViewModel node);
}
