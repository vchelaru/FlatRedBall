using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;

namespace FlatRedBall.Content.Saves
{
    #region XML Docs
    /// <summary>
    /// Base class for TextSave and TextSaveContent.
    /// </summary>
    #endregion
    public class TextSaveBase
    {
        #region Fields

        public float X;
        public float Y;
        public float Z;

        public float RotationX;
        public float RotationY;
        public float RotationZ;

        public string DisplayText;

        public string Name;
        public string Parent;

        public float Scale;
        public float Spacing;
        public float NewLineDistance;

        public float MaxWidth = float.PositiveInfinity;
        public MaxWidthBehavior MaxWidthBehavior = MaxWidthBehavior.Wrap;

        public VerticalAlignment VerticalAlignment;
        public HorizontalAlignment HorizontalAlignment;

        public bool Visible = true;

        public bool CursorSelectable = true;

        public string FontTexture;
        public string FontFile;

        // When adding Red, Green, Blue, be sure to do the conversions between FRB MDX and FRB XNA
        public float Red = 255;
        public float Green = 255;
        public float Blue = 255;

        public string ColorOperation = "SelectArg2";

        public float RelativeX;
        public float RelativeY;
        public float RelativeZ;

        public float RelativeRotationX;
        public float RelativeRotationY;
        public float RelativeRotationZ;

        #endregion
    }
}
