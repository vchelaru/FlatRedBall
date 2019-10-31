using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XnaAndWinforms;
using RenderingLibrary;
using Cursor = InputLibrary.Cursor;
using InputLibrary;

// this class is pulled from a standard Glue library, but this plugin was created before that was
// stanardized, so this is a copy of that code.  Eventually we need to clean this up and use the class
// that is part of the standard libraries.
namespace TmxEditor.Controllers
{
    public class CameraPanningLogic
    {
        RenderingLibrary.Camera mCamera;
        GraphicsDeviceControl mControl;

        Cursor mCursor;
        Keyboard mKeyboard;

        SystemManagers mManagers;

        public event EventHandler Panning;

        public CameraPanningLogic(GraphicsDeviceControl graphicsControl, SystemManagers managers, Cursor cursor, Keyboard keyboard)
        {
            mManagers = managers;
            
            mKeyboard = keyboard;

            mCursor = cursor;
            mCursor.Initialize(graphicsControl);
            mCamera = managers.Renderer.Camera;
            mControl = graphicsControl;
            graphicsControl.XnaUpdate += new Action(Activity);

        }

        void Activity()
        {
            
            if (mCursor.MiddleDown && 
                mCursor.IsInWindow)
            {
                if (mCursor.XChange != 0 || mCursor.YChange != 0)
                {
                    mCamera.X -= mCursor.XChange / mManagers.Renderer.Camera.Zoom;
                    mCamera.Y -= mCursor.YChange / mManagers.Renderer.Camera.Zoom;

                    if(Panning != null)
                    {
                        Panning(this, null);
                    }
                }
            }

        }


    }
}
