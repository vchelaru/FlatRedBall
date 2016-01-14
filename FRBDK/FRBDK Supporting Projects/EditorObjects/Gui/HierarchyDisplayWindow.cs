using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using EditorObjects.Visualization;
using System.Collections;
using FlatRedBall.Math;
using Microsoft.Xna.Framework.Graphics;

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#endif

namespace EditorObjects.Gui
{
    public class HierarchyDisplayWindow : CameraViewWindow
    {
        #region Fields

        HierarchyDiagram mHierarchyDiagram;

        #endregion

        #region Properties

        public string CustomParentProperty
        {
            get { return mHierarchyDiagram.CustomParentProperty; }
            set { mHierarchyDiagram.CustomParentProperty = value; }
        }
            

        public IAttachable SelectedObject
        {
            get
            {
                return mHierarchyDiagram.SelectedObject;
            }
            set
            {
                if (value != mHierarchyDiagram.SelectedObject)
                {
                    HighlightObjectNoCall(value);

                    if (Highlight != null)
                    {
                        Highlight(this);
                    }
                }
            }
        }

        public IList ListShowing
        {
            get { return mHierarchyDiagram.ListShowing; }
            set { mHierarchyDiagram.ListShowing = value;}
        }

        #endregion

        #region Events

        #region XML Docs
        /// <summary>
        /// Event raised whenever the SelectedObject is changed either by the user
        /// clicking or by the code setting the value.
        /// </summary>
        #endregion
        public event GuiMessage Highlight;

        public event GuiMessage StrongSelect;

        #endregion

        #region Event Methods

        private void ShowObjectDisplayWindowForSelectedObject(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(SelectedObject);
            }
        }

        #endregion

        #region Methods


        #region Constructor

        public HierarchyDisplayWindow(Cursor cursor, string contentManagerName)
            : base(cursor, contentManagerName)
        {
            HasMoveBar = true;

            Camera.DrawsWorld = false;

            Resizable = true;

            mHierarchyDiagram = new HierarchyDiagram();
            mHierarchyDiagram.TextColor = Color.DarkBlue;
            mHierarchyDiagram.SelectionColor = Color.Red;

            mHierarchyDiagram.LayerUsing = this.Camera.Layer;

            ScaleX = 6;
            ScaleY = 6;

            this.StrongSelect += ShowObjectDisplayWindowForSelectedObject;

            UpdateDisplayResolution();
        }

        #endregion


        #region Public Methods

        public void Activity()
        {
            UpdateToList();

            SelectionActivity();
        }


        public void HighlightObjectNoCall(IAttachable objectToHighlight)
        {
            bool didChange = mHierarchyDiagram.SelectedObject != objectToHighlight;

            if (didChange)
            {
                mHierarchyDiagram.SelectedObject = objectToHighlight;

                HierarchyNode hierarchyNode = mHierarchyDiagram.GetNodeFromAttachable(SelectedObject);

                if (hierarchyNode != null)
                {
                    this.Camera.X = hierarchyNode.X;
                    this.Camera.Y = hierarchyNode.Y;
                }
            }
        }


        public void UpdateToList()
        {
            mHierarchyDiagram.UpdateToList();

            foreach (HierarchyNode node in mHierarchyDiagram.Nodes)
            {
                node.Label = ObjectDisplayManager.GetStringRepresentationFor(node.ObjectRepresenting);
            }

            if (mHierarchyDiagram.ScaleX != 0)
            {

                Camera.SetBordersAtZ(
                    mHierarchyDiagram.CenterX - mHierarchyDiagram.ScaleX,
                    mHierarchyDiagram.CenterY - mHierarchyDiagram.ScaleY,
                    mHierarchyDiagram.CenterX + mHierarchyDiagram.ScaleX,
                    mHierarchyDiagram.CenterY + mHierarchyDiagram.ScaleY,
                    0);
            }

        }

        #endregion


        #region Private Methods


        private HierarchyNode GetNodeOver()
        {
            float worldX = WorldXAt(0);
            float worldY = WorldYAt(0);

            return mHierarchyDiagram.GetNodeOver(worldX, worldY);
        }


        private void SelectionActivity()
        {
            bool shouldTest = mCursor.PrimaryPush || mCursor.PrimaryDoubleClick ||
                mCursor.SecondaryPush;

            if ( mCursor.WindowOver == this && shouldTest && !this.IsCursorOnMoveBar(mCursor))
            {

                // See if over any hierarchy nodes
                HierarchyNode nodeOver = GetNodeOver();

                // don't use the properties here because the property moves the camera too.  We don't want that.
                if (nodeOver != null)
                {
                    mHierarchyDiagram.SelectedObject = nodeOver.ObjectRepresenting;
                }
                else
                {
                    mHierarchyDiagram.SelectedObject = null;
                }

                if (mCursor.PrimaryPush || mCursor.SecondaryPush)
                {
                    if (Highlight != null)
                    {
                        Highlight(this);
                    }
                }

                if (mCursor.PrimaryDoubleClick)
                {
                    if (this.StrongSelect != null)
                    {
                        StrongSelect(this);
                    }
                }
            }

        }

        #endregion


        #endregion
    }
}
