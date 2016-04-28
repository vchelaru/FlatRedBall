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
using GlueTestProject.Entities;
using GlueTestProject.Factories;
#endif

namespace GlueTestProject.Screens
{
	public partial class ListScreen
	{

		void CustomInitialize()
		{
            var addAndRemove = TextInheritingEntityFactory.CreateNew();

            addAndRemove.Destroy();
            if (TextInheritingEntityList.Contains(addAndRemove))
            {
                throw new Exception("Destroying an instance of a CreatedByOtherEntities Entity " + 
                    "which inherits from a FRB type doesn't remove it from lists that it is a part of, and it should");
            }
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
