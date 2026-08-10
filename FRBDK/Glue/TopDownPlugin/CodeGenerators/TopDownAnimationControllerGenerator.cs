using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.CodeGenerators;

public class TopDownAnimationControllerGenerator : FullFileCodeGenerator
{
    public override string RelativeFile => "TopDown/TopDownAnimationControllerGenerator.Generated.cs";

    static TopDownAnimationControllerGenerator mSelf;
    public static TopDownAnimationControllerGenerator Self =>
        mSelf ??= new TopDownAnimationControllerGenerator();

    protected override string GenerateFileContents()
    {
        var toReturn =
@"
using System.Linq;

namespace " + GlueState.Self.ProjectNamespace + @".TopDown
{
    public enum AnimationSpeedAssignment
    {
        ForceTo1,
        NoAssignment,
        BasedOnVelocityMultiplier,
        BasedOnMaxSpeedRatioMultiplier,
        BasedOnInputMultiplier
    }

    public class TopDownAnimationConfiguration
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

        public AnimationSpeedAssignment AnimationSpeedAssignment { get; set; }

        public System.Func<bool> AdditionalPredicate;

        public override string ToString()
        {
            return AnimationName;
        }
    }

    public class TopDownAnimationController : FlatRedBall.Graphics.Animation.AnimationController
    {
        ITopDownEntity TopDownEntity;

        public bool IsActive { get; set; } = true;

        System.Collections.Generic.List<TopDownAnimationConfiguration> topDownAnimationConfigurations;
        public System.Collections.ObjectModel.ReadOnlyCollection<TopDownAnimationConfiguration> Configurations { get; private set; }

        public TopDownAnimationController(ITopDownEntity topDownEntity)
        {
            TopDownEntity = topDownEntity;
            topDownAnimationConfigurations = 
                new System.Collections.Generic.List<TopDownAnimationConfiguration>();
            Configurations = new System.Collections.ObjectModel.ReadOnlyCollection<TopDownAnimationConfiguration>(topDownAnimationConfigurations);
        }

        public TopDownAnimationConfiguration GetConfiguration(string animationName) =>
            topDownAnimationConfigurations.First(item => item.AnimationName == animationName);

        public void AddLayer(TopDownAnimationConfiguration configuration)
        {
            var layer = new FlatRedBall.Graphics.Animation.AnimationLayer();
            this.Layers.Add(layer);
            topDownAnimationConfigurations.Add(configuration);
";

        if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AnimationLayerHasName)
        {
            toReturn +=
                @"
            layer.Name = configuration.AnimationName;
";

        }

        toReturn +=
            @"

            layer.EveryFrameAction = () =>
            {
                if (!IsActive)
                {
                    return null;
                }

                bool shouldSet = true;

                Microsoft.Xna.Framework.Vector3 velocityRelativeToGround;
                velocityRelativeToGround.X = TopDownEntity.XVelocity - TopDownEntity.GroundXVelocity;
                velocityRelativeToGround.Y = TopDownEntity.YVelocity - TopDownEntity.GroundYVelocity;
                velocityRelativeToGround.Z = 0;

                var absoluteVelocity = (float)System.Math.Sqrt(velocityRelativeToGround.X * velocityRelativeToGround.X + velocityRelativeToGround.Y * velocityRelativeToGround.Y);

                var absoluteInput = TopDownEntity.MovementInput?.Magnitude ?? 0;

                if(shouldSet && !string.IsNullOrEmpty(configuration.MovementName))
                {
                    shouldSet = configuration.MovementName == TopDownEntity.CurrentMovement?.Name;
                }

                shouldSet = shouldSet && (absoluteVelocity < configuration.MinVelocityAbsolute) == false;
                shouldSet = shouldSet && (absoluteVelocity > configuration.MaxVelocityAbsolute) == false;

                shouldSet = shouldSet && (absoluteInput < configuration.MinMovementInputAbsolute) == false;
                shouldSet = shouldSet && (absoluteInput > configuration.MaxMovementInputAbsolute) == false;

                if (shouldSet && configuration.AdditionalPredicate != null)
                {
                    shouldSet = configuration.AdditionalPredicate();
                }


                if (shouldSet)
                {
                    switch (configuration.AnimationSpeedAssignment)
                    {
                        case AnimationSpeedAssignment.ForceTo1:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;

                                if (asSprite != null)
                                {
                                    asSprite.AnimationSpeed = 1;
                                }
                            }
                            break;
                        case AnimationSpeedAssignment.NoAssignment:
                            break;
                        case AnimationSpeedAssignment.BasedOnVelocityMultiplier:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;

                                if (configuration.AbsoluteVelocityAnimationSpeedMultiplier != null)
                                {
                                    asSprite.AnimationSpeed = configuration.AbsoluteVelocityAnimationSpeedMultiplier.Value *
                                        absoluteVelocity;
                                }
                            }
                            break;
                        case AnimationSpeedAssignment.BasedOnMaxSpeedRatioMultiplier:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;
                                if (asSprite != null)
                                {
                                    if (configuration.MaxSpeedRatioMultiplier != null)
                                    {
                                        if (TopDownEntity.MaxSpeed == 0)
                                        {
                                            asSprite.AnimationSpeed = 1;
                                        }
                                        else
                                        {
                                            asSprite.AnimationSpeed = configuration.MaxSpeedRatioMultiplier.Value * absoluteVelocity / TopDownEntity.MaxSpeed;
                                        }
                                    }
                                }
                            }
                            break;
                        case AnimationSpeedAssignment.BasedOnInputMultiplier:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;
                                if (asSprite != null)
                                {
                                    asSprite.AnimationSpeed = TopDownEntity.MovementInput.Magnitude;
                                }
                            }
                            break;
                    }


                    var toReturn = configuration.AnimationName;
                    if (configuration.IsDirectionFacingAppended)
                    {
                        toReturn += TopDownEntity.DirectionFacing.ToString();
                    }
                    return toReturn;
                }

                return null;
            };
        }
    }
}


";
        return toReturn;
    }

}
