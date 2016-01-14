using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Utilities
{
    #region XML Docs
    /// <summary>
    /// Contains information for simulating an expansion or contraction of a Sprite. 
    /// </summary>
    /// <remarks>
    /// This struct is used by a number of FlatRedball classes such as SpriteGrid and MathFunctions.
    /// </remarks>
    #endregion
    public struct SpriteSelectionOptions
    {

        public static SpriteSelectionOptions Default =
            new SpriteSelectionOptions(0, 0, 0, 0);

        public float TopAllowance;
        public float BottomAllowance;
        public float LeftAllowance;
        public float RightAllowance;

        public SpriteSelectionOptions(float topAllowance, float bottomAllowance, float leftAllowance, float rightAllowance)
        {
            TopAllowance = topAllowance;
            BottomAllowance = bottomAllowance;
            LeftAllowance = leftAllowance;
            RightAllowance = rightAllowance;
        }
    }
}
