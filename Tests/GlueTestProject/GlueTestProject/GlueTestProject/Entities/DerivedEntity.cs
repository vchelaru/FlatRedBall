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
	public partial class DerivedEntity
	{
		private void CustomInitialize()
		{
            var texture = BobbleBikerLogo;
            if (this.SpriteReferencingTextureInBase.Texture != BobbleBikerLogo)
            {
                throw new Exception("Sprites in derived classes should be able to reference files in base classes");
            }


            if (this.CircleObject == null)
            {
                throw new Exception("The CircleObject in the derived Entity was never initialized.  It should not be null");

            }
            if (this.CircleObject.Radius < 1.99f)
            {
                throw new Exception("Derived variables are being overidden by base variables");
            }

            if (this.CircleInstanceExposedInDerived.Radius < 1.99f)
            {
                throw new Exception("Derived variables in exposed in derived objects are being overridden by base");
            }

            if (this.CircleCreatedInDerived.RelativeX != 22)
            {
                throw new Exception("Instance variables set on objects defined in derived entities are not applying");
            }
		}

		private void CustomActivity()
		{


		}

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {
            // Any file that's loaded by the base
            // Entity should be loaded before the derived
            // Entity has a chance to do anything.
            if (StaticFile1 == null)
            {
                throw new Exception("The base Entity has not properly instantiated its File.  This should be instantiated by now.");
            }

        }
	}
}
