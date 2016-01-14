using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace InstructionEditor.Gui
{
    public class ToolsWindow : EditorObjects.Gui.ToolsWindow
    {
        #region Fields

        private ToggleButton mMoveButton;
        private ToggleButton mScaleButton;
        private ToggleButton mRotateButton;

        private ToggleButton mAttachButton;
        private ToggleButton mDetachButton;

        private Button mCopyButton;
        #endregion

        #region Properties

        public ToggleButton MoveButton
        {
            get { return mMoveButton; }
        }

        public ToggleButton ScaleButton
        {
            get { return mScaleButton; }
        }

        public ToggleButton RotateButton
        {
            get { return mRotateButton; }
        }

        public ToggleButton AttachButton
        {
            get { return mAttachButton; }
        }

        public ToggleButton DetachButton
        {
            get { return mDetachButton; }
        }

        public Button CopyButton
        {
            get { return mCopyButton; }
        }

        #endregion

        #region Methods

        public ToolsWindow()
            : base()
        {
            mMoveButton = AddToggleButton(ToolsWindow.ToolsButton.Move, Microsoft.Xna.Framework.Input.Keys.M);
            mScaleButton = AddToggleButton(ToolsWindow.ToolsButton.Scale, Microsoft.Xna.Framework.Input.Keys.X);
            mRotateButton = AddToggleButton(ToolsWindow.ToolsButton.Rotate, Microsoft.Xna.Framework.Input.Keys.R);
            mAttachButton = AddToggleButton(ToolsWindow.ToolsButton.Attach, Microsoft.Xna.Framework.Input.Keys.A);
            mDetachButton = AddToggleButton(ToolsWindow.ToolsButton.Detach, Microsoft.Xna.Framework.Input.Keys.D);
            mCopyButton = AddButton(ToolsWindow.ToolsButton.Copy);

            mMoveButton.AddToRadioGroup(mScaleButton);
            mMoveButton.AddToRadioGroup(mRotateButton);

            mAttachButton.Enabled = false;
            
        }

        #endregion
    }
}
