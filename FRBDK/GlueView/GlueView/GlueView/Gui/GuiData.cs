using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;

namespace GlueView.Gui
{
    public static class GuiData
    {
        //static CameraAndScreenControlWindow mCameraAndScreenControlWindow;
        //public static LocalizationWindow LocalizationWindow
        //{
        //    get;
        //    private set;
        //}

        public static void Initialize()
        {
            // Camera and screen control window has been moved to winforms
            //mCameraAndScreenControlWindow = new CameraAndScreenControlWindow(GuiManager.Cursor);
            //GuiManager.AddWindow(mCameraAndScreenControlWindow);

            // Vic asks:  Why is this here?!?  Should be moved to EditorLogic.cs I think.
            FlatRedBall.SpriteManager.AutoIncrementParticleCountValue = 500;

            //LocalizationWindow = new LocalizationWindow(GuiManager.Cursor);
            //GuiManager.AddWindow(LocalizationWindow);
            //LocalizationWindow.X = LocalizationWindow.ScaleX;
            //LocalizationWindow.Y = LocalizationWindow.ScaleY + Window.MoveBarHeight;
            
            

        }
    }
}
