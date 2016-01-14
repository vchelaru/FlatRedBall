using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

#if FRB_MDX
using Microsoft.DirectX;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
#endif

namespace EditorObjects.Gui
{
    public class Vector3OkWindow : Window
    {
        #region Fields

        Vector3Display mVector3Display;
        Button okButton;

        #endregion

        #region Properties

        public Vector3 Vector3Value
        {
            set { mVector3Display.Vector3Value = value; }   
            get { return mVector3Display.Vector3Value; }
        }

        #endregion

        #region Events

        public event GuiMessage OkClick;

        #endregion

        #region Event Methods

        private void ShiftSceneOk(Window callingWindow)
        {
            if (OkClick != null)
            {
                OkClick(this);
            }
        }

        #endregion


        public Vector3OkWindow(Cursor cursor)
            : base(cursor)
        {
            this.ScaleX = 6.5f;
            this.ScaleY = 6.0f;
            HasMoveBar = true;
            HasCloseButton = true;

            mVector3Display = new Vector3Display(cursor);
            this.AddWindow(mVector3Display);
            mVector3Display.Y = .5f + mVector3Display.ScaleY;

            this.Closing += GuiManager.RemoveWindow;

            Button okButton = new Button(mCursor);
            AddWindow(okButton);
            const float border = .5f;
            okButton.Y = this.ScaleY * 2 - okButton.ScaleY - border;
            okButton.ScaleX = 3;
            okButton.Text = "Ok";
            okButton.Click += ShiftSceneOk;
        }

    }
}
