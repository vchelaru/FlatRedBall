using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;
using FlatRedBall.Utilities;
using FlatRedBall.Instructions;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;

namespace InstructionEditor.Gui
{
    public class KeyframeListSelectionWindow : Window
    {
        #region Fields

        ComboBox mObjectSelectionComboBox;
        ComboBox mKeyframeListSelectionComboBox;

        Button mOkButton;
        Button mCancelButton;

        Dictionary<INameable, InstructionSet> mInstructionSets;

        #endregion

        #region Properties

        public KeyframeList SelectedKeyframeList
        {
            get { return mKeyframeListSelectionComboBox.SelectedObject as KeyframeList; }
        }

        public INameable SelectedNameable
        {
            get { return mObjectSelectionComboBox.SelectedObject as INameable; }
        }

        #endregion

        #region Events

        public event GuiMessage OkClick;

        #endregion

        #region Event Methods

        private void OnOkClick(Window callingWindow)
        {
            if (OkClick != null)
                OkClick(this);

            GuiManager.RemoveWindow(this);
        }

        private void SelectKeyframe(Window callingWindow)
        {
            KeyframeList keyframeList = mKeyframeListSelectionComboBox.SelectedObject as KeyframeList;

            mOkButton.Enabled = keyframeList != null;
        }

        private void SelectObject(Window callingWindow)
        {
            mKeyframeListSelectionComboBox.Clear();
            if(mInstructionSets.ContainsKey(mObjectSelectionComboBox.SelectedObject as INameable))
            {
                InstructionSet instructionSet = mInstructionSets[mObjectSelectionComboBox.SelectedObject as INameable];

                foreach (KeyframeList keyframeList in instructionSet)
                {
                    mKeyframeListSelectionComboBox.AddItem(keyframeList.Name, keyframeList);
                }

                if (mKeyframeListSelectionComboBox.Count != 0)
                {
                    // Automatically select the first item
                    mKeyframeListSelectionComboBox.SelectItemByObject(instructionSet[0]);
                }
            }
        }

        #endregion

        #region Methods

        public KeyframeListSelectionWindow(Cursor cursor)
            : base(cursor)
        {
            #region This properties
            this.ScaleX = 10;
            this.ScaleY = 5;
            this.HasMoveBar = true;
            #endregion

            mObjectSelectionComboBox = this.AddComboBox();
            mObjectSelectionComboBox.ScaleX = this.ScaleX - 1;
            mObjectSelectionComboBox.X = ScaleX;
            mObjectSelectionComboBox.Y = 2;
            mObjectSelectionComboBox.ItemClick += SelectObject;

            mKeyframeListSelectionComboBox = this.AddComboBox();
            mKeyframeListSelectionComboBox.ScaleX = this.ScaleX - 1;
            mKeyframeListSelectionComboBox.X = ScaleX;
            mKeyframeListSelectionComboBox.Y = 5;
            mKeyframeListSelectionComboBox.ItemClick += SelectKeyframe;

            mOkButton = AddButton();
            mOkButton.Text = "Ok";
            mOkButton.X = 5.0f;
            mOkButton.ScaleX = 4.5f;
            mOkButton.Click += OnOkClick;
            mOkButton.Y = 8;
            mOkButton.Enabled = false;

            mCancelButton = AddButton();
            mCancelButton.Text = "Cancel";
            mCancelButton.X = 2 * this.ScaleX - 5.0f ;
            mCancelButton.ScaleX = 4.5f;
            mCancelButton.Y = 8;
            // Cancel closes the window.  Don't add this event to OK because it will be removed in the 
            // ok event.  The reason it's done that way is so that this Window's events aren't cleared.
            mCancelButton.Click += GuiManager.RemoveParentOfWindow; 

        }

        public void PopulateComboBoxes(Scene scene, Dictionary<INameable, InstructionSet> instructionSets)
        {
            #region Add the Scene
            foreach (Sprite sprite in scene.Sprites)
            {
                mObjectSelectionComboBox.AddItem(sprite.Name, sprite);
            }

            foreach (SpriteFrame spriteFrame in scene.SpriteFrames)
            {
                mObjectSelectionComboBox.AddItem(spriteFrame.Name, spriteFrame);
            }

            foreach (Text text in scene.Texts)
            {
                mObjectSelectionComboBox.AddItem(text.Name, text);
            }

            foreach (SpriteGrid spriteGrid in scene.SpriteGrids)
            {
                mObjectSelectionComboBox.AddItem(spriteGrid.Name, spriteGrid);
            }

            foreach (PositionedModel positionedModel in scene.PositionedModels)
            {
                mObjectSelectionComboBox.AddItem(positionedModel.Name, positionedModel);
            }
            #endregion

            mInstructionSets = instructionSets;
        }

        #endregion
    }
}
