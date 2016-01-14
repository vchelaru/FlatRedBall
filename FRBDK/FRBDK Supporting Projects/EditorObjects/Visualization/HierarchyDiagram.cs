#define UseDictionaries

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using FlatRedBall.Math;
using FlatRedBall;
using FlatRedBall.Gui;
using System.Security.Policy;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using System.Reflection;

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#elif FRB_XNA
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#else
using Color = System.Drawing.Color;
#endif


namespace EditorObjects.Visualization
{
    public class HierarchyDiagram
    {
        #region Fields

        IList mListOfAttachables;

        Layer mLayerUsing;

#if UseDictionaries
        Dictionary<IAttachable, HierarchyNode> mNodes = new Dictionary<IAttachable,HierarchyNode>();
#endif

        PositionedObjectList<HierarchyNode> mNodesAsList = new PositionedObjectList<HierarchyNode>();

        PositionedObjectList<HierarchyNode> mUnparentedNodes = new PositionedObjectList<HierarchyNode>();

        IAttachable mSelectedObject;

        HierarchyNode mNodeGrabbed;

        AxisAlignedRectangle mSelectionMarker;

        Color mTextColor;

        #endregion

        #region Properties

        public float CenterX
        {
            get
            {
                return ScaleX;
            }
        }

        public float CenterY
        {
            get
            {
                return 5 + 1 + .5f - ScaleY;
            }

        }

        public Color SelectionColor
        {
            get { return mSelectionMarker.Color; }
            set { mSelectionMarker.Color = value; }
        }

        public Color TextColor
        {
            get { return mTextColor; }
            set 
            { 
                mTextColor = value;
                foreach(KeyValuePair<IAttachable, HierarchyNode> kvp in mNodes)
                {
                    HierarchyNode hierarchyNode = kvp.Value;

                    hierarchyNode.TextRed = mTextColor.R;
                    hierarchyNode.TextGreen = mTextColor.G;
                    hierarchyNode.TextBlue = mTextColor.B;
                }
            
            }
        }

        public string CustomParentProperty
        {
            get;
            set;
        }

        public IAttachable SelectedObject
        {
            get { return mSelectedObject; }
            set
            {
                mSelectedObject = value;
                UpdateSelectionMarker();
            }
        }

        public PositionedObjectList<HierarchyNode> Nodes
        {
            get { return mNodesAsList; }
        }

        public Layer LayerUsing
        {
            get { return mLayerUsing; }
            set 
            { 
                mLayerUsing = value;
                ShapeManager.AddToLayer(mSelectionMarker, mLayerUsing);
                UpdateShapesOnLayers();
            }
        }

        public IList ListShowing
        {
            get { return mListOfAttachables; }
            set 
            { 
                mListOfAttachables = value;
                UpdateToList();
            }
        }

        public float ScaleX
        {
            get
            {
                if (mNodesAsList.Count == 0)
                {
                    return 0;
                }
                else
                {
                    float totalWidth = 0;
                    for (int i = 0; i < mUnparentedNodes.Count; i++)
                    {
                        totalWidth += mUnparentedNodes[i].Width;
                    }

                    return totalWidth / 2.0f;
                }
            }
        }

