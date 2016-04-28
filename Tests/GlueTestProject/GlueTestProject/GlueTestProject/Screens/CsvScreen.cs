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
using GlueTestProject.DataTypes;
using FlatRedBall.IO.Csv;
#endif

namespace GlueTestProject.Screens
{
    public class ManualCsvClass
    {
        public string Name { get; set; }
        public List<string> StringList { get; set; }


    }

	public partial class CsvScreen
	{
        bool mHasCsvVariableEventBeenRaised = false;

		void CustomInitialize()
		{
            if (StaticVariableEntity.FromGlobalCsv == null)
            {
                throw new Exception("Referencing a static entity's CSV variables is not working becaus the static variable is not properly set.  This variable is of a type defined in GlobalContent.");
            }

            // check if constants are made properly
            string in1 = SpreadsheetClass.FirstGuy;
            // to ignore warnings
            if (in1 != null){}

            string in2 = SpreadsheetClass.GuyInFile2;
            if (in2 != null) { }

            if (GlobalCsvVariable.Name != "Name2")
            {
                throw new Exception("Variables using global CSVs in Screens are not getting their values set");
            }

            if (GlobalCsvVariableWithEvent.Name != "Name3")
            {
                throw new Exception("Variables with events using global CSVs in Screens are not getting their values set");
            }

            if (mHasCsvVariableEventBeenRaised == false)
            {
                throw new Exception("Events for CSV variables that are set in Glue are not raised");
            }

            if (new DataTypes.Spreadsheet().ListOfString == null)
            {
                throw new Exception("new instances of CSV objects should 'new' any lists");
            }

            // Check that the class with no associated CSV actually exists - we'll get a compile error if not:
            ClassWithNoAssociatedCsvs instance = new ClassWithNoAssociatedCsvs();

            var list = CsvFileManager.CsvDeserializeList(typeof(ManualCsvClass),
                "Content/Screens/CsvScreen/CsvManuallyLoadedForPropertyTest.csv");

            ManualCsvClass entry = list[0] as ManualCsvClass;

            if (entry.StringList.Count == 0)
            {
                throw new NotImplementedException("CSVs that contain lists which are deserialized into properties are not set");
            }

            if (CsvUsingCustomDataFile["First"].StringListField.Count == 0)
            {
                throw new Exception("Primitive (string) lists as fields in custom data are not being deserialized properly");
            }

            if (CsvUsingCustomDataFile["First"].StringListProperty.Count == 0)
            {
                throw new Exception("Primitive (string) lists as properties in custom data are not being deserialized properly");
            }

            if (CsvUsingCustomDataFile["First"].ComplexCustomTypeListField.Count == 0)
            {
                throw new Exception("Complex type lists as fields in custom data are not being deserialized properly");
            }

            if (CsvUsingCustomDataFile["First"].ComplexCustomTypeListProperty.Count == 0)
            {
                throw new Exception("Complex type lists as properties in custom data are not being deserialized properly");
            }

            if (CsvUsingCustomDataFile["First"].ComplexCustomTypeListProperty[1].CustomEnumType != CustomDataTypes.CustomEnumType.Enum2)
            {
                throw new Exception("Enums in complex type property lists are not getting set.");
            }

            if (CsvUsingCustomDataFile["First"].StringProperty != "Test1")
            {
                throw new Exception("String properties are not being set correctly");
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
