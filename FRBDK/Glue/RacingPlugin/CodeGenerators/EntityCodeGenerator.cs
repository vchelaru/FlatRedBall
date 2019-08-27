using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using RacingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.CodeGenerators
{
    class EntityCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////
            if(GetIfIsRacingEntity(element) == false)
            {
                return codeBlock;
            }
            ///////////////End Early Out///////////////////

            codeBlock.Line("float lateralToForwardTransferRatio;");
            codeBlock.Line("float currentTurnRate;");
            codeBlock.Line("bool IsAllowedToDrive;");
            codeBlock.Line("bool wasMovingForwardAtStartOfCollisionRecording;");

            codeBlock.Line("CollisionHistory collisionHistory = new CollisionHistory();");

            var property = codeBlock.Property("public float", "EffectiveStability");
            var get = property.Get();
            get.Line("var toReturn = CarData.Stability;");
            var ifBlock = get.If("!Gas.IsDown")
                .Line("toReturn += CarData.NoGasExtraStability;");
            ifBlock = get.If("currentTurnRate == 0")
                .Line("toReturn += CarData.NoTurnExtraStability;");

            get.Line("return toReturn;");

            codeBlock.Property("public Microsoft.Xna.Framework.Vector3", "Forward")
                .Get()
                    .Line("return RotationMatrix.Up;");

            codeBlock.Property("public Microsoft.Xna.Framework.Vector3", "Right")
                .Get()
                    .Line("return RotationMatrix.Right;");

            codeBlock.Property("public Microsoft.Xna.Framework.Vector3", "Left")
                .Get()
                    .Line("return RotationMatrix.Left;");

            codeBlock.AutoProperty("private FlatRedBall.Input.IPressableInput", "Gas");
            codeBlock.AutoProperty("private FlatRedBall.Input.IPressableInput", "Brake");
            codeBlock.AutoProperty("private FlatRedBall.Input.I1DInput", "SteeringInput");

            codeBlock.Property("public float", "CurrentForwardSpeed")
                .Get()
                    .Line("return Microsoft.Xna.Framework.Vector3.Dot(this.Velocity, this.Forward);");

            codeBlock.Property("public float", "CurrentLateralSpeed")
                .Get()
                    .Line("return Microsoft.Xna.Framework.Vector3.Dot(this.Velocity, this.Right);");

            GenerateLateralSpeedAdjustmentActivityMethod(codeBlock);

            GenerateForwardBackActivityMethod(codeBlock);

            GenerateTurningActivity(codeBlock);

            GenerateBeforeAfterCollisionLogic(codeBlock);

            return codeBlock;
            /*


        public bool IsAllowedToDrive
        {
            get; set;
        } = true;
             */
        }

        private void GenerateBeforeAfterCollisionLogic(ICodeBlock codeBlock)
        {
            var beforeMethod = codeBlock.Function("public void", "RecordBeforeCollisionState");
            beforeMethod.Line("wasMovingForwardAtStartOfCollisionRecording = this.CurrentForwardSpeed > 0;");

            var afterMethod = codeBlock.Function("public void", "RecordAfterCollisionState");
            afterMethod.Line(@"
                bool isMovingForward = this.CurrentForwardSpeed > 0;
                if (this.CurrentForwardSpeed > 0)
                {
                    collisionHistory.LastTimeMovingForward = TimeManager.CurrentTime;
                }

                if (this.Brake.IsDown)
                {
                    collisionHistory.LastBrakePressedTime = TimeManager.CurrentTime;
                }

                if (wasMovingForwardAtStartOfCollisionRecording && !isMovingForward)
                {
                    // collided, moving in reverse
                    collisionHistory.LastReverseSendingCollisionTime = TimeManager.CurrentTime;
                }
");
        }

        private void GenerateTurningActivity(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("private void", "TurningActivity");
            method.Line(@"

            if (IsAllowedToDrive)
            {
                const float steeringDeadzone = .05f;
                const float minSpeedForMaxTurn = 30;
                               
                float turningValue = -SteeringInput.Value;

                bool isInDeadZone = Math.Abs(turningValue) < steeringDeadzone;

                bool isReturningToNeutral =
                    // User is not holding anything:
                    isInDeadZone ||
                    // User is holding the opposite direction of which way the car is turning
                    (Math.Sign(turningValue) * Math.Sign(currentTurnRate) == -1);

                float adjustment = 0;

                if (!isReturningToNeutral)
                {
                    adjustment =
                        turningValue * CarData.MaxTurnRate * TimeManager.SecondDifference / CarData.TimeToMaxTurn;
                }
                else
                {
                    adjustment =
                        -CarData.MaxTurnRate * System.Math.Sign(currentTurnRate) * TimeManager.SecondDifference / CarData.SteeringWheelToNormalTime;

                    bool isLessThan25Percent = Math.Abs(currentTurnRate) * 4 < CarData.MaxTurnRate;

                    if (isLessThan25Percent)
                    {
                        adjustment *= .25f;
                    }
                }

                currentTurnRate += adjustment;

                if (isReturningToNeutral)
                {
                    // to make it not wiggle around 0
                    if (Math.Abs(currentTurnRate) < .05f)
                    {
                        currentTurnRate = 0;
                    }
                }

                currentTurnRate = System.Math.Max(currentTurnRate, -CarData.MaxTurnRate);
                currentTurnRate = System.Math.Min(currentTurnRate, CarData.MaxTurnRate);

                // This prevents the car from turning when stopped, or from turning very fast when moving slowly.

                this.RotationZVelocity = currentTurnRate;

                bool shouldReduceTurningFromSpeed = this.CurrentForwardSpeed < minSpeedForMaxTurn;

                if (shouldReduceTurningFromSpeed)
                {
                    // This not only applies a ratio, but also reverses the rotation when the car is going in reverse.
                    // However, when a car smashes into a wall, it's really disruptive to have the car go into reverse steering.
                    // therefore, we'll use a CollisionHistory instance to tell us if we should:
                    float ratio = 1;
                    if (CurrentForwardSpeed > 0)
                    {
                        ratio = CurrentForwardSpeed / minSpeedForMaxTurn;
                    }
                    else // moving backwards
                    {
                        bool shouldSteerLikeMovingForward = collisionHistory.GetIfShouldApplyForwardTurningControls();
                        if (shouldSteerLikeMovingForward)
                        {
                            ratio = Math.Abs(CurrentForwardSpeed) / minSpeedForMaxTurn;
                        }
                        else
                        {
                            ratio = CurrentForwardSpeed / minSpeedForMaxTurn;
                        }
                    }


                    this.RotationZVelocity *= ratio;
                }
            }
");

        }

        private void GenerateForwardBackActivityMethod(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("private void", "ForwardBackActivity");
            method.Line(@"
            if (IsAllowedToDrive)
            {
                if (Brake.IsDown)
                {
                    float forwardSpeed = CurrentForwardSpeed;

                    // Moving forward, so this is the brake
                    if (forwardSpeed > 0)
                    {
                        // slow down
                        var amountToSubtract = Math.Min(forwardSpeed, TimeManager.SecondDifference * CarData.BrakeStoppingAcceleration);
                        Velocity -= Forward * amountToSubtract;
                    }

                    // re-get the forward speed since brakes may have adjusted it:
                    const float epsilon = 0.001f;
                    forwardSpeed = CurrentForwardSpeed;
                    if (forwardSpeed < epsilon)
                    {
                        // Stopped or moving backward, so we're going to accelerate backwards
                        // Add since CurrentForwardSpeed is negative
                        float maxAmountToSubtract = CarData.ReverseMaxSpeed + CurrentForwardSpeed;

                        float amountToSubtract = System.Math.Min(TimeManager.SecondDifference * CarData.ReverseAcceleration, maxAmountToSubtract);
                        amountToSubtract = System.Math.Max(0, amountToSubtract);

                        Velocity -= Forward * amountToSubtract;

                    }
                }
                else if (Gas.IsDown)
                {
                    // This allows cars to actually travel faster than their max 
                    // (like through boosts or being bumped) without allowing cars to keep accelerating past max speed.
                    float maxAmountToAdd = CarData.EffectiveMaxSpeed - CurrentForwardSpeed;

                    var effectiveAcceleration = CarData.ForwardAcceleration;
                    //foreach (var modifier in Modifiers)
                    //{
                    //    modifier.AdjustAcceleration(ref effectiveAcceleration);
                    //}

                    float amountToAdd = System.Math.Min(TimeManager.SecondDifference * effectiveAcceleration, maxAmountToAdd);
                    amountToAdd = System.Math.Max(0, amountToAdd);
                    Velocity += Forward * amountToAdd;
                }
                else
                {
                    float forwardSpeed = CurrentForwardSpeed;
                    float amountToSubtract = 0;
                    if (forwardSpeed > 0)
                    {
                        amountToSubtract = Math.Min(forwardSpeed, TimeManager.SecondDifference * 
                            CarData.NoGasSlowdownRate);
                    }
                    else
                    {
                        amountToSubtract = Math.Max(forwardSpeed, -TimeManager.SecondDifference * 
                            CarData.NoGasSlowdownRate);
                    }

                    Velocity -= Forward * amountToSubtract;
                }

                if (CurrentForwardSpeed > CarData.EffectiveMaxSpeed)
                {
                    Velocity -= Forward * TimeManager.SecondDifference * CarData.FastSlowDown;
                }
            }
");
            /*
             *          
            

             * 
             */
        }

        private void GenerateLateralSpeedAdjustmentActivityMethod(ICodeBlock codeBlock)
        {
            codeBlock = codeBlock.Function("private void", "LateralSpeedAdjustmentActivity", "");

            codeBlock.Line(@"
            float lateralSpeed = CurrentLateralSpeed;

            if (lateralSpeed != 0)
            {
                var velocity2D = Velocity;
                velocity2D.Z = 0;

                float lateralDampening = EffectiveStability; // this makes the car not slide
                float forwardPercentage = CurrentForwardSpeed / velocity2D.Length();

                float amountToAdjustBy = lateralDampening * TimeManager.SecondDifference;
                if (amountToAdjustBy > Math.Abs(lateralSpeed))
                {
                    amountToAdjustBy = Math.Abs(lateralSpeed);
                }

                lateralToForwardTransferRatio = forwardPercentage;

                var whatToSubtract = amountToAdjustBy * Right * Math.Sign(lateralSpeed);

                float lengthSquaredBeforeSubtraction = velocity2D.LengthSquared();

                if (lengthSquaredBeforeSubtraction != 0)
                {
                    Velocity -= whatToSubtract;

                    float optimalForwardVectorLength = (float)System.Math.Sqrt(
                        lengthSquaredBeforeSubtraction - (CurrentLateralSpeed * CurrentLateralSpeed));
                    float currentForwardVectorLength = CurrentForwardSpeed;
                    if (CurrentForwardSpeed > 0)
                    {
                        float optimalAdd = optimalForwardVectorLength - currentForwardVectorLength;

                        float actualAdd = optimalAdd * forwardPercentage;

                        this.Velocity += actualAdd * Forward;
                    }
                }
            }
");


            /*
             * 

             */



        }

        private bool GetIfIsRacingEntity(IElement element)
        {
            return element is EntitySave && element.Properties
                .GetValue<bool>(nameof(RacingEntityViewModel.IsRacingEntity));
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////
            if (GetIfIsRacingEntity(element) == false)
            {
                return codeBlock;
            }
            ///////////////End Early Out///////////////////
            
            codeBlock.Line("ForwardBackActivity();");

            codeBlock.Line("LateralSpeedAdjustmentActivity();");

            codeBlock.Line("TurningActivity();");

            return codeBlock;
        }
    }
}
