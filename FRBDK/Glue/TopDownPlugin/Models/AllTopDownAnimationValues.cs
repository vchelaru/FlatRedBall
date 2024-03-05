using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.Models
{
    public enum AnimationSpeedAssignment
    {
        ForceTo1,
        NoAssignment,
        BasedOnVelocityMultiplier,
        BasedOnMaxSpeedRatioMultiplier,
        BasedOnHorizontalInputMultiplier

    }

    internal class AllTopDownAnimationValues
    {
        public List<IndividualTopDownAnimationValues> Values { get; set; }
         = new List<IndividualTopDownAnimationValues>();
    }

    public class IndividualTopDownAnimationValues
    {
        public string AnimationName { get; set; }
        public bool IsDirectionFacingAppended { get; set; } = true;


        public float? MinVelocityAbsolute { get; set; }
        public float? MaxVelocityAbsolute { get; set; }

        public float? AbsoluteVelocityAnimationSpeedMultiplier { get; set; }

        public float? MinMovementInputAbsolute { get; set; }
        public float? MaxMovementInputAbsolute { get; set; }

        public float? MaxSpeedRatioMultiplier { get; set; }

        public string MovementName { get; set; }

        public string CustomCondition { get; set; }


        public AnimationSpeedAssignment AnimationSpeedAssignment { get; set; }

        public string Notes { get; set; }
    }
}
