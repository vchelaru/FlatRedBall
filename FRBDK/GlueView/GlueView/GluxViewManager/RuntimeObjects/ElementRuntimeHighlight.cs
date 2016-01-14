using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics;
using System.Reflection;
using FlatRedBall.ManagedSpriteGroups;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Glue.RuntimeObjects;

namespace FlatRedBall.Glue
{
    public class ElementRuntimeHighlight : Highlight
    {
        #region Enums

        enum FadingInOrOut
        {
            FadingIn,
            FadingOut
        }

        #endregion

        #region Fields

        float mAlpha = 1;
        FadingInOrOut mFadeState;
        #endregion

        #region Properties

        public bool FadeInAndOut
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public ElementRuntimeHighlight()
        {

        }

        public void Activity()
        {
            ////////////////////////////EARLY OUT//////////////////////////
            if (mHighlightShapes == null || mHighlightShapes.Count == 0 || FadeInAndOut == false)
            {
                return;
            }
            ////////////////////////END EARLY OUT//////////////////////////

            const float fadeRate = 2;

            switch (mFadeState)
            {
                case FadingInOrOut.FadingIn:

                    mAlpha += TimeManager.SecondDifference * fadeRate;

                    if (mAlpha > .99f)
                    {
                        mFadeState = FadingInOrOut.FadingOut;
                    }

                    break;
                case FadingInOrOut.FadingOut:
                    mAlpha -= TimeManager.SecondDifference * fadeRate;

                    if (mAlpha < .01f)
                    {
                        mFadeState = FadingInOrOut.FadingIn;
                    }
                    break;
            }

            mAlpha = System.Math.Max(0, mAlpha);
            mAlpha = System.Math.Min(1, mAlpha);


            Color color = new Color(new Vector4(mAlpha, mAlpha, mAlpha, mAlpha));

            foreach (Polygon polygon in mHighlightShapes)
            {
                polygon.Color = color;
            }
        }

        #endregion
    }
}
