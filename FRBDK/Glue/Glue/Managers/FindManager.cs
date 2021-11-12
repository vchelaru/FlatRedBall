using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.Managers
{
    public interface IFindManager
    {

        TreeNode TreeNodeForDirectoryOrEntityNode(string containingDirection, TreeNode containingNode);

        TreeNode TreeNodeByDirectory(string containingDirection, TreeNode containingNode);

        BaseElementTreeNode ElementTreeNode(IElement element);

        EntityTreeNode EntityTreeNode(EntitySave entitySave);

        ScreenTreeNode ScreenTreeNode(string screenFileName);

        ScreenTreeNode ScreenTreeNode(ScreenSave screenSave);

        string GlobalContentFilesPath { get; }

        ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave);

        TreeNode TreeNodeByTag(object tag);

        string ContentPathFor(IElement element);

        bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave);
    }

    public class FindManager : IFindManager
    {

        public string GlobalContentFilesPath
        {
            get
            {
                return ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";
                
            }
        }


        public TreeNode TreeNodeForDirectoryOrEntityNode(string containingDirection, TreeNode containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return ElementViewWindow.EntitiesTreeNode;
            }
            else
            {
                return TreeNodeByDirectory(containingDirection, containingNode);
            }
        }

        public TreeNode TreeNodeByDirectory(string containingDirection, TreeNode containingNode)
        {
            if (string.IsNullOrEmpty(containingDirection))
            {
                return null;
            }
            else
            {
                int indexOfSlash = containingDirection.IndexOf("/");

                string rootDirectory = containingDirection;

                if (indexOfSlash != -1)
                {
                    rootDirectory = containingDirection.Substring(0, indexOfSlash);
                }

                for (int i = 0; i < containingNode.Nodes.Count; i++)
                {
                    TreeNode subNode = containingNode.Nodes[i];

                    if (subNode.IsDirectoryNode() && subNode.Text.ToLower() == rootDirectory.ToLower())
                    {
                        // use the containingDirectory here
                        if (indexOfSlash == -1 || indexOfSlash == containingDirection.Length - 1)
                        {
                            return subNode;
                        }
                        else
                        {
                            return TreeNodeByDirectory(containingDirection.Substring(indexOfSlash + 1), subNode);
                        }
                    }
                }

                return null;
            }
        }

        public BaseElementTreeNode ElementTreeNode(IElement element)
        {
            if (element is ScreenSave)
            {
                return ScreenTreeNode(element as ScreenSave);
            }
            else
            {
                return EntityTreeNode(element as EntitySave);
            }
        }

        public EntityTreeNode EntityTreeNode(EntitySave entitySave)
        {
            // Vic says: I don't know why I had code duplication here, but let's fix it:
            //return GetEntityTreeNode(entitySave.Name);
            // Vic says:  Update on Sept. 13 2010 - Turns out that the GetEntityTreeNode call above uses the name.  But this one
            // does an actual comparison between the EntitySave referenced by the tree node so it doesn't depend
            // on the Entity's name.  Very important when doing renaming!

            string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name));

            TreeNode treeNodeToAddTo = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(containingDirectory.Substring("Entities/".Length), ElementViewWindow.EntitiesTreeNode);


            for (int i = 0; i < treeNodeToAddTo.Nodes.Count; i++)
            {
                if (treeNodeToAddTo.Nodes[i] is EntityTreeNode)
                {
                    EntityTreeNode asEntityTreeNode = treeNodeToAddTo.Nodes[i] as EntityTreeNode;
                    if (asEntityTreeNode.EntitySave == entitySave)
                    {
                        return asEntityTreeNode;
                    }
                }
            }

            return null;
        }

        public ScreenTreeNode ScreenTreeNode(string screenFileName)
        {


            for (int i = 0; i < ElementViewWindow.ScreensTreeNode.Nodes.Count; i++)
            {
                if (ElementViewWindow.ScreensTreeNode.Nodes[i].Tag is ScreenSave screenSave)
                {
                    if (screenSave.Name == screenFileName)
                    {
                        return ElementViewWindow.ScreensTreeNode.Nodes[i] as ScreenTreeNode;
                    }
                }
            }

            return null;
        }

        public ScreenTreeNode ScreenTreeNode(ScreenSave screenSave)
        {
            for (int i = 0; i < ElementViewWindow.ScreensTreeNode.Nodes.Count; i++)
            {
                if (ElementViewWindow.ScreensTreeNode.Nodes[i] is ScreenTreeNode)
                {
                    ScreenTreeNode asScreenTreeNode = ElementViewWindow.ScreensTreeNode.Nodes[i] as ScreenTreeNode;

                    if (asScreenTreeNode.SaveObject == screenSave)
                    {
                        return asScreenTreeNode;
                    }
                }
            }
            return null;
        }

        public TreeNode GlobalContentFilesTreeNode()
        {
            return ElementViewWindow.GlobalContentFileNode;
        }

        public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave)
        {
            IElement container = namedObjectSave.GetContainer();

            if (container is ScreenSave)
            {
                var screenTreeNode = GlueState.Self.Find.ScreenTreeNode((ScreenSave)container);
                return TreeNodeWrapper.CreateOrNull( screenTreeNode.GetTreeNodeFor(namedObjectSave));
            }
            else if (container is EntitySave)
            {
                var entityTreeNode = GlueState.Self.Find.EntityTreeNode((EntitySave)container);
                return TreeNodeWrapper.CreateOrNull(entityTreeNode.GetTreeNodeFor(namedObjectSave));
            }
            else
            {
                return null;
            }
        }

        public TreeNode TreeNodeByTagIn(object tag, TreeNodeCollection treeNodeCollection)
        {
            foreach (TreeNode treeNode in treeNodeCollection)
            {
                if (treeNode.Tag == tag)
                {
                    return treeNode;
                }

                TreeNode foundNode = TreeNodeByTagIn(tag, treeNode.Nodes);

                if (foundNode != null)
                {
                    return foundNode;
                }
            }
            return null;
        }

        public TreeNode TreeNodeByTag(object tag)
        {
            var found = TreeNodeByTagIn(tag, ElementViewWindow.ScreensTreeNode.Nodes);

            if(found == null)
            {
                found = TreeNodeByTagIn(tag, ElementViewWindow.EntitiesTreeNode.Nodes);
            }
            if(found == null)
            {
                found = TreeNodeByTagIn(tag, ElementViewWindow.GlobalContentFileNode.Nodes);
            }
            return found;
        }

        public string ContentPathFor(IElement element)
        {
            return ElementCommands.GetFullPathContentDirectory(element, null);

        }

        public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave)
        {
            IElement container = referencedFileSave.GetContainer();

            bool isContained = false;
            if (container != null)
            {
                isContained = container.GetAllReferencedFileSavesRecursively().Contains(referencedFileSave);
            }
            else
            {
                isContained = ProjectManager.GlueProjectSave.GlobalFiles.Contains(referencedFileSave);

            }

            return isContained;

        }
    }


}
