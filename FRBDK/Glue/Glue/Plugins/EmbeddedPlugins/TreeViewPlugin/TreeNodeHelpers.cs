using System;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using L = Localization;

namespace FlatRedBall.Glue.FormHelpers;

public static class TreeNodeHelpers
{
    #region IsXXXXXXXXNode
        

    public static bool IsNamedObjectNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag != null &&
               treeNodeInQuestion.Tag is NamedObjectSave;
    }

    public static bool IsReferencedFile(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion != null && 
               treeNodeInQuestion.Tag != null &&
               treeNodeInQuestion.Tag is ReferencedFileSave;
    }

    public static bool IsCustomVariable(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag != null &&
               treeNodeInQuestion.Tag is CustomVariable;
    }

    public static bool IsEventResponseTreeNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag != null &&
               treeNodeInQuestion.Tag is EventResponseSave;

    }

    public static bool IsCodeNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag == null && treeNodeInQuestion.Text.EndsWith(".cs");
    }

    public static bool IsRootCodeNode(this TreeNode treeNodeInQuestion)
    {

        return treeNodeInQuestion.Tag == null && treeNodeInQuestion.Text == "Code" &&
               treeNodeInQuestion.Parent != null && treeNodeInQuestion.Parent.IsElementNode();
    }

    public static bool IsScreenNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag is ScreenSave;

    }

    public static bool IsEntityNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Tag is EntitySave;
    }

    public static bool IsElementNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.IsScreenNode() || treeNodeInQuestion.IsEntityNode();
    }

    public static bool IsRootNamedObjectNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Text == "Objects" &&
               treeNodeInQuestion.Parent != null &&
               (treeNodeInQuestion.Parent.IsEntityNode() || treeNodeInQuestion.Parent.IsScreenNode());
    }

    public static bool IsRootLayerNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Text == "Layers" &&
               treeNodeInQuestion.Parent != null &&
               treeNodeInQuestion.Tag == null &&
               treeNodeInQuestion.Parent.IsRootNamedObjectNode();
    }

    public static bool IsRootScreenNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Text == "Screens" && treeNodeInQuestion.Parent == null;
    }

    public static bool IsRootEntityNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Text == "Entities" && treeNodeInQuestion.Parent == null;
    }

    public static bool IsRootCustomVariablesNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Parent != null &&
               treeNodeInQuestion.Text == "Variables";

    }

    public static bool IsRootEventsNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Parent != null &&
               (treeNodeInQuestion.Parent.IsEntityNode() || treeNodeInQuestion.Parent.IsScreenNode()) &&
               treeNodeInQuestion.Text == "Events";
    }

    /// <summary>
    /// Returns whether the node is a "Files" tree node in a Screen or Entity
    /// </summary>
    /// <param name="treeNodeInQuestion">The tree node - which is the calling node in an extension method.</param>
    /// <returns>Whether the tree node is the "Files" node</returns>
    public static bool IsFilesContainerNode(this TreeNode treeNodeInQuestion)
    {
        TreeNode parentTreeNode = treeNodeInQuestion.Parent;
        return treeNodeInQuestion.Text == "Files" && parentTreeNode != null &&
               (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
    }

    public static bool IsFolderInFilesContainerNode(this TreeNode treeNodeInQuestion)
    {
        TreeNode parentTreeNode = treeNodeInQuestion.Parent;

        return treeNodeInQuestion.Tag == null && parentTreeNode != null &&
               (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

    }

    public static bool IsFolderForEntities(this TreeNode treeNodeInQuestion)
    {
        //TODO:  this fails when deleting a folder inside files.  We gotta fix that.  Try deleting the Palette folders in CreepBase in Baron
        if (treeNodeInQuestion == null)
        {
            return false;
        }

        TreeNode parent = treeNodeInQuestion.Parent;

        if (parent == null)
        {
            return false;
        }

        if (parent.IsFilesContainerNode())
        {
            return false;
        }

        return treeNodeInQuestion.Tag == null &&
               treeNodeInQuestion.IsChildOfRootEntityNode();


        //return treeNodeInQuestion.Root().IsRootEntityNode() && treeNodeInQuestion != treeNodeInQuestion.Root();
    }

    public static bool IsRootObjectNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Text == "Objects" && treeNodeInQuestion.Tag == null;
    }

    public static bool IsGlobalContentContainerNode(this TreeNode treeNodeInQuestion)
    {
        return false;// treeNodeInQuestion == ElementViewWindow.GlobalContentFileNode;
    }

    public static bool IsFolderForGlobalContentFiles(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        if (treeNodeInQuestion.Parent == null)
        {
            return false;
        }

        TreeNode parent = treeNodeInQuestion.Parent;

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

    public static bool IsChildOfGlobalContent(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        if (treeNodeInQuestion.Parent == null)
        {
            return false;
        }

        if (treeNodeInQuestion.Parent.IsGlobalContentContainerNode())
        {
            return true;
        }
        else
        {
            return treeNodeInQuestion.Parent.IsChildOfGlobalContent();
        }
    }

    public static bool IsChildOfRootEntityNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        if (treeNodeInQuestion.Parent == null)
        {
            return false;
        }
        else if (treeNodeInQuestion.Parent.IsRootEntityNode())
        {
            return true;
        }
        else
        {
            TreeNode parentTreeNode = treeNodeInQuestion.Parent;
            return parentTreeNode.IsChildOfRootEntityNode();
        }
    }



    public static bool IsUnreferencedFileContainerNode(this TreeNode treeNodeInQuestion)
    {
        return treeNodeInQuestion.Parent == null && treeNodeInQuestion.Text == "Unreferenced Files";
    }
        
    /// <summary>
    /// Returns whether the tree node is folder containing entities, files in global content, or folder in files.
    /// </summary>
    /// <param name="treeNodeInQuestion">Th tree node to check</param>
    /// <returns>Whether the tree node is a folder node</returns>
    public static bool IsDirectoryNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        if (treeNodeInQuestion.Parent == null)
        {
            return false;
        }

        if (treeNodeInQuestion.Tag != null)
        {
            return false;
        }

        if (treeNodeInQuestion.IsReferencedFile())
            return false;

        if (treeNodeInQuestion.Parent.IsRootEntityNode() || treeNodeInQuestion.Parent.IsGlobalContentContainerNode())
            return true;


        if (!treeNodeInQuestion.IsEntityNode() && !treeNodeInQuestion.IsScreenNode() && (treeNodeInQuestion.Parent.IsFilesContainerNode() || treeNodeInQuestion.Parent.IsDirectoryNode()))
        {
            return true;
        }

        else
            return false;
    }

    public static bool IsStateListNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        TreeNode parentTreeNode = treeNodeInQuestion.Parent;
        return treeNodeInQuestion.Text == "States" && parentTreeNode != null &&
               (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
    }

    public static bool IsStateNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Tag is StateSave;
        // May 13, 2011: We can't just base it off of what the parent is because
        // now we have state categories.  Using the tag just seems cleaner anyway.
        //TreeNode parentTreeNode = treeNodeInQuestion.Parent;
        //return parentTreeNode != null && parentTreeNode.IsStateListNode();            
    }

    public static bool IsStateCategoryNode(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if(treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        return treeNodeInQuestion.Tag is StateSaveCategory;
    }

    #endregion

    public static TreeNode Root(this TreeNode treeNodeInQuestion)
    {
        if (treeNodeInQuestion == null)
        {
            return treeNodeInQuestion;
        }
        else if(treeNodeInQuestion.Parent == null)
        {
            return treeNodeInQuestion;
        }
        else
        {
            return treeNodeInQuestion.Parent.Root();
        }
    }

    public static string GetRelativePath(this TreeNode treeNodeInQuestion)
    {
#if DEBUG
        if (treeNodeInQuestion == null)
        {
            throw new ArgumentNullException(nameof(treeNodeInQuestion));
        }
#endif
        #region Directory tree node
        if (treeNodeInQuestion.IsDirectoryNode())
        {
            if (treeNodeInQuestion.Parent.IsRootEntityNode())
            {
                return $"{L.Texts.Entities}/{treeNodeInQuestion.Text}/";

            }
            if (treeNodeInQuestion.Parent.IsRootScreenNode())
            {
                return $"{L.Texts.Screens}/{treeNodeInQuestion.Text}/";
            }
            
            if (treeNodeInQuestion.Parent.IsGlobalContentContainerNode())
            {

                string contentDirectory = GlueCommands.Self.GetAbsoluteFileName("GlobalContent", true);

                string returnValue = contentDirectory + treeNodeInQuestion.Text;
                if (treeNodeInQuestion.IsDirectoryNode())
                {
                    returnValue += "/";
                }
                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);

                return returnValue;
            }
            else
            {
                // It's a tree node, so make it have a "/" at the end
                return treeNodeInQuestion.Parent.GetRelativePath() + treeNodeInQuestion.Text + "/";
            }
        }
        #endregion

        #region Global content container

        else if (treeNodeInQuestion.IsGlobalContentContainerNode())
        {
            var returnValue = ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";


            // But we want to make this relative to the project, so let's do that
            returnValue = ProjectManager.MakeRelativeContent(returnValue);



            return returnValue;
        }
        #endregion

        else if (treeNodeInQuestion.IsFilesContainerNode())
        {
            string valueToReturn = treeNodeInQuestion.Parent.GetRelativePath();


            return valueToReturn;
        }
        else if (treeNodeInQuestion.IsFolderInFilesContainerNode())
        {
            return treeNodeInQuestion.Parent.GetRelativePath() + treeNodeInQuestion.Text + "/";
        }
        else if (treeNodeInQuestion.IsElementNode())
        {
            return ((IElement)treeNodeInQuestion.Tag).Name + "/";
        }
        else if (treeNodeInQuestion.IsReferencedFile())
        {
            string toReturn = treeNodeInQuestion.Parent.GetRelativePath() + treeNodeInQuestion.Text;
            toReturn = toReturn.Replace("/", "\\");
            return toReturn;
        }
        else
        {
            // Improve this to handle embeded stuff
            string textToReturn = treeNodeInQuestion.Text;

            if (string.IsNullOrEmpty(FlatRedBall.IO.FileManager.GetExtension(textToReturn)))
            {
                textToReturn += "/";
            }

            return textToReturn;
        }
    }

    public static TreeNode GetContainingElementTreeNode(this TreeNode containedTreeNode)
    {
        if (containedTreeNode.IsElementNode())
        {
            return containedTreeNode;
        }
        else if (containedTreeNode.Parent == null)
        {
            return null;
        }
        else
        {
            return containedTreeNode.Parent.GetContainingElementTreeNode();
        }
    }

    public static TreeNode NextNodeCrawlingTree(this TreeNode node)
    {
        // return child?
        if (node.Nodes.Count != 0)
        {
            return node.Nodes[0];
        }

        // return sibling?
        TreeNode nextSibling = node.NextNode;

        if (nextSibling != null)
        {
            return nextSibling;
        }

        TreeNode parentNode = node.Parent;

        while (parentNode != null)
        {
            if (parentNode.NextNode == null)
            {
                parentNode = parentNode.Parent;
            }
            else
            {
                return parentNode.NextNode;
            }

        }

        return null;

    }

    public static TreeNode FirstOrDefault(this TreeNodeCollection collection, Func<TreeNode, bool> func)
    {
        foreach (TreeNode node in collection)
        {
            if (func(node))
            {
                return node;
            }
        }

        return null;
    }

}