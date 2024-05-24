using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class NamedObjectsRootNodeViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        NodeViewModel LayersTreeNode;
        NodeViewModel CollisionRelationshipTreeNode;

        public bool IsGameScreen { get; private set; }

        public NamedObjectsRootNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(TreeNodeType.NamedObjectContainerNode, parent)
        {
            this.glueElement = glueElement;

            IsGameScreen = glueElement is ScreenSave screenSave && 
                (screenSave.Name.EndsWith ("\\GameScreen") || screenSave.Name.EndsWith("/GameScreen"));
        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            UpdateToNamedObjectSaves(glueElement.NamedObjects, this);

        }

        private void UpdateToNamedObjectSaves(List<NamedObjectSave> namedObjectList, NodeViewModel currentNode)
        {
            if (currentNode == this)
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
            if (currentNode == this)
            {
                if (LayersTreeNode != null)
                {
                    foldersAtTop++;
                }
                if (CollisionRelationshipTreeNode != null)
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

            for (int i = currentNode.Children.Count - 1; i > -1; i--)
            {
                var nodeAtI = currentNode.Children[i];
                NamedObjectSave treeNamedObject = nodeAtI.Tag as NamedObjectSave;

                if (nodeAtI != LayersTreeNode && nodeAtI != CollisionRelationshipTreeNode)
                {
                    if (!namedObjectList.Contains(treeNamedObject) || treeNamedObject.IsLayer || treeNamedObject.IsCollisionRelationship())
                    {
                        // Remove them since they're handled by the dedicated lists.
                        currentNode.Children.RemoveAt(i);
                    }
                    else
                    {
                        UpdateToNamedObjectSaves(treeNamedObject.ContainedObjects, nodeAtI);
                    }
                }
            }
        }

        private void UpdateLayersTreeNode(List<NamedObjectSave> namedObjectList)
        {
            var layers = namedObjectList
                            .Where(item => item.IsLayer)
                            .ToArray();

            var showLayers = layers.Length > 0 || IsGameScreen;

            if (showLayers)
            {
                if (LayersTreeNode == null)
                {
                    LayersTreeNode = new NodeViewModel(TreeNodeType.Other, this);
                    LayersTreeNode.ImageSource = LayersIcon;
                    LayersTreeNode.Text = "Layers";
                    //LayersTreeNode.SelectedImageKey = "layerList.png";
                    //LayersTreeNode.ImageKey = "layerList.png";
                    this.Children.Add(LayersTreeNode);
                }


                if (this.Children.IndexOf(LayersTreeNode) > 0)
                {
                    this.Children.Remove(LayersTreeNode);
                    this.Children.Insert(0, LayersTreeNode);
                }


                for (int i = 0; i < layers.Length; i++)
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
                for (int i = LayersTreeNode.Children.Count - 1; i > -1; i--)
                {
                    var node = LayersTreeNode.Children[i];
                    if (layers.Contains(node.Tag as NamedObjectSave) == false)
                    {
                        LayersTreeNode.Children.RemoveAt(i);
                    }
                }
            }
            else
            {
                if (LayersTreeNode != null)
                {
                    this.Children.Remove(LayersTreeNode);
                    LayersTreeNode = null;
                }
            }
        }

        private void UpdateCollisionRelationshipTreeNode(List<NamedObjectSave> namedObjectList)
        {
            var collisionRelationships = namedObjectList
                .Where(item => item.IsCollisionRelationship())
                .ToArray();

            var shouldShowCollisionRelationshipNode = collisionRelationships.Length > 0 || IsGameScreen;

            if (shouldShowCollisionRelationshipNode)
            {
                if (CollisionRelationshipTreeNode == null)
                {
                    CollisionRelationshipTreeNode = new NodeViewModel(FlatRedBall.Glue.FormHelpers.TreeNodeType.Other , this);
                    CollisionRelationshipTreeNode.ImageSource = CollisionsIcon;
                    CollisionRelationshipTreeNode.Text = "Collision Relationships";
                    //CollisionRelationshipTreeNode.SelectedImageKey = "collisionRelationshipList.png";
                    //CollisionRelationshipTreeNode.ImageKey = "collisionRelationshipList.png";
                    this.Children.Add(CollisionRelationshipTreeNode);
                }


                var desiredIndex = 0;
                if (LayersTreeNode != null)
                {
                    desiredIndex++;
                }

                if (this.Children.IndexOf(CollisionRelationshipTreeNode) > desiredIndex)
                {
                    this.Children.Remove(CollisionRelationshipTreeNode);
                    this.Children.Insert(desiredIndex, CollisionRelationshipTreeNode);
                }

                for (int i = 0; i < collisionRelationships.Length; i++)
                {
                    var collisionRelationship = collisionRelationships[i];

                    var treeNode = GetTreeNodeFor(collisionRelationship, CollisionRelationshipTreeNode);

                    if (treeNode == null)
                    {
                        treeNode = CreateTreeNodeForNamedObjectAtIndex(CollisionRelationshipTreeNode, i, collisionRelationship);
                    }

                    if (treeNode != null)
                    {
                        UpdateTreeNodeForNamedObjectAtIndex(CollisionRelationshipTreeNode, i, collisionRelationship, treeNode);
                    }
                }

                for (int i = CollisionRelationshipTreeNode.Children.Count - 1; i > -1; i--)
                {
                    var node = CollisionRelationshipTreeNode.Children[i];
                    if (collisionRelationships.Contains(node.Tag as NamedObjectSave) == false)
                    {
                        CollisionRelationshipTreeNode.Children.RemoveAt(i);
                    }
                }
            }
            else
            {
                if (CollisionRelationshipTreeNode != null)
                {
                    this.Children.Remove(CollisionRelationshipTreeNode);
                    CollisionRelationshipTreeNode = null;
                }
            }

        }

        public NodeViewModel GetTreeNodeFor(NamedObjectSave namedObjectSave)
        {

            return GetTreeNodeFor(namedObjectSave, this);
        }

        public NodeViewModel GetTreeNodeFor(NamedObjectSave namedObjectSave, NodeViewModel treeNode)
        {
            for (int i = 0; i < treeNode.Children.Count; i++)
            {
                var node = treeNode.Children[i];

                if (node.Tag == namedObjectSave)
                {
                    return node;
                }

                var returnValue = GetTreeNodeFor(namedObjectSave, node);

                if (returnValue != null)
                {
                    return returnValue;
                }
            }

            return null;
        }

        private static NodeViewModel CreateTreeNodeForNamedObjectAtIndex(NodeViewModel parentNode, int i, NamedObjectSave namedObject)
        {
            var treeNode = new NodeViewModel(FlatRedBall.Glue.FormHelpers.TreeNodeType.ReferencedFileSaveNode, parentNode);
            treeNode.Tag = namedObject;
            treeNode.IsEditable = true;

            treeNode.Text = namedObject.InstanceName;

            BitmapImage imageSource = GetIcon(namedObject);

            treeNode.ImageSource = imageSource;
            //treeNode.SelectedImageKey = "object.png";
            //treeNode.ImageKey = "object.png";

            treeNode.Tag = namedObject;

            parentNode.Children.Insert(i, treeNode);
            return treeNode;
        }

        public static BitmapImage GetIcon(NamedObjectSave namedObject)
        {
            var imageSource = EntityInstanceIcon;

            if(namedObject.IsContainer)
            {
                imageSource = EntityInstanceIsContainerIcon;
            }
            else if (namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Layer)
            {
                imageSource = LayerIcon;
            }
            else if (namedObject.IsCollisionRelationship())
            {
                imageSource = CollisionIcon;
            }
            else if (namedObject.IsList)
            {
                if(namedObject.DefinedByBase)
                {
                    imageSource = NodeViewModel.EntityInstanceListDerivedIcon;
                }
                else
                {
                    imageSource = NodeViewModel.EntityInstanceListIcon;
                }
            }
            else if(namedObject.SourceClassType == "TileShapeCollection" || 
                namedObject.SourceClassType == "FlatRedBall.TileCollisions.TileShapeCollection")
            {
                imageSource = TileShapeCollectionIcon;
            }
            else if(namedObject.DefinedByBase)
            {
                imageSource = EntityInstanceDerivedIcon;
            }

            return imageSource;
        }

        private static void UpdateTreeNodeForNamedObjectAtIndex(NodeViewModel currentNode, int i, NamedObjectSave namedObject, NodeViewModel treeNode)
        {
            if (treeNode.Text != namedObject.InstanceName)
            {
                treeNode.Text = namedObject.InstanceName;
            }

            //Color colorToSet = GetNosTreeNodeColor(namedObject);

            //if (colorToSet != treeNode.ForeColor)
            //{
            //    treeNode.ForeColor = colorToSet;
            //}

            int indexOfTreeNode = currentNode.Children.IndexOf(treeNode);

            if (indexOfTreeNode != i)
            {
                treeNode.Parent.Remove(treeNode);
                currentNode.Children.Insert(i, treeNode);
                treeNode.Parent = currentNode;
            }

            treeNode.ImageSource = GetIcon(namedObject);
        }

    }
}
