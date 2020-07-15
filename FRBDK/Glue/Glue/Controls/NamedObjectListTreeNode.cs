using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using System.Drawing;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms.Design;
using FlatRedBall.Graphics;

namespace FlatRedBall.Glue.Controls
{
    public class NamedObjectListTreeNode : TreeNode
    {
        TreeNode LayersTreeNode;
        TreeNode CollisionRelationshipTreeNode;

        public NamedObjectListTreeNode(string text)
            : base(text)
        {
            
        }

        public TreeNode GetTreeNodeFor(NamedObjectSave namedObjectSave)
        {

            return GetTreeNodeFor(namedObjectSave, this);
        }

		public TreeNode GetTreeNodeFor(NamedObjectSave namedObjectSave, TreeNode treeNode)
		{
			for (int i = 0; i < treeNode.Nodes.Count; i++)
			{
                TreeNode node = treeNode.Nodes[i];

				if (node.Tag == namedObjectSave)
				{
					return node;
				}

                TreeNode returnValue = GetTreeNodeFor(namedObjectSave, node);

                if (returnValue != null)
                {
                    return returnValue;
                }
			}

			return null;
		}

        public void UpdateToNamedObjectSaves(IElement element)
        {
            UpdateToNamedObjectSaves(element.NamedObjects, this);

        }

        private void UpdateToNamedObjectSaves(List<NamedObjectSave> namedObjectList, TreeNode currentNode)
        {
            if(currentNode == this)
            {
                UpdateLayersTreeNode(namedObjectList);
                UpdateCollisionRelationshipTreeNode(namedObjectList);
            }

            var objectsToShow = namedObjectList
                .Where(item => (item.IsNodeHidden == false || EditorData.PreferenceSettings.ShowHiddenNodes == true) 
                    && item.IsLayer == false 
                    && item.IsCollisionRelationship() == false
                    )
                .ToList();
            int foldersAtTop = 0;
            if(currentNode == this)
            {
                if(LayersTreeNode != null)
                {
                    foldersAtTop++;
                }
                if(CollisionRelationshipTreeNode != null)
                {
                    foldersAtTop++;
                }
            }

            for (int i = 0; i < objectsToShow.Count; i++)
            {
                NamedObjectSave namedObject = objectsToShow[i];

                var desiredIndex = i + foldersAtTop;

                var treeNode = GetTreeNodeFor(namedObject, currentNode);

                if (treeNode == null)
                {
                    treeNode = CreateTreeNodeForNamedObjectAtIndex(currentNode, desiredIndex, namedObject);
                }

                if (treeNode != null)
                {
                    UpdateTreeNodeForNamedObjectAtIndex(currentNode, desiredIndex, namedObject, treeNode);
                }
            }

            for (int i = currentNode.Nodes.Count - 1; i > -1; i--)
            {
                var nodeAtI = currentNode.Nodes[i];
                NamedObjectSave treeNamedObject = nodeAtI.Tag as NamedObjectSave;

                if(nodeAtI != LayersTreeNode && nodeAtI != CollisionRelationshipTreeNode)
                {
                    if (!namedObjectList.Contains(treeNamedObject) || treeNamedObject.IsLayer || treeNamedObject.IsCollisionRelationship())
                    {
                        // Remove them since they're handled by the dedicated lists.
                        currentNode.Nodes.RemoveAt(i);
                    }
                    else
                    {
                        UpdateToNamedObjectSaves(treeNamedObject.ContainedObjects, nodeAtI);
                    }
                }
            }
        }

        private static void UpdateTreeNodeForNamedObjectAtIndex(TreeNode currentNode, int i, NamedObjectSave namedObject, TreeNode treeNode)
        {
            if (treeNode.Text != namedObject.InstanceName)
            {
                treeNode.Text = namedObject.InstanceName;
            }

            Color colorToSet = GetNosTreeNodeColor(namedObject);

            if (colorToSet != treeNode.ForeColor)
            {
                treeNode.ForeColor = colorToSet;
            }

            int indexOfTreeNode = currentNode.Nodes.IndexOf(treeNode);

            if (indexOfTreeNode != i)
            {
                currentNode.Nodes.Remove(treeNode);
                currentNode.Nodes.Insert(i, treeNode);
            }
        }

