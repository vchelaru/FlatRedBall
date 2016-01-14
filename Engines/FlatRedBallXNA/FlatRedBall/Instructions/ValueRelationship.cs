using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Stores the related state, velocity, and acceleration values.
    /// </summary>
    /// <remarks>
    /// This is used when interpolating instructions.
    /// </remarks>
    #endregion
    public class VelocityValueRelationship
    {
        public string State;
        public string Velocity;
        public string Acceleration;

        public VelocityValueRelationship(string state, string velocity, string acceleration)
        {
            State = state;
            Velocity = velocity;
            Acceleration = acceleration;
        }

        public override string ToString()
        {
            return State + " - " + Velocity + " - " + Acceleration;
        }
    }

    public class AnimationValueRelationship
    {
        public string Frame;
        public string AnimationSpeed;
        public string CurrentAnimationObject;
        public string NumberOfFrames;

        public AnimationValueRelationship(string frame, string animationSpeed, string currentAnimationObject, string numberOfFrames)
        {
            Frame = frame;
            AnimationSpeed = animationSpeed;
            CurrentAnimationObject = currentAnimationObject;
            NumberOfFrames = numberOfFrames;
        }


    }

    public class AbsoluteRelativeValueRelationship
    {
        public string AbsoluteValue;
        public string RelativeValue;

        public AbsoluteRelativeValueRelationship(string absoluteValue, string relativeValue)
        {
            AbsoluteValue = absoluteValue;
            RelativeValue = relativeValue;
        }

        public override string ToString()
        {
            return AbsoluteValue + " : " + RelativeValue;
        }

    }
}
