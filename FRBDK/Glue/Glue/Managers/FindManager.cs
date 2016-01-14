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
    public class FindManager
    {

        public string GlobalContentFilesPath
        {
            get
            {
                string contentDirectory = GlueState.Self.CurrentGlueProjectDirectory + "GlobalContent/";

                if (ProjectManager.ContentProject != null)
                {
                    contentDirectory = ProjectManager.ContentProject.Directory + "GlobalContent/";
                }

                string returnValue = contentDirectory;

                return returnValue;
            }
        }

        public TreeNode TreeNodeForDirectory(string containingDirectory)
        {
            bool isEntity = true;

            // Let's see if this thing is really an Entity


            string relativeToProject = FileManager.Standardize(containingDirectory).ToLower();

            if (FileManager.IsRelativeTo(relativeToProject, FileManager.RelativeDirectory))
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject);
            }
            else if (ProjectManager.ContentProject != null)
            {
                relativeToProject = FileManager.MakeRelative(relativeToProject, ProjectManager.ContentProject.Directory);
            }

            if (relativeToProject.StartsWith("content/globalcontent") || relativeToProject.StartsWith("globalcontent")
                )
            {
                isEntity = false;
            }

            if (isEntity)
            {
                if (!FileManager.IsRelative(containingDirectory))
                {
                    containingDirectory = FileManager.MakeRelative(containingDirectory,
                        FileManager.RelativeDirectory + "Entities/");
                }

                return TreeNodeForDirectoryOrEntityNode(containingDirectory, ElementViewWindow.EntitiesTreeNode);
            }
            else
            {
                string subdirectory = FileManager.RelativeDirectory;

                if (ProjectManager.ContentProject != null)
                {
                    subdirectory = ProjectManager.ContentProject.Directory;
                }
                subdirectory += "GlobalContent/";


                containingDirectory = FileManager.MakeRelative(containingDirectory, subdirectory);

                if (containingDirectory == "")
                {
                    return ElementViewWindow.GlobalContentFileNode;
                }
                else
                {

                    return TreeNodeForDirectoryOrEntityNode(containingDirectory, ElementViewWindow.GlobalContentFileNode);
                }
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

        public EntityTreeNode EntityTreeNode(string entityFileName)
        {



            string directory = FileManager.MakeRelative(FileManager.GetDirectory(entityFileName));

            directory = directory.Substring("Entities/".Length);

            TreeNode containingTreeNode = null;

            if (!string.IsNullOrEmpty(directory))
            {
                containingTreeNode = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(
                 directory, ElementViewWindow.EntitiesTreeNode);
            }
            else
            {
                containingTreeNode = ElementViewWindow.EntitiesTreeNode;
            }

            for (int i = 0; i < containingTreeNode.Nodes.Count; i++)
            {
                if (containingTreeNode.Nodes[i] is EntityTreeNode)
                {
                    EntityTreeNode asEntityTreeNode = containingTreeNode.Nodes[i] as EntityTreeNode;

                    if (asEntityTreeNode.EntitySave.Name == entityFileName)
                    {
                        return asEntityTreeNode;
                    }
                }
            }

            return null;
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
                if (ElementViewWindow.ScreensTreeNode.Nodes[i] is ScreenTreeNode)
                {
                    ScreenTreeNode asScreenTreeNode = ElementViewWindow.ScreensTreeNode.Nodes[i] as ScreenTreeNode;

                    if (asScreenTreeNode.ScreenSave.Name == screenFileName)
                    {
                        return asScreenTreeNode;
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

                    if (asScreenTreeNode.ScreenSave == screenSave)
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

        public TreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave)
        {
            IElement container = namedObjectSave.GetContainer();

            if (container is ScreenSave)
            {
                ScreenTreeNode screenTreeNode = GlueState.Self.Find.ScreenTreeNode((ScreenSave)container);
                return screenTreeNode.GetTreeNodeFor(namedObjectSave);
            }
            else if (container is EntitySave)
            {
                EntityTreeNode entityTreeNode = GlueState.Self.Find.EntityTreeNode((EntitySave)container);
                return entityTreeNode.GetTreeNodeFor(namedObjectSave);
            }
            else
            {
                return null;
            }
        }

        public TreeNode ReferencedFileSaveTreeNode(ReferencedFileSave referencedFileSave)
        {
            IElement container = referencedFileSave.GetContainer();

            if (container == null)
            {
                return TreeNodeByTagIn(referencedFileSave, ElementViewWindow.GlobalContentFileNode.Nodes);
            }
            else if (container is ScreenSave)
            {
                ScreenTreeNode screenTreeNode = GlueState.Self.Find.ScreenTreeNode((ScreenSave)container);
                return screenTreeNode.GetTreeNodeFor(referencedFileSave);
            }
            else if (container is EntitySave)
            {
                EntityTreeNode entityTreeNode = GlueState.Self.Find.EntityTreeNode((EntitySave)container);
                return entityTreeNode.GetTreeNodeFor(referencedFileSave);
            }

            return null;

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

        public TreeNode CustomVariableTreeNode(CustomVariable variable)
        {
            TreeNode foundNode = null;

            foreach (ScreenTreeNode treeNode in ElementViewWindow.ScreensTreeNode.Nodes)
            {
                foundNode = treeNode.GetTreeNodeFor(variable);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }

            TreeNodeCollection nodeCollection = ElementViewWindow.EntitiesTreeNode.Nodes;


            // This could contain directories


            foundNode = FindCustomVariableInEntities(variable, nodeCollection);

            return foundNode;

        }

        public TreeNode EventResponseTreeNode(EventResponseSave eventResponse)
        {
            TreeNode foundNode = null;

            foreach (ScreenTreeNode treeNode in ElementViewWindow.ScreensTreeNode.Nodes)
            {
                foundNode = treeNode.GetTreeNodeFor(eventResponse);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }

            TreeNodeCollection nodeCollection = ElementViewWindow.EntitiesTreeNode.Nodes;
            foundNode = FindEventResponseSaveInEntities(eventResponse, nodeCollection);

            return foundNode;
        }

        public TreeNode StateTreeNode(StateSave stateSave)
        {
            TreeNode treeNode = TreeNodeByTagIn(stateSave, ElementViewWindow.ScreensTreeNode.Nodes);

            if (treeNode == null)
            {
                treeNode = TreeNodeByTagIn(stateSave, ElementViewWindow.EntitiesTreeNode.Nodes);
            }

            return treeNode;
        }

        private TreeNode FindCustomVariableInEntities(CustomVariable variable, TreeNodeCollection nodeCollection)
        {
            TreeNode foundNode = null;

            foreach (TreeNode treeNode in nodeCollection)
            {
                if (treeNode is EntityTreeNode)
                {
                    foundNode = ((EntityTreeNode)treeNode).GetTreeNodeFor(variable);
                    if (foundNode != null)
                    {
                        break;
                    }
                }
                else
                {
                    foundNode = FindCustomVariableInEntities(variable, treeNode.Nodes);
                    if (foundNode != null)
                    {
                        break;
                    }
                }
            }
            return foundNode;
        }

        private TreeNode FindEventResponseSaveInEntities(EventResponseSave eventResponse, TreeNodeCollection nodeCollection)
        {
            TreeNode foundNode = null;

            foreach (TreeNode treeNode in nodeCollection)
            {
                if (treeNode is EntityTreeNode)
                {
                    foundNode = ((EntityTreeNode)treeNode).GetTreeNodeFor(eventResponse);
                    if (foundNode != null)
                    {
                        break;
                    }
                }
                else
                {
                    foundNode = FindEventResponseSaveInEntities(eventResponse, treeNode.Nodes);
                    if (foundNode != null)
                    {
                        break;
                    }
                }
            }
            return foundNode;
        }

        public TreeNode StateCategoryTreeNode(StateSaveCategory category)
        {
            TreeNode treeNode = TreeNodeByTagIn(category, ElementViewWindow.ScreensTreeNode.Nodes);

            if (treeNode == null)
            {
                treeNode = TreeNodeByTagIn(category, ElementViewWindow.EntitiesTreeNode.Nodes);
            }

            return treeNode;

        }

        public string ContentPathFor(IElement element)
        {
            return ElementCommands.GetFullPathContentDirectory(element, null);

        }

    }


}
