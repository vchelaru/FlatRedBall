using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.CommandsAndState;

namespace FlatRedBall.AnimationEditorForms
{
    public class PropertyGridManager
    {
        #region Fields

        AnimationFrameDisplayer mAnimationFrameDisplayer;
        AnimationChainDisplayer mAnimationChainDisplayer;

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

        public event EventHandler AnimationChainChange;
        public event EventHandler AnimationFrameChange;

        public PropertyGridManager()
        {
            mAnimationFrameDisplayer = new AnimationFrameDisplayer();
            mAnimationChainDisplayer = new AnimationChainDisplayer();
        }

        public void Initialize(System.Windows.Forms.PropertyGrid propertyGrid, TileMapInfoWindow tileMapInfoWindow)
        {
            mPropertyGrid = propertyGrid;
            mTileMapInfoWindow = tileMapInfoWindow;
            mPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(HandleChangedProperty);

            tileMapInfoWindow.ValueChanged += HandleChangedPropertyEventHandler;
        }

        void HandleChangedProperty(object s, PropertyValueChangedEventArgs e)
        {
            WireframeManager.Self.RefreshAll();
            
            // The name could have changed, so let's refresh the node if the selected
            // item is an AnimationChain:
            if (SelectedState.Self.SelectedFrame == null)
            {
                TreeViewManager.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
            }
            
            if (AnimationChainChange != null)
            {
                AnimationChainChange(this, null);
            }
            if (AnimationFrameChange != null)
            {
                AnimationFrameChange(this, null);

                if(e.ChangedItem.PropertyDescriptor.Name == "TextureName")
                {
                    // The texture changed, so let the displayer know:
                    mAnimationFrameDisplayer.SetFrame(SelectedState.Self.SelectedFrame,
                        WireframeManager.Self.Texture);
                }
            }
        }

        void HandleChangedPropertyEventHandler(object s, EventArgs args)
        {
            HandleChangedProperty(null, null);
        }

        public void Refresh()
        {
            if (SelectedState.Self.SelectedFrame != null)
            {
                mAnimationChainDisplayer.PropertyGrid = null;
                mAnimationFrameDisplayer.PropertyGrid = mPropertyGrid;
                mAnimationFrameDisplayer.SetFrame(SelectedState.Self.SelectedFrame,
                    WireframeManager.Self.Texture);
                mAnimationFrameDisplayer.RefreshOnTimer = true;
                mPropertyGrid.Refresh();

                string fileName = SelectedState.Self.SelectedFrame.TextureName;

            }
            else if (SelectedState.Self.SelectedChain != null)
            {
                mAnimationFrameDisplayer.PropertyGrid = null;
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
            if (SelectedState.Self.SelectedFrame == null || ApplicationState.Self.UnitType != AnimationEditorForms.UnitType.SpriteSheet)
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
