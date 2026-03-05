using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Managers
{
    public interface IFindManager
    {
        ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave);

        ITreeNode TreeNodeByTag(object tag);

        ITreeNode GlobalContentTreeNode { get; }

        string GlobalContentFilesPath { get; }

        bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave);
    }
}
