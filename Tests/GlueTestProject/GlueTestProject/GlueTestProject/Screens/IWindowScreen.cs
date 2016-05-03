using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Gui;
using GlueTestProject.Entities;
using GlueTestProject.Factories;
#endif

namespace GlueTestProject.Screens
{
	public partial class IWindowScreen
	{

		void CustomInitialize()
		{
            IWindowContainerInstance.Visible = false;
            if (((IWindow)IWindowContainerInstance.IWindowEntityInstance).Visible)
            {
                throw new Exception("The parent IWindowContainerInstance is invisible, but its child still reports its IWindow.Visible as true.  It should be false");
            }

            var newInstance = new IWindowDerivedFromNoIWindow(this.ContentManagerName);

            if (GuiManager.Windows.Contains(newInstance) == false)
            {
                throw new Exception("IWindow instances that inherit from non-IWindows are not being added to the GuiManager");
            }
            newInstance.Destroy();

            var result = IWindowDerivedFromNoIWindowFactory.CreateNew();
            if (GuiManager.Windows.Contains(result) == false)
            {
                throw new Exception("Instantiated IWindows by factories are not added to the GuiManager and they should be!");
            }
            result.Destroy();


        }

		void CustomActivity(bool firstTimeCalled)
		{
            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
