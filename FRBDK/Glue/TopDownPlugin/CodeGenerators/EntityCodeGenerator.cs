using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Controllers;
using TopDownPlugin.Data;
using TopDownPlugin.Logic;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.CodeGenerators
{
    public class EntityCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        public override void GenerateAdditionalClasses(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////////
            var entitySave = element as EntitySave;
            if (entitySave == null)
            {
                return;
            }

            //////////////End Early Out//////////////////////
            ///

            if (MainController.Self.GetIfInheritsFromTopDown(entitySave))
            {
                GenerateDerivedElementSave(element, codeBlock);
            }
            else if(TopDownEntityPropertyLogic.GetIfIsTopDown(entitySave))
            {
                GenerateTopDownElementSave(element, codeBlock);

            }

        }

        string projectNamespace => GlueState.Self.ProjectNamespace;

        private void GenerateDerivedElementSave(IElement element, ICodeBlock codeBlock)
        {

            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CsvInheritanceSupport)
            {
                var className = element.GetStrippedName();
                codeBlock = codeBlock.Class("public partial", className, "");
                codeBlock.Line($"protected override System.Collections.Generic.Dictionary<string, {projectNamespace}.DataTypes.TopDownValues> TopDownValues => TopDownValuesStatic;");
            }


        }

        private void GenerateTopDownElementSave(IElement element, ICodeBlock codeBlock)
        {
            var className = element.GetStrippedName();

            codeBlock = codeBlock.Class("public partial", className, ": TopDown.ITopDownEntity");

            codeBlock.Line("#region Top Down Fields");

            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CsvInheritanceSupport)
            {
                codeBlock.Line($"protected virtual System.Collections.Generic.Dictionary<string, {projectNamespace}.DataTypes.TopDownValues> TopDownValues => TopDownValuesStatic;");
            }

            // Made a field so the user can assign individual values
            // Made a Vector3 to make assignment and usage easier.
            codeBlock.Line("public Microsoft.Xna.Framework.Vector3 GroundVelocity;");

            WriteAnimationFields(element, codeBlock);

            codeBlock.Line("DataTypes.TopDownValues mCurrentMovement;");
            codeBlock.Line("public float TopDownSpeedMultiplier { get; set; } = 1;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement variables used when applying input.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public DataTypes.TopDownValues", "CurrentMovement")
                .Get()
                    .Line("return mCurrentMovement;").End()
                .Set("private")
                    .Line("mCurrentMovement = value;");


            codeBlock.Property("public FlatRedBall.Input.IInputDevice", "InputDevice")
                .Line("get;")
                .Line("private set;");

            codeBlock.Line("TopDownDirection mDirectionFacing;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Which direciton the character is facing.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public TopDownDirection", "DirectionFacing")
                .Get()
                    .Line("return mDirectionFacing;")
                    .End()
                .Set()
                    .Line("mDirectionFacing = value;");


            codeBlock.Property("public PossibleDirections", "PossibleDirections")
                .AutoGet().End()
                .AutoSet();

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls the horizontal movement of the character.");
            codeBlock.Line("/// Common examples include a d-pad, analog stick, or keyboard keys.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.I2DInput", "MovementInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether input is read to control the movement of the character.");
            codeBlock.Line("/// This can be turned off if the player should not be able to control");
            codeBlock.Line("/// the character.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public bool", "InputEnabled");

            codeBlock.Line("TopDown.DirectionBasedAnimationLayer mTopDownAnimationLayer;");


            codeBlock.Line("#endregion");
        }

        private static void WriteAnimationFields(IElement element, ICodeBlock codeBlock)
        {
            string ToQuotedSetName(string setValue)
            {
                if(setValue == null)
                {
                    return "null";
                }
                else
                {
                    return $"\"{setValue}\"";
                }
            }

            TopDownAnimationData animationData = null;
            var animationFilePath = MainController.GetAnimationFilePathFor(element as EntitySave);
            if (animationFilePath.Exists())
            {
                try
                {
                    var contents = System.IO.File.ReadAllText(animationFilePath.FullPath);
                    animationData = JsonConvert.DeserializeObject<TopDownAnimationData>(contents);
                }
                catch
                {
                    // do nothing, codegen will skip this
                }
            }

            var hasAnimationSets =
                animationData?.Animations.Count > 0;

            if(!hasAnimationSets)
            {
                codeBlock.Line("public List<TopDown.AnimationSet> AnimationSets { get; set; } = new List<TopDown.AnimationSet>();");

            }
            else
            {
                codeBlock.Line("public List<TopDown.AnimationSet> AnimationSets { get; set; } = new List<TopDown.AnimationSet>");

                var listBlock = codeBlock.Block();
                (listBlock.PostCodeLines[0] as CodeLine).Value += ";";

                foreach(var movementValueAnimation in animationData.Animations)
                {

                    string prefix = movementValueAnimation.MovementValuesName + "_";

                    foreach(var set in movementValueAnimation.AnimationSets)
                    {

                        var hasAnimations =
                            set.UpLeftAnimation != null ||
                            set.UpAnimation != null ||
                            set.UpRightAnimation != null ||
                            set.LeftAnimation != null ||
                            set.RightAnimation != null ||
                            set.DownLeftAnimation != null ||
                            set.DownAnimation != null ||
                            set.DownRightAnimation != null;

                        if(hasAnimations)
                        {
                            listBlock.Line($"new TopDown.AnimationSet()");
                            var assignmentBlock = listBlock.Block();
                            (assignmentBlock.PostCodeLines[0] as CodeLine).Value += ",";

                            string minSpeed =
                                set.AnimationSetName == "Idle" ? "0f" : ".1f";

                            assignmentBlock.Line($"MinSpeed = {minSpeed},");

                            assignmentBlock.Line($"MovementValueName = \"{movementValueAnimation.MovementValuesName}\",");



                            assignmentBlock.Line($"UpLeftAnimationName = {ToQuotedSetName(set.UpLeftAnimation)},");
                            assignmentBlock.Line($"UpAnimationName = {ToQuotedSetName(set.UpAnimation)},");
                            assignmentBlock.Line($"UpRightAnimationName = {ToQuotedSetName(set.UpRightAnimation)},");

                            assignmentBlock.Line($"LeftAnimationName = {ToQuotedSetName(set.LeftAnimation)},");
                            assignmentBlock.Line($"RightAnimationName = {ToQuotedSetName(set.RightAnimation)},");

                            assignmentBlock.Line($"DownLeftAnimationName = {ToQuotedSetName(set.DownLeftAnimation)},");
                            assignmentBlock.Line($"DownAnimationName = {ToQuotedSetName(set.DownAnimation)},");
                            assignmentBlock.Line($"DownRightAnimationName = {ToQuotedSetName(set.DownRightAnimation)}");

                        }
                    }
                }
            }

        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            /////////////////Early Out//////////////////////
            if (TopDownEntityPropertyLogic.GetIfIsTopDown(element) == false)
            {
                return codeBlock;
            }
            //////////////End Early Out//////////////////////

            // The platformer plugin sets events here, but we don't need to
            // here on the top-down (yet) since there are no events
            // Update 1 - actually we should prob assign the default movement
            // here if there is one...
            codeBlock.Line("InitializeInput();");

            var ifBlock = codeBlock.If("TopDownValues?.Count > 0");
            {
                ifBlock.Line("mCurrentMovement = TopDownValues.Values.FirstOrDefault();");
            }
            codeBlock.Line("PossibleDirections = PossibleDirections.FourWay;");

            codeBlock.Line("mTopDownAnimationLayer = new TopDown.DirectionBasedAnimationLayer();");
            codeBlock.Line("mTopDownAnimationLayer.TopDownEntity = this;");

            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if (!TopDownEntityPropertyLogic.GetIfIsTopDown(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line(
@"
        #region Top-Down Methods    

        public void InitializeTopDownInput(FlatRedBall.Input.IInputDevice inputDevice)
        {
            this.MovementInput = inputDevice?.Default2DInput;
            this.InputDevice = inputDevice;
            InputEnabled = inputDevice != null;

            CustomInitializeTopDownInput();
        }

        partial void CustomInitializeTopDownInput();

        private void ApplyMovementInput()
        {
            ////////early out/////////
            if(mCurrentMovement == null)
            {
                return;
            }
            //////end early out

            var absoluteVelocity = this.Velocity;
            var groundVelocity = this.Velocity - GroundVelocity;

            var desiredGroundVelocity = Microsoft.Xna.Framework.Vector3.Zero;
            var desiredAbsoluteVelocity = GroundVelocity;

            if (InputEnabled && MovementInput != null)
            {
                var vector = new Microsoft.Xna.Framework.Vector2(MovementInput.X, MovementInput.Y);
                if(vector.LengthSquared() > 1)
                {
                    vector = Microsoft.Xna.Framework.Vector2ExtensionMethods.AtLength(vector, 1);
                }
                desiredGroundVelocity = new Microsoft.Xna.Framework.Vector3(vector.X, vector.Y, absoluteVelocity.Z) * 
                    mCurrentMovement.MaxSpeed * TopDownSpeedMultiplier;
            }
            desiredAbsoluteVelocity = GroundVelocity + desiredGroundVelocity;

            var absoluteDifference =  desiredAbsoluteVelocity - absoluteVelocity;
            var groundDifference = desiredGroundVelocity - groundVelocity;

            Acceleration = Microsoft.Xna.Framework.Vector3.Zero;

            var absoluteDifferenceLength = absoluteDifference.Length();

            const float differenceEpsilon = .1f;

            if (absoluteDifferenceLength > differenceEpsilon)
            {
                var isMovingRelativeToGround = groundVelocity.X != 0 || groundVelocity.Y != 0;
                var isDesiredGroundVelocityNonZero = desiredGroundVelocity.X != 0 || desiredGroundVelocity.Y != 0;

                // A 0 to 1 ratio of acceleration to deceleration, where 1 means the player is accelerating completely,
                // and 0 means decelerating completely. This value will often be between 0 and 1 because the player may
                // set desired velocity perpendicular to the current velocity
                float accelerationRatio = 1;
                if(isMovingRelativeToGround && isDesiredGroundVelocityNonZero == false)
                {
                    // slowing down completely
                    accelerationRatio = 0;
                }
                else if(isMovingRelativeToGround == false && isDesiredGroundVelocityNonZero)
                {
                    accelerationRatio = 1;
                }
                else
                {
                    // both is moving and has a non-zero desired value
                    var movementAngle = (float)Math.Atan2(absoluteVelocity.Y, absoluteVelocity.X);
                    var desiredAngle = (float)Math.Atan2(absoluteDifference.Y, absoluteDifference.X);

                    accelerationRatio = 1-
                        Math.Abs(FlatRedBall.Math.MathFunctions.AngleToAngle(movementAngle, desiredAngle)) / (float)Math.PI;

                }

                var secondsToTake = Microsoft.Xna.Framework.MathHelper.Lerp(
                    mCurrentMovement.DecelerationTime,
                    mCurrentMovement.AccelerationTime,
                    accelerationRatio);

                if(!mCurrentMovement.UsesAcceleration || secondsToTake == 0)
                {
                    this.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                    this.Velocity = desiredAbsoluteVelocity;
                }
                else
                {
                    float accelerationMagnitude;
                    if(this.Velocity.Length() > mCurrentMovement.MaxSpeed * TopDownSpeedMultiplier && mCurrentMovement.IsUsingCustomDeceleration)
                    {
                        accelerationMagnitude = mCurrentMovement.CustomDecelerationValue;
                    }
                    else
                    {
                        // If the TopDownSpeedMultiplier is really small, then this makes acceleration go slower
                        // so it reaches the max speed in the same amount of time. However, if the player is moving
                        // faster than the multiplier, we want to slow down at normal speed rather than multiplied by
                        // TopDownSpeedMulitplier. An extreme case is where TopDownSpeedMultiplier is 0, In that case, the
                        // character would never slow down, which is bad.
                        if(this.Velocity.Length() > mCurrentMovement.MaxSpeed * TopDownSpeedMultiplier)
                        {
                            // This may not give perfect control over deceleration, but that's why CustomDecelerationValue exists
                            accelerationMagnitude = mCurrentMovement.MaxSpeed / secondsToTake;
                        }
                        else
                        {
                            accelerationMagnitude = TopDownSpeedMultiplier * mCurrentMovement.MaxSpeed / secondsToTake;
                        }
                    }
                
                    var nonNormalizedAbsoluteDifference = absoluteDifference;
                    var normalizedAbsoluteDifference = absoluteDifference;
                    normalizedAbsoluteDifference.Normalize();
                
                    var accelerationToSet = accelerationMagnitude * normalizedAbsoluteDifference;
                    var expectedVelocityToAdd = accelerationToSet * TimeManager.SecondDifference;
                
                    if(expectedVelocityToAdd.Length() > nonNormalizedAbsoluteDifference.Length())
                    {
                        // we will overshoot it, so let's adjust the acceleration accordingly:
                        var ratioOfToAdd = nonNormalizedAbsoluteDifference.Length() / expectedVelocityToAdd.Length();
                        this.Acceleration = accelerationToSet * ratioOfToAdd;
                    }
                    else
                        this.Acceleration = accelerationToSet;
                }


                if (mCurrentMovement.UpdateDirectionFromInput)
                {
                    if (desiredGroundVelocity.X != 0 || desiredGroundVelocity.Y != 0)
                    {
                        mDirectionFacing = TopDownDirectionExtensions.FromDirection(desiredGroundVelocity.X, desiredGroundVelocity.Y, PossibleDirections, mDirectionFacing);
                    }
                }
                else if (mCurrentMovement.UpdateDirectionFromVelocity)
                {
                    const float velocityEpsilon = .1f;
                    var shouldAssignDirection = (this.Velocity.Length() - groundVelocity.Length()) > velocityEpsilon || groundDifference.Length() > 0;

                    if(groundVelocity.LengthSquared() == 0)
                    {
                        if(desiredGroundVelocity.X != 0 || desiredGroundVelocity.Y != 0)
                        {
                            // use the desired movement value, so the player can
                            // change directions when facing a wall
                            mDirectionFacing = TopDownDirectionExtensions.FromDirection(desiredGroundVelocity.X, desiredGroundVelocity.Y, PossibleDirections, mDirectionFacing);
                        }
                    }
                    else
                    {
                        mDirectionFacing = TopDownDirectionExtensions.FromDirection(groundVelocity.X, groundVelocity.Y, PossibleDirections, mDirectionFacing);
                    }
                }
            }
            else
            {
                Velocity = desiredAbsoluteVelocity;
                Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
            }

        }

        #endregion

");

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if (!TopDownEntityPropertyLogic.GetIfIsTopDown(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line("ApplyMovementInput();");

            return codeBlock;
        }
    }
}
