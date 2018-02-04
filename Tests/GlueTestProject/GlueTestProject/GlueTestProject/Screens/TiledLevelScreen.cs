#region Usings

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
using System.Linq;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

using GlueTestProject.TestFramework;
#endregion

namespace GlueTestProject.Screens
{
	public partial class TiledLevelScreen
	{

		void CustomInitialize()
		{

            InitializeLevel("Level1");

            foreach (var item in Level1Info)
            {
                if(item.EmbeddedAnimation != null && item.EmbeddedAnimation.Count != 0)
                {
                    AnimationChain animationChain = new AnimationChain();
                    foreach(var frame in item.EmbeddedAnimation)
                    {
                    }
                }
            }

            if (CreatedByTiledList.Count == 0)
            {
                throw new Exception("Entities created from tiled are not appearing in the screen's list");
            }

            if(CreatedByTiledList.Count(item=>item.Z == 2) == 0)
            {
                throw new Exception("Entities created from tiled object layers are not appearing in the screen's list with the right Z.");
            }

            TestTypeEntityCreation();
		}

        private void TestTypeEntityCreation()
        {
            CreatedByTiledTypeList.Count.ShouldNotBe(0, "because setting a Tiled object's Type should result in that entity being created");

            var first = CreatedByTiledTypeList[0];
            first.SetByDefaultInt.ShouldBe(4, "because entities should have default values set from the tileset");
        }

        void CustomActivity(bool firstTimeCalled)
		{
            if(!firstTimeCalled)
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
