﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.AnimationEditorForms.PropertyGrids;
using FlatRedBall.AnimationEditorForms.Preview;

namespace FlatRedBall.AnimationEditorForms
{
    public class PropertyGridManager
    {
        #region Fields

        AnimationFrameDisplayer mAnimationFrameDisplayer;
        AnimationChainDisplayer mAnimationChainDisplayer;
        AxisAlignedRectangleDisplayer rectangleDisplayer;
        CircleDisplayer circleDisplayer;

        static PropertyGridManager mSelf;

        System.Windows.Forms.PropertyGrid mPropertyGrid;
        TileMapInfoWindow mTileMapInfoWindow;

        #endregion

        #region Properties

        public UnitType UnitType
        {
            get { return mAnimationFrameDisplayer.CoordinateType; }
            set 
            { 
                mAnimationFrameDisplayer.CoordinateType = value;
                mPropertyGrid.Refresh();
                UpdateTileMapInformationListAndDisplay();
                WireframeManager.Self.RefreshAll();
            }
        }

        public static PropertyGridManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new PropertyGridManager();
                }
                return mSelf;
            }
        }

        #endregion

        #region Events

        public event EventHandler AnimationChainChange;
        public event EventHandler AnimationFrameChange;
        #endregion

        public PropertyGridManager()
        {
            mAnimationFrameDisplayer = new AnimationFrameDisplayer();
            mAnimationChainDisplayer = new AnimationChainDisplayer();
            rectangleDisplayer = new AxisAlignedRectangleDisplayer();
            circleDisplayer = new CircleDisplayer();
        }

        public void Initialize(System.Windows.Forms.PropertyGrid propertyGrid, TileMapInfoWindow tileMapInfoWindow)
        {
            mPropertyGrid = propertyGrid;
            mTileMapInfoWindow = tileMapInfoWindow;
            mPropertyGrid.PropertyValueChanged += HandleChangedProperty;

            tileMapInfoWindow.ValueChanged += HandleChangedPropertyEventHandler;
        }

        void HandleChangedProperty(object s, PropertyValueChangedEventArgs e)
        {
            WireframeManager.Self.RefreshAll();
            
            // The name could have changed, so let's refresh the node if the selected
            // item is an AnimationChain (not a frame):
            if (SelectedState.Self.SelectedChain != null && SelectedState.Self.SelectedFrame == null)
            {
                AppCommands.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
            }
            // actually even in this case the name of a shape could have changed
            else if(SelectedState.Self.SelectedAxisAlignedRectangle != null ||
                SelectedState.Self.SelectedCircle != null)
            {
                // Update the entire frame, which updates the shape:
                AppCommands.Self.RefreshTreeNode(SelectedState.Self.SelectedFrame);
            }

            AnimationChainChange?.Invoke(this, null);

            if (AnimationFrameChange != null)
            {
                AnimationFrameChange(this, null);

                if(e?.ChangedItem?.PropertyDescriptor?.Name == "TextureName")
                {
                    // The texture changed, so let the displayer know:
                    mAnimationFrameDisplayer.SetFrame(SelectedState.Self.SelectedFrame,
                        SelectedState.Self.SelectedTexture);
                }
            }

            if(SelectedState.Self.SelectedAxisAlignedRectangle != null)
            {
                PreviewManager.Self.RefreshAll();
                ApplicationEvents.Self.RaiseAfterAxisAlignedRectangleChanged(SelectedState.Self.SelectedAxisAlignedRectangle);
            }
            else if(SelectedState.Self.SelectedCircle != null)
            {
                PreviewManager.Self.RefreshAll();
                ApplicationEvents.Self.RaiseAfterCircleChanged(SelectedState.Self.SelectedCircle);
            }

        }

        void HandleChangedPropertyEventHandler(object s, EventArgs args)
        {
            HandleChangedProperty(null, null);
        }

        public void Refresh()
        {
            // check shapes (most specific) before frames or chains (more general)
            if(SelectedState.Self.SelectedAxisAlignedRectangle != null)
            {
                mAnimationChainDisplayer.PropertyGrid = null;
                mAnimationFrameDisplayer.PropertyGrid = null;
                circleDisplayer.PropertyGrid = null;

                rectangleDisplayer.PropertyGrid = mPropertyGrid;
                rectangleDisplayer.RefreshOnTimer = true;
                rectangleDisplayer.Instance = SelectedState.Self.SelectedAxisAlignedRectangle;

                mPropertyGrid.Refresh();
            }
            else if(SelectedState.Self.SelectedCircle != null)
            {
                mAnimationChainDisplayer.PropertyGrid = null;
                mAnimationFrameDisplayer.PropertyGrid = null;
                rectangleDisplayer.PropertyGrid = null;

                circleDisplayer.PropertyGrid = mPropertyGrid;
                circleDisplayer.RefreshOnTimer = true;
                circleDisplayer.Instance = SelectedState.Self.SelectedCircle;

                mPropertyGrid.Refresh();
            }
            else if (SelectedState.Self.SelectedFrame != null)
            {
                mAnimationChainDisplayer.PropertyGrid = null;
                rectangleDisplayer.PropertyGrid = null;
                circleDisplayer.PropertyGrid = null;

                mAnimationFrameDisplayer.PropertyGrid = mPropertyGrid;
                mAnimationFrameDisplayer.SetFrame(SelectedState.Self.SelectedFrame,
                    SelectedState.Self.SelectedTexture);
                mAnimationFrameDisplayer.RefreshOnTimer = true;

                mPropertyGrid.Refresh();
            }
            else if (SelectedState.Self.SelectedChain != null)
            {
                mAnimationFrameDisplayer.PropertyGrid = null;
                rectangleDisplayer.PropertyGrid = null;
                circleDisplayer.PropertyGrid = null;

                mAnimationChainDisplayer.PropertyGrid = mPropertyGrid;
                mAnimationChainDisplayer.Instance = SelectedState.Self.SelectedChain;
                mAnimationChainDisplayer.RefreshOnTimer = true;

                mPropertyGrid.Refresh();

            }
            UpdateTileMapInformationListAndDisplay();
        }

        public void UpdateTileMapInformationListAndDisplay()
        {
            string fileName = SelectedState.Self.SelectedTextureName;
            TileMapInformation tileMapInfo = ProjectManager.Self.TileMapInformationList.GetTileMapInformation(fileName);
            if (tileMapInfo == null && !string.IsNullOrEmpty(fileName))
            {
                tileMapInfo = new TileMapInformation();
                tileMapInfo.Name = fileName;
                ProjectManager.Self.TileMapInformationList.TileMapInfos.Add(tileMapInfo);
            }
            if (SelectedState.Self.SelectedFrame == null || AppState.Self.UnitType != AnimationEditorForms.UnitType.SpriteSheet)
            {
                this.mTileMapInfoWindow.Visible = false;
            }
            else
            {
                this.mTileMapInfoWindow.Visible = true;
                this.mTileMapInfoWindow.TileMapInformation = tileMapInfo;
            }
        }

        public void SetTileX(AnimationFrameSave frame, int value)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame", "Argument 'frame' cannot be null");
            }


            mAnimationFrameDisplayer.SetTileX(frame, value);
            mPropertyGrid.Refresh();
        }


        public void SetTileY(AnimationFrameSave frame, int value)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame", "Argument 'frame' cannot be null");
            }

            mAnimationFrameDisplayer.SetTileY(frame, value);
            mPropertyGrid.Refresh();
        }
    }
}
