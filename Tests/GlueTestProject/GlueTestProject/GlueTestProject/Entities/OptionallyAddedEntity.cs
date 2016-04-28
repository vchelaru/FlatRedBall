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
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;


#endif

namespace GlueTestProject.Entities
{
	public partial class OptionallyAddedEntity
	{
		private void CustomInitialize()
		{
            // Let's give this an XVelocity
            // Since it's not added to the managers
            // it should never move.  We can test the 
            // position in the activity
            this.NotAddedToManagers.RelativeXVelocity = 10;
            this.NotAddedToManagers.XVelocity = 10;

            if (this.NotInstantiated != null)
            {
                throw new Exception("Instantiated property is set to false but the object has been instantiated");
            }

		}

		private void CustomActivity()
		{

            if (NotAddedToManagers.X != 0 || NotAddedToManagers.RelativeX != 0)
            {
                throw new Exception("The NotAddedToManagers Entity is moving despite not being added to managers");
            }


            // let's test delayed instantiation
            if (this.DelayedInstantiationEntityInstance != null)
            {
                // Its activity sets itself to Y = 10, so it better have a Y = 10
                if (DelayedInstantiationEntityInstance.Y != 10)
                {
                    throw new Exception("NotInstantiated isn't getting its Activity called after it's instantiated");
                }
            }

            if (DelayedInstantiationEntityInstance == null)
            {
                DelayedInstantiationEntityInstance = new DelayedInstantiationEntity(ContentManagerName);
            }
		}

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
