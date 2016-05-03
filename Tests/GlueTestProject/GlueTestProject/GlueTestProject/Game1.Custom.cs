using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueTestProject
{
    public partial class Game1
    {
        void CustomPreGlobalContentInitialize()
        {
            GlobalContent.SpreadsheetToReplace = new Dictionary<string, DataTypes.SpreadsheetToReplace>();

            CsvFileManager.CsvDeserializeDictionary<string, DataTypes.SpreadsheetToReplace>(
                "content/globalcontent/replacewiththis.csv",
                GlobalContent.SpreadsheetToReplace
                );
        }

        void CustomInitialize()
        {


        }

        void CustomActivity()
        {


        }


    }
}
