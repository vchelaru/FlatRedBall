using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.FormHelpers;

public enum TreeNodeType
{
    EntityRootNode,
    ScreenRootNode,
    GlobalContentRootNode,
    EntityNode,
    ScreenNode,
    /// <summary>
    /// The "Files" node inside an entity or screen.
    /// </summary>
    ReferenedFileSaveContainerNode,
    NamedObjectContainerNode,
    CustomVariablesContainerNode,
    EventsContainerNode,
    StateContainerNode,
    CodeContainerNode,
    ReferencedFileSaveNode,
    CustomVariableNode,
    NamedObjectSaveNode,
    EventNode,
    StateCategoryNode,
    StateNode,
    CodeNode,
    /// <summary>
    /// This is a directory node, but the exact type is not specified and must be determined through the parent hierarchy.
    /// </summary>
    GeneralDirectoryNode,
    /// <summary>
    /// A specialty node that doesn't fit into any other category, such as the root Layer or CollisionRelationships nodes.
    /// </summary>
    Other
}

public interface ITreeNode
{
    object Tag { get; set; }

    ITreeNode Parent { get; }

    string Text { get; set; }

    IEnumerable<ITreeNode> Children { get; }

    #region "Is" methods

    public TreeNodeType TreeNodeType { get; }

    /// <summary>
    /// Returns whether this is a directory node that stores entities, files in global content, or files in an entity.
    /// </summary>
    public bool IsDirectoryNode()
    {
        if (Parent == null)
        {
            return false;
        }

        if (Tag != null)
        {
            return false;
        }

        if (Parent.IsRootEntityNode() || Parent.IsGlobalContentContainerNode() || Parent.IsRootScreenNode())
            return true;


        if (Parent.IsFilesContainerNode() || Parent.IsDirectoryNode())
        {
            return true;
        }

        else
            return false;
    }

    public bool IsRootEntityNode() => TreeNodeType == TreeNodeType.EntityRootNode;
    public bool IsRootScreenNode() => TreeNodeType == TreeNodeType.ScreenRootNode;


    public bool IsEntityNode() => TreeNodeType == TreeNodeType.EntityNode;

    public bool IsScreenNode() => TreeNodeType == TreeNodeType.ScreenNode;

    public bool IsGlobalContentContainerNode() => TreeNodeType == TreeNodeType.GlobalContentRootNode;

    /// <summary>
    /// Returns whether this is the Files folder inside a Glue element.
    /// </summary>
    public bool IsFilesContainerNode() => TreeNodeType == TreeNodeType.ReferenedFileSaveContainerNode;

    public bool IsFolderInFilesContainerNode()
    {
        var parentTreeNode = Parent;

        return Tag == null && parentTreeNode != null &&
               (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

    }

    public bool IsElementNode() => TreeNodeType == TreeNodeType.ScreenNode || TreeNodeType == TreeNodeType.EntityNode;
    public bool IsReferencedFile() => TreeNodeType == TreeNodeType.ReferencedFileSaveNode;

    public ITreeNode GetContainingElementTreeNode()
    {
        if (IsElementNode())
        {
            return this;
        }
        else if (Parent == null)
        {
            return null;
        }
        else
        {
            return Parent.GetContainingElementTreeNode();
        }
    }
    public bool IsRootLayerNode()
    {
        return Text.Equals("Layers", StringComparison.OrdinalIgnoreCase) &&
               Parent != null &&
               Tag == null &&
               Parent.IsRootNamedObjectNode();
    }

    public bool IsRootCollisionRelationshipsNode()
    {
        return Text.Equals("Collision Relationships", StringComparison.OrdinalIgnoreCase) &&
               Parent != null &&
               Tag == null &&
               Parent.IsRootNamedObjectNode();
    }

    public bool IsRootNamedObjectNode() => TreeNodeType == TreeNodeType.NamedObjectContainerNode;

    public bool IsRootCustomVariablesNode() => TreeNodeType == TreeNodeType.CustomVariablesContainerNode;

    public bool IsRootEventsNode() => TreeNodeType == TreeNodeType.EventsContainerNode;

    public bool IsNamedObjectNode() => TreeNodeType == TreeNodeType.NamedObjectSaveNode;

    public bool IsCustomVariable() => TreeNodeType == TreeNodeType.CustomVariableNode;

    public bool IsCodeNode() => TreeNodeType == TreeNodeType.CodeNode;

    public bool IsRootCodeNode() => TreeNodeType == TreeNodeType.CodeContainerNode;

    public bool IsRootStateNode() => TreeNodeType == TreeNodeType.StateContainerNode;

    public bool IsStateCategoryNode() => TreeNodeType == TreeNodeType.StateCategoryNode;

    public bool IsStateNode() => TreeNodeType == TreeNodeType.StateNode;

    public bool IsEventResponseTreeNode() => TreeNodeType == TreeNodeType.EventNode;

    /// <summary>
    /// Returns whether this is a folder inside GloalContent.
    /// </summary>
    /// <returns>Whether this is a folder inside GlobalContent.</returns>
    public bool IsFolderForGlobalContentFiles()
    {
        if (Parent == null || Tag != null)
        {
            return false;
        }

        var parent = Parent;

        while (parent != null)
        {
            if (parent.IsGlobalContentContainerNode())
            {
                return true;
            }
            else
            {
                parent = parent.Parent;
            }
        }

        return false;
    }

    public bool IsChildOfGlobalContent()
    {
        if (Parent == null)
        {
            return false;
        }

        if (Parent.IsGlobalContentContainerNode())
        {
            return true;
        }
        else
        {
            return Parent.IsChildOfGlobalContent();
        }
    }

    public bool IsChildOfRootEntityNode()
    {
        if (Parent == null)
        {
            return false;
        }
        else if (Parent.IsRootEntityNode())
        {
            return true;
        }
        else
        {
            return Parent.IsChildOfRootEntityNode();
        }
    }


    public bool IsChildOfRootScreenNode()
    {
        if (Parent == null)
        {
            return false;
        }
        else if (Parent.IsRootScreenNode())
        {
            return true;
        }
        else
        {
            return Parent.IsChildOfRootScreenNode();
        }
    }


    public bool IsFolderForEntities()
    {
        //TODO:  this fails when deleting a folder inside files.  We got to fix that.  Try deleting the Palette folders in CreepBase in Baron

        var parent = Parent;

        if (parent == null)
        {
            return false;
        }

        if (parent.IsFilesContainerNode())
        {
            return false;
        }

        return Tag == null &&
               IsChildOfRootEntityNode();
    }

    public bool IsFolderForScreens()
    {
        var parent = Parent;

        if (parent == null)
        {
            return false;
        }

        if (parent.IsFilesContainerNode())
        {
            return false;
        }

        return Tag == null &&
               IsChildOfRootScreenNode();
    }

    #endregion

    void Remove(ITreeNode child);
    void Add(ITreeNode child);

    /// <summary>
    /// The root tree node - the node which has no parent. Can be 'this', the parent of 'this',
    /// or many levels up from 'this'.
    /// </summary>
    public ITreeNode Root => Parent?.Root ?? this;

    /// <summary>
    /// Returns the file path of the selected node relative to the project root. Note that this is NOT the relative path considering
    /// the tree nodes.
    /// </summary>
    /// <returns></returns>
    public string GetRelativeFilePath()
    {
        var asTreeNode = this;
        #region Directory tree node
        if (this.IsDirectoryNode())
        {
            if (Parent.IsRootEntityNode())
            {
                return $"Entities/" + Text + "/";

            }
            if (Parent.IsRootScreenNode())
            {
                return $"Screens/" + Text + "/";
            }

            if (Parent.IsGlobalContentContainerNode())
            {

                string contentDirectory = GlueCommands.Self.GetAbsoluteFileName("GlobalContent/", true);

                string returnValue = contentDirectory + Text;
                if (this.IsDirectoryNode())
                {
                    returnValue += "/";
                }
                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);

                return returnValue;
            }

            // It's a tree node, so make it have a "/" at the end
            return Parent.GetRelativeFilePath() + Text + "/";
        }
        #endregion

        #region Global content container

        if (this.IsGlobalContentContainerNode())
        {
            var returnValue = ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";

            // But we want to make this relative to the project, so let's do that
            returnValue = ProjectManager.MakeRelativeContent(returnValue);



            return returnValue;
        }
        #endregion

        if (asTreeNode.IsFilesContainerNode())
        {
            // don't append "Files" here, because adding "Files" causes problems when searching for files
            string valueToReturn = Parent.GetRelativeFilePath();
            return Parent.GetRelativeFilePath();
        }

        if (asTreeNode.IsFolderInFilesContainerNode())
        {
            return Parent.GetRelativeFilePath() + Text + "/";
        }

        if (asTreeNode.IsReferencedFile())
        {
            string toReturn = Parent.GetRelativeFilePath() + Text;
            toReturn = toReturn.Replace("/", "\\");
            return toReturn;
        }

        if (asTreeNode.IsCodeNode())
        {
            var toReturn = Parent.GetRelativeFilePath();
            // take off "code"...
            toReturn = FileManager.GetDirectory(toReturn, RelativeType.Relative);
            // ... and the name of the element
            toReturn = FileManager.GetDirectory(toReturn, RelativeType.Relative);
            toReturn = toReturn + Text;
            return toReturn;
        }
        else
        {
            if (Parent == null)
            {
                string valueToReturn = this.Text + "/";
                return valueToReturn;
            }
            else
            {
                var extension = FileManager.GetExtension(this.Text);
                string valueToReturn = Parent.GetRelativeFilePath() + this.Text;
                if (string.IsNullOrEmpty(extension))
                {
                    valueToReturn += "/";
                }
                return valueToReturn;

            }
        }
    }

    public string GetRelativeTreeNodePath()
    {
        if (Parent != null)
        {
            return Parent.GetRelativeTreeNodePath() + "/" + this.Text;
        }
        else
        {
            return this.Text;
        }
    }

    ITreeNode FindByName(string name);

    void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode);

    ITreeNode FindByTagRecursive(object tag);

    void SortByTextConsideringDirectories();
}

