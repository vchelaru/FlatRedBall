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
	public partial class CsvEntity
	{
		private void CustomInitialize()
		{
            if (this.SpriteInstanceTexture != yellowball)
            {
                throw new Exception("Custom script is not properly overriding the default Texture");
            }

            if (StaticCsv != CsvForVariable[DataTypes.CsvForVariable.Robot])
            {
                throw new Exception("Static types aren't being set properly");
            }

            if (CustomTypeCsvVariable.Health.Name != "UmpireNeck")
            {
                throw new Exception("Custom types are not deserializing in game properly");
            }
            if (CustomTypeCsvVariable.ListOfDataTypes.Count == 0)
            {
                throw new Exception("List of custom types in CSV are not deserializing to a list properly");
            }
            if (CustomTypeCsvVariable.ListOfDataTypes[2].WhereCanBeBought.Count == 0)
            {
                throw new Exception("Lists of items inside custom types inside a list are not deserialized properly");
            }
            if (CustomTypeCsvVariable.ListOfDataTypes[2].WhereCanBeBought[0] != "SLC")
            {
                throw new Exception("Lists in custom types in lists are not deserialized properly");
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


        }
	}
}