        private static TreeNode CreateTreeNodeForNamedObjectAtIndex(TreeNode currentNode, int i, NamedObjectSave namedObject)
        {
            TreeNode treeNode = new TreeNode(namedObject.InstanceName);
            treeNode.SelectedImageKey = "object.png";
            treeNode.ImageKey = "object.png";

            treeNode.Tag = namedObject;

            currentNode.Nodes.Insert(i, treeNode);
            return treeNode;
        }

        private void UpdateCollisionRelationshipTreeNode(List<NamedObjectSave> namedObjectList)
        {
            var collisionRelationships = namedObjectList
                .Where(item => item.IsCollisionRelationship())
                .ToArray();

            if(collisionRelationships.Length > 0)
            {
                if(CollisionRelationshipTreeNode == null)
                {
                    CollisionRelationshipTreeNode = new TreeNode();
                    CollisionRelationshipTreeNode.Text = "Collision Relationships";
                    CollisionRelationshipTreeNode.SelectedImageKey = "collisionRelationshipList.png";
                    CollisionRelationshipTreeNode.ImageKey = "collisionRelationshipList.png";
                    this.Nodes.Add(CollisionRelationshipTreeNode);
                }

                if(this.Nodes.IndexOf(CollisionRelationshipTreeNode) > 0)
                {
                    this.Nodes.Remove(CollisionRelationshipTreeNode);
                    this.Nodes.Insert(0, CollisionRelationshipTreeNode);
                }

                for (int i = 0; i < collisionRelationships.Length; i++)
                {
                    var collisionRelationship = collisionRelationships[i];

                    var treeNode = GetTreeNodeFor(collisionRelationship, CollisionRelationshipTreeNode);

                    if(treeNode == null)
                    {
                        treeNode = CreateTreeNodeForNamedObjectAtIndex(CollisionRelationshipTreeNode, i, collisionRelationship);
                    }

                    if(treeNode != null)
                    {
                        UpdateTreeNodeForNamedObjectAtIndex(CollisionRelationshipTreeNode, i, collisionRelationship, treeNode);
                    }
                }
            }
            else
            {
                if(CollisionRelationshipTreeNode != null)
                {
                    this.Nodes.Remove(CollisionRelationshipTreeNode);
                    CollisionRelationshipTreeNode = null;
                }
            }

        }

        private void UpdateLayersTreeNode(List<NamedObjectSave> namedObjectList)
        {
            var layers = namedObjectList
                            .Where(item => item.IsLayer)
                            .ToArray();

            if(layers.Length > 0)
            {
                if(LayersTreeNode == null)
                {
                    LayersTreeNode = new TreeNode();
                    LayersTreeNode.Text = "Layers";
                    LayersTreeNode.SelectedImageKey = "layerList.png";
                    LayersTreeNode.ImageKey = "layerList.png";
                    this.Nodes.Add(LayersTreeNode);
                }

                for(int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];

                    var treeNode = GetTreeNodeFor(layer, LayersTreeNode);

                    if (treeNode == null)
                    {
                        treeNode = CreateTreeNodeForNamedObjectAtIndex(LayersTreeNode, i, layer);
                    }

                    if (treeNode != null)
                    {
                        UpdateTreeNodeForNamedObjectAtIndex(LayersTreeNode, i, layer, treeNode);
                    }
                }
            }
            else
            {
                if (LayersTreeNode != null)
                {
                    this.Nodes.Remove(LayersTreeNode);
                    LayersTreeNode = null;
                }
            }
        }

        public static Color GetNosTreeNodeColor(NamedObjectSave namedObject)
        {
            Color colorToSet;

            if (namedObject.IsDisabled)
            {
                colorToSet = ElementViewWindow.DisabledColor;
            }
            else if (namedObject.IsContainer)
            {
                colorToSet = ElementViewWindow.IsContainerColor;
            }
            else if (namedObject.SetByDerived)
            {
                colorToSet = ElementViewWindow.SetByDerivedColor;
            }
            else if (namedObject.InstantiatedByBase)
            {
                colorToSet = ElementViewWindow.InstantiatedByBase;
            }
            else if (namedObject.DefinedByBase)
            {
                colorToSet = ElementViewWindow.DefinedByBaseColor;
            }
            else if (namedObject.FileCreatedBy != null)
            {
                colorToSet = ElementViewWindow.AutoGeneratedColor;
            }
            else if (namedObject.SourceType == SourceType.FlatRedBallType && namedObject.SourceClassType == "Layer")
            {
                colorToSet = ElementViewWindow.LayerObjectColor;
            }
            else
            {
                colorToSet = Color.White;
            }
            return colorToSet;
        }
    }
}
