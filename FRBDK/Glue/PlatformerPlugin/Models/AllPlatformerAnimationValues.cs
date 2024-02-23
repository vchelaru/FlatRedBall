using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformerPluginCore.SaveClasses
{
    public enum AnimationSpeedAssignment
    {
        ForceTo1,
        NoAssignment,
        BasedOnVelocityMultiplier,
        BasedOnMaxSpeedRatioMultiplier,
        BasedOnHorizontalInputMultiplier

    }

    public class AllPlatformerAnimationValues
    {
        public List<IndividualPlatformerAnimationValues> Values { get; set; } = new List<IndividualPlatformerAnimationValues>();
    }

    public class IndividualPlatformerAnimationValues
    {
        public string AnimationName { get; set; }

        public bool HasLeftAndRight { get; set; }

        public float? MinXVelocityAbsolute { get; set; }
        public float? MaxXVelocityAbsolute { get; set; }

        public float? MinYVelocity { get; set; }
        public float? MaxYVelocity { get; set; }

        public float? MinHorizontalInputAbsolute { get; set; }
        public float? MaxHorizontalInputAbsolute { get; set; }

        public float? AbsoluteXVelocityAnimationSpeedMultiplier { get; set; }
        public float? AbsoluteYVelocityAnimationSpeedMultiplier { get; set; }

        public float? MaxSpeedXRatioMultiplier { get; set; }
        public float? MaxSpeedYRatioMultiplier { get; set; }


        public bool? OnGroundRequirement { get; set; }

        public string MovementName { get; set; }
        public string CustomCondition { get; set; }

        public AnimationSpeedAssignment AnimationSpeedAssignment { get; set; }

        public string Notes { get; set; }
    }
}
