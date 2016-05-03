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
#endif

namespace GlueTestProject.Screens
{
	public partial class InheritanceScreen
	{

		void CustomInitialize()
		{
            // populate the list with some instances of the Sprite-inheriting Entity.
            // We're going to put them on a Layer.  In old Glue (before February 2014)
            // the List would remove itself first, removing the entity from all lists including
            // the list that the Screen has.  
            for (int i = 0; i < 3; i++)
            {
                Entities.SpriteInheritingEntity newInstance = new Entities.SpriteInheritingEntity(
                    ContentManagerName, false);
                newInstance.AddToManagers(this.LayerInstance);

                this.SpriteInheritingEntityList.Add(newInstance);
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
