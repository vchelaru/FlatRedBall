using System;
using System.Collections.Generic;
using System.Text;

namespace EditorObjects.Data
{
    public class LineGridSave
    {
        #region Fields

        public int NumberOfHorizontalLines = 11;
        public int NumberOfVerticalLines = 11;

        // The world coordinate centers
        public float X;
        public float Y;

        public float DistanceBetweenLines = 1;

        public bool Visible = true;

        #endregion

        #region Methods


        public static LineGridSave FromLineGrid(LineGrid lineGrid)
        {
            LineGridSave lineGridSave = new LineGridSave();

            lineGridSave.NumberOfHorizontalLines = lineGrid.NumberOfHorizontalLines;
            lineGridSave.NumberOfVerticalLines = lineGrid.NumberOfVerticalLines;

            lineGridSave.X = lineGrid.X;
            lineGridSave.Y = lineGrid.Y;

            lineGridSave.DistanceBetweenLines = lineGrid.DistanceBetweenLines;

            lineGridSave.Visible = lineGrid.Visible;

            return lineGridSave;
        }

        public void ToLineGrid(LineGrid lineGridToModify)
        {
            lineGridToModify.NumberOfHorizontalLines = NumberOfHorizontalLines;
            lineGridToModify.NumberOfVerticalLines = NumberOfVerticalLines;

            lineGridToModify.X = X;
            lineGridToModify.Y = Y;

            lineGridToModify.DistanceBetweenLines = DistanceBetweenLines;

            lineGridToModify.Visible = Visible;
        }

        #endregion
    }
}