        public float ScaleY
        {
            get
            {
                if (mNodesAsList.Count == 0)
                {
                    return 0;
                }
                else
                {
                    int maxDepth = 0;

                    for (int i = 0; i < mNodesAsList.Count; i++)
                    {
                        maxDepth =
                            Math.Max(maxDepth, mNodesAsList[i].HierarchyDepth);
                    }

                    return (maxDepth * 4 + 2 + 1)/2.0f;
                }
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        public HierarchyDiagram()
        {
            mSelectionMarker = new AxisAlignedRectangle();
            mSelectionMarker.ScaleX = mSelectionMarker.ScaleY = 1.3f;
        }

        public void Activity(float cursorWorldX, float cursorWorldY)
        {
            MouseControlOverObjects(cursorWorldX, cursorWorldY);

            UpdateToList();

            //AutoPosition();
        }

        public void AutoPosition()
        {
            const float depthSpacing = 4;

            for (int i = 0; i < mNodesAsList.Count; i++)
            {
                HierarchyNode node = mNodesAsList[i];

                node.RelativeY = -depthSpacing;

                IAttachable parent = GetParent( node.ObjectRepresenting);

                if ( parent == null)
                {
                    node.AttachTo(null, false);
                    node.Y = 5;
                }
                else
                {
                    HierarchyNode parentNode = GetNodeFromAttachable(parent);
                    node.AttachTo(parentNode, false);                   
                }
            }

            mUnparentedNodes.Clear();

            // Gotta do this after all attachments have been made
            for (int i = 0; i < mNodesAsList.Count; i++)
            {
                HierarchyNode node = mNodesAsList[i];

                if (node.Parent != null)
                {
                    node.SetRelativeX();
                    node.ForceUpdateDependencies();
                }
                else
                {
                    float xToStartAt = 0;

                    if (mUnparentedNodes.Count != 0)
                    {
                        xToStartAt = mUnparentedNodes.Last.X +
                            mUnparentedNodes.Last.Width/2.0f;
                    }

                    node.Y = 5;
                    node.X = xToStartAt + node.Width/2.0f;

                    mUnparentedNodes.Add(node);
                }

            }

            UpdateSelectionMarker();

        }

        public HierarchyNode GetNodeFromAttachable(IAttachable attachable)
        {
            if (attachable == null)
            {
                return null;
            }
            if (mNodes.ContainsKey(attachable))
            {
                return mNodes[attachable];
            }
            else
            {
                return null;
            }
        }

        public HierarchyNode GetNodeOver(float worldX, float worldY)
        {
            for (int i = 0; i < mNodesAsList.Count; i++)
            {
                HierarchyNode node = mNodesAsList[i];

                if (node.IsMouseOver(worldX, worldY))
                {
                    return node;
                }
            }

            return null;
        }

        public void UpdateToList()
        {
            bool hasAnythingChanged = false;

            #region If there is no list watching, then return
            if (mListOfAttachables == null)
            {
                return;
            }
            #endregion

            #region Add nodes to the dictionary if there are any in the list that aren't in the dictionary

            for(int i = 0; i < mListOfAttachables.Count; i++)
            {
                IAttachable iAttachable = (IAttachable)mListOfAttachables[i];

                if (iAttachable == null)
                {
                    throw new Exception();
                }

                if (!IsNodeCreatedForAttachable(iAttachable))
                {
                    HierarchyNode hierarchyNode = new HierarchyNode(VisibleRepresentationType.Sprite);

                    hierarchyNode.TextRed = mTextColor.R;
                    hierarchyNode.TextGreen = mTextColor.G;
                    hierarchyNode.TextBlue = mTextColor.B;

                    if (LayerUsing != null)
                    {
                        hierarchyNode.AddToLayer(LayerUsing);
                    }

                    mNodes.Add(iAttachable, hierarchyNode);

                    hierarchyNode.ObjectRepresenting = iAttachable;
                    mNodesAsList.Add(hierarchyNode);

                    hasAnythingChanged = true;

                }
            }

            #endregion

            #region Remove nodes if necessary

            if (mListOfAttachables.Count < this.mNodesAsList.Count)
            {
                for (int i = 0; i < mNodesAsList.Count; i++)
                {
                    HierarchyNode node = mNodesAsList[i];

                    if (!this.mListOfAttachables.Contains(node.ObjectRepresenting))
                    {
                        // Remove this node
                        mNodes.Remove(node.ObjectRepresenting);
                        node.Destroy();

                        hasAnythingChanged = true;
                    }
                }
                 //There are nodes in the dictionary that have to be removed
            }

            #endregion

            #region Update the element visibility of each node

            foreach (HierarchyNode node in mNodesAsList)
            {
                HierarchyNode parentNode = null;

                IAttachable nodeParent = GetParent( node.ObjectRepresenting);

                if ( nodeParent != null)
                {
                    parentNode = GetNodeFromAttachable(nodeParent);
                }

                hasAnythingChanged |= node.UpdateElementVisibility(parentNode);
            }

            #endregion

            if (hasAnythingChanged)
            {
                AutoPosition();
            }
        }

        #endregion

        #region Private Methods



        private IAttachable GetParent(IAttachable objectToGetParentOf)
        {
            if (string.IsNullOrEmpty(CustomParentProperty))
            {
                return objectToGetParentOf.ParentAsIAttachable;
            }
            else
            {
                PropertyInfo pi = objectToGetParentOf.GetType().GetProperty(CustomParentProperty);

                return (IAttachable)pi.GetValue(objectToGetParentOf, null);
            }

        }

        private bool IsNodeCreatedForAttachable(IAttachable attachable)
        {
#if UseDictionaries
            {
                return mNodes.ContainsKey(attachable);
            }
#else
            {
                return GetNodeFromAttachable(attachable) != null;
            }
#endif
        }

        private void MouseControlOverObjects(float worldX, float worldY)
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                mNodeGrabbed = GetNodeOver(worldX, worldY);
            }

            if (cursor.PrimaryClick)
            {
                mNodeGrabbed = null;
            }

            if (mNodeGrabbed != null)
            {
                PositionedObjectMover.MouseMoveObject<HierarchyNode>(mNodeGrabbed);
            }
        }

        private void UpdateSelectionMarker()
        {
            mSelectionMarker.Visible = mSelectedObject != null;

            if (mSelectionMarker.Visible)
            {
                if (mNodes.ContainsKey(mSelectedObject))
                {
                    mSelectionMarker.Position = mNodes[mSelectedObject].Position;
                }
                else
                {
                    SelectedObject = null;
                }
            }

        }

        private void UpdateShapesOnLayers()
        {
            foreach (HierarchyNode node in mNodesAsList)
            {
                node.AddToLayer(mLayerUsing);
            }
        }

        #endregion

        #endregion

    }
}
