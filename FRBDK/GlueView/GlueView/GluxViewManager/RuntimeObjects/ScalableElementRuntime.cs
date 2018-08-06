using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;

namespace FlatRedBall.Glue.RuntimeObjects
{
    public class ScalableElementRuntime : ElementRuntime, IScalable
    {

        public float ScaleX
        {
            get
            {
                return DirectScalableReference.ScaleX;
            }
            set
            {
                DirectScalableReference.ScaleX = value;
            }
        }

        public float ScaleY
        {
            get
            {
                return DirectScalableReference.ScaleY;
            }
            set
            {
                DirectScalableReference.ScaleY = value;
            }
        }

        public float ScaleXVelocity
        {
            get
            {
                return DirectScalableReference.ScaleXVelocity;
            }
            set
            {
                DirectScalableReference.ScaleXVelocity = value;
            }
        }

        public float ScaleYVelocity
        {
            get
            {
                return DirectScalableReference.ScaleYVelocity;
            }
            set
            {
                DirectScalableReference.ScaleYVelocity = value;
            }
        }

        public IScalable DirectScalableReference
        {
            get
            {
                return DirectObjectReference as IScalable;
            }
        }

    }
}
