using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.TreeViewPlugin.ViewModels;

class CodeRootViewModel : NodeViewModel
{
    private readonly GlueElement _glueElement;

    public CodeRootViewModel(NodeViewModel parent, GlueElement glueElement) : base(parent)
    {
        this._glueElement = glueElement;
    }

    public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
    {
        Children.Clear();

        var files = CodeWriter.GetAllCodeFilesFor(_glueElement);

        foreach (var file in files)
        {
            // See if there is already a tree node for this
            NodeViewModel foundTreeNode = null;

            string text = file.NoPath;


            foreach (var treeNode in Children)
            {
                if (treeNode.Text == text)
                {
                    foundTreeNode = treeNode;
                    break;
                }
            }

            if (foundTreeNode == null)
            {
                var treeNode = new NodeViewModel(this);
                treeNode.ImageSource = CodeIcon;
                treeNode.Text = text;
                Children.Add(treeNode);
            }
        }
    }
}