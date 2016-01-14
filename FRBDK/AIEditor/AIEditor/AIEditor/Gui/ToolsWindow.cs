using System;
using System.Collections.Generic;
using System.Text;


using FlatRedBall;

using FlatRedBall.Gui;

#if FRB_MDX
using Microsoft.DirectX.DirectInput;
#else
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif


namespace AIEditor.Gui
{
    public class ToolsWindow : EditorObjects.Gui.ToolsWindow
    {
        #region Fields

        ToggleButton mMoveButton;
        ToggleButton mFindPathToNodeButton;

        #endregion

        #region Properties

        public bool IsMoveButtonPressed
        {
            get { return mMoveButton.IsPressed; }
        }

        public bool IsFindPathToNodeButtonPressed
        {
            get { return mFindPathToNodeButton.IsPressed; }
            set { mFindPathToNodeButton.Unpress(); }
        }

        #endregion

        #region Events

        void FindPathToNodeButtonPress(Window callingWindow)
        {
            EditorData.EditingLogic.ClearPath();
        }

        void MoveButtonPress(Window callingWindow)
        {
            EditorData.EditingLogic.ClearPath();
        }

        #endregion

        #region Methods

        public ToolsWindow()
            : base()
        {
            this.mMoveButton = AddToggleButton(Keys.M);
            this.mMoveButton.Text = "Move";
            this.mMoveButton.SetOverlayTextures(2, 0);

            Texture2D findPathTexture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\FindPathTo.png", "Global");

            mFindPathToNodeButton = AddToggleButton(Keys.F);
            mFindPathToNodeButton.Text = "Find Path To Node";
            mFindPathToNodeButton.SetOverlayTextures(findPathTexture, null);
            mFindPathToNodeButton.Push += FindPathToNodeButtonPress;
            
            mMoveButton.AddToRadioGroup(mFindPathToNodeButton);


        }

        #endregion

    }
}
