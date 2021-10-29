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
using FlatRedBall.IO;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Content.Scene;
#endif

namespace GlueTestProject.Screens
{
	public partial class XmlScreen
	{

		void CustomInitialize()
		{
            SpriteSave spriteSave = new SpriteSave();
            spriteSave.X = 4;
            FileManager.InitializeUserFolder("Global");

            string directory = FileManager.GetUserFolder("Global");
            string fileName = directory + "TestSave.xml";
            FileManager.XmlSerialize(spriteSave, fileName);


            if(FileManager.FileExists(fileName) == false)
            {
                throw new Exception("The file " + fileName + " does not exist");
            }


            var loaded = FileManager.XmlDeserialize<SpriteSave>(fileName);

            if (loaded.X != 4)
            {
                throw new Exception("XML serialization is not working properly");
            }


            string fileToAddAndDelete = directory + "DeleteMe.xml";
            FileManager.XmlSerialize<SpriteSave>(spriteSave, fileName);
            FileManager.DeleteFile(fileName);



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
