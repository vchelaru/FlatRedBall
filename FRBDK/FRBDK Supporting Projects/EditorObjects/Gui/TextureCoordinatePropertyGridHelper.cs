using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    class TextureCoordinatePropertyGridHelper
    {
        //public static void CreatePixelCoordinateUi(PropertyGrid propertyGrid,
        //    string topProperty, string bottomProperty, string leftProperty, string rightProperty)
        //{
        //    UpDown throwaway;

        //    CreatePixelCoordinateUi(propertyGrid, topProperty, bottomProperty, leftProperty, rightProperty,
        //        out throwaway, out throwaway, out throwaway, out throwaway);
        //}

        public static void CreatePixelCoordinateUi(PropertyGrid propertyGrid,
            string topProperty, string bottomProperty, string leftProperty, string rightProperty,
            string category,
            out UpDown topPixel, out UpDown leftPixel, out UpDown heightPixel, out UpDown widthPixel)
        {
            propertyGrid.ExcludeMember(topProperty);
            propertyGrid.ExcludeMember(bottomProperty);
            propertyGrid.ExcludeMember(leftProperty);
            propertyGrid.ExcludeMember(rightProperty);

            leftPixel = new UpDown(GuiManager.Cursor);
            leftPixel.ScaleX = 7;
            leftPixel.Precision = 0;
            propertyGrid.AddWindow(leftPixel, category);
            propertyGrid.SetLabelForWindow(leftPixel, "Left Pixel");

            topPixel = new UpDown(GuiManager.Cursor);
            topPixel.ScaleX = 7;
            topPixel.Precision = 0;
            propertyGrid.AddWindow(topPixel, category);
            propertyGrid.SetLabelForWindow(topPixel, "Top Pixel");


            widthPixel = new UpDown(GuiManager.Cursor);
            widthPixel.ScaleX = 7;
            widthPixel.Precision = 0;
            propertyGrid.AddWindow(widthPixel, category);
            propertyGrid.SetLabelForWindow(widthPixel, "Pixel Width");


            heightPixel = new UpDown(GuiManager.Cursor);
            heightPixel.ScaleX = 7;
            heightPixel.Precision = 0;
            propertyGrid.AddWindow(heightPixel, category);
            propertyGrid.SetLabelForWindow(heightPixel, "Pixel Height");
        }
    }
}
