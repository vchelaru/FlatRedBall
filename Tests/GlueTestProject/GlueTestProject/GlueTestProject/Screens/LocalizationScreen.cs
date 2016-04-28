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
	public partial class LocalizationScreen
	{

		void CustomInitialize()
		{
            string translated = LocalizationManager.Translate("T_Hello");

            if (this.SceneFile.Texts[0].DisplayText != translated)
            {
                throw new Exception("Localiation of object-less Text objects in .scnx files in Screens does not seem to work");
            }
            if (this.TextObjectFromSceneObject.DisplayText != translated)
            {
                if (this.TextObjectFromSceneObject.DisplayText == translated + " - UNTRANSLATED")
                {
                    throw new Exception("Text objects that come from .scnx files in Screens are being translated twice");
                }
                else
                {
                    throw new Exception("Localization of Text objects that come from .scnx files in Screens are not translating properly");
                }
            }

            if (this.TextObject2.DisplayText != translated)
            {
                throw new Exception("Localization of Text from file without Entire Scene object is not working properly");
            }

            if (this.TextObjectNotFromFile.DisplayText != translated)
            {
                throw new Exception("Localization of custom variables on text objects from FRB types is not working properly");
            }
		}

		void CustomActivity(bool firstTimeCalled)
		{
            if (firstTimeCalled == false)
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
