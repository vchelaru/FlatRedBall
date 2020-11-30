using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.CodeGenerators
{
    public class AiCodeGenerator : FullFileCodeGenerator
    {
        static AiCodeGenerator mSelf;
        public static AiCodeGenerator Self
        {
            get
            {
                if(mSelf == null)
                {
                    mSelf = new AiCodeGenerator();
                }
                return mSelf;
            }
        }

        public override string RelativeFile => "TopDown/TopDownAiInput.Generated.cs";

        protected override string GenerateFileContents()
        {
            var toReturn = $@"
using FlatRedBall;
using FlatRedBall.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public class TopDownAiInput<T> : IInputDevice where T : PositionedObject, TopDown.ITopDownEntity
    {{
        const float requiredDistanceForNextTarget = 16;

        public bool RemoveTargetOnReaching
        {{
            get; set;
        }}

        public bool ShouldRemoveLastTarget {{ get; set; }}

        public bool StopOnTarget
        {{
            get; set;
        }} = true;

        public bool IsActive
        {{
            get; set;
        }} = true;

        public event Action<T> TargetReached;

        #region Internal Classes
        class Values2DInput : I2DInput
        {{
            public float X {{ get; set; }}

            public float Y {{ get; set; }}

            public float XVelocity => throw new NotImplementedException();

            public float YVelocity => throw new NotImplementedException();

            public float Magnitude => throw new NotImplementedException();
        }}

        #endregion

        #region IInputDevice Properties

        Values2DInput values2DInput = new Values2DInput();
        public virtual I2DInput Default2DInput => values2DInput;

        public virtual IPressableInput DefaultUpPressable => throw new NotImplementedException();

        public virtual IPressableInput DefaultDownPressable => throw new NotImplementedException();

        public virtual IPressableInput DefaultLeftPressable => throw new NotImplementedException();

        public virtual IPressableInput DefaultRightPressable => throw new NotImplementedException();

        public virtual I1DInput DefaultHorizontalInput => throw new NotImplementedException();

        public virtual I1DInput DefaultVerticalInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultPrimaryActionInput => throw new NotImplementedException();
        public virtual IPressableInput DefaultSecondaryActionInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultConfirmInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultJoinInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultPauseInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultBackInput => throw new NotImplementedException();

        #endregion

        public Vector3? Target {{ get; set; }}

        public List<Vector3> Path {{ get; private set; }} = new List<Vector3>();

        public T Owner {{ get; set; }}

        public TopDownAiInput(T owner)
        {{
            this.Owner = owner;
        }}

        public void Activity()
        {{
            values2DInput.X = 0;
            values2DInput.Y = 0;
            if(Target != null && Owner?.CurrentMovement != null && IsActive)
            {{
                var targetX = Target.Value.X;
                var targetY = Target.Value.Y;

                var xDiff = targetX - Owner.Position.X;
                var yDiff = targetY - Owner.Position.Y;

                var isCloseToFindNextTarget =
                    Math.Abs(xDiff) < requiredDistanceForNextTarget && Math.Abs(yDiff) < requiredDistanceForNextTarget;

                var shouldRemove = isCloseToFindNextTarget &&
                    RemoveTargetOnReaching &&
                    (Path.Count > 1 || ShouldRemoveLastTarget);

                if (shouldRemove)
                {{
                    TargetReached?.Invoke(Owner);
                    if(Path.Count > 0)
                    {{
                        Target = Path[0];
                        Path.RemoveAt(0);

                        // do it again
                        Activity();
                    }}
                    else
                    {{
                        Target = null;
                    }}

                }}
                else if(xDiff != 0 || yDiff != 0)
                {{
                    bool shouldMoveFullSpeed;
                    if(StopOnTarget)
                    {{
                        var currentMovementLength = Owner.Velocity.Length();
                        var currentRatioOfMax = currentMovementLength / Owner.CurrentMovement.MaxSpeed;

                        var currentTimeToSlowDown = currentRatioOfMax * Owner.CurrentMovement.DecelerationTime;
                        var maxSpeed = Owner.CurrentMovement.MaxSpeed;
                        var maxAccelerationValue = -maxSpeed/Owner.CurrentMovement.DecelerationTime;

                        //// create the temporary vectors:
                        // Not sure where but there's an off-by-1 error somewhere, so account for it by subtracting one frame.
                        var position = new Vector3((float)(2*currentMovementLength *
                             +FlatRedBallServices.Game.TargetElapsedTime.TotalSeconds), 0, 0);
                        var velocity = new Vector3(currentMovementLength, 0, 0);
                        var acceleration = new Vector3(maxAccelerationValue, 0, 0);

                        var positionAfterTime = FlatRedBall.Math.MathFunctions.GetPositionAfterTime(
                          ref position,
                          ref velocity,
                          ref acceleration,
                          currentTimeToSlowDown);

                        var lengthToSlow = Math.Abs(positionAfterTime.X);
                        shouldMoveFullSpeed = (xDiff * xDiff) + (yDiff * yDiff) > lengthToSlow * lengthToSlow;
                    }}
                    else
                    {{
                        shouldMoveFullSpeed = true;
                    }}

                    if(shouldMoveFullSpeed)
                    {{

                        var angle = (float)System.Math.Atan2(yDiff, xDiff);

                        values2DInput.X = (float)Math.Cos(angle);
                        values2DInput.Y = (float)Math.Sin(angle);
                    }}
                }}
            }}
        }}
    }}
}}


";

            return toReturn;
        }

    }
}
