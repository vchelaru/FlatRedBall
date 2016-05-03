using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
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
#endif

namespace GlueTestProject.Screens
{
	public partial class InstructionScreen
	{
        bool mHasInstructionBeenExecuted = false;

        public bool HasSetBeenExecuted
        {
            get;
            set;
        }
        
		void CustomInitialize()
		{
            Instructions.Add(new DelegateInstruction(() => mHasInstructionBeenExecuted = true));
            this.Set("HasSetBeenExecuted").To(true).After(0);
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (ActivityCallCount > 0)
            {
                if (!mHasInstructionBeenExecuted)
                {
                    throw new Exception("Instructions are not being executed for Screens");
                }
                if (!HasSetBeenExecuted)
                {
                    throw new Exception("Instructions using fluent interface are not being executed for Screens");
                }
            }
            if (ActivityCallCount > 15)
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
