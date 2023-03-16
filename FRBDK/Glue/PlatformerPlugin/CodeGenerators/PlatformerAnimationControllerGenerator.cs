using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class PlatformerAnimationControllerGenerator : Singleton<PlatformerAnimationControllerGenerator>
    {
        string RelativeFileLocation => "Platformer/PlatformerAnimationController.Generated.cs";


        public void GenerateAndSave()
        {

            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = RelativeFileLocation;

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var glueProjectDirectory = GlueState.Self.CurrentGlueProjectDirectory;

                if (!string.IsNullOrEmpty(glueProjectDirectory))
                {
                    var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                    try
                    {
                        GlueCommands.Self.TryMultipleTimes(() =>
                            System.IO.File.WriteAllText(fullFile, contents));
                    }
                    catch (Exception e)
                    {
                        GlueCommands.Self.PrintError(e.ToString());
                    }
                }

            }, "Adding PlatformerAnimationConfiguration.Generated.cs to the project");


        }

        private string GenerateFileContents()
        {
            var toReturn =
@"
using System.Linq;

namespace " + GlueState.Self.ProjectNamespace + @".Entities
{
    public enum AnimationSpeedAssignment
    {
        ForceTo1,
        NoAssignment,
        BasedOnVelocityMultiplier,
        BasedOnMaxSpeedRatioMultiplier
    }

    public class PlatformerAnimationConfiguration
    {
        public string AnimationName { get; set; }
        public bool HasLeftAndRight { get; set; } = true;

        public float? MinXVelocityAbsolute { get; set; }
        public float? MaxXVelocityAbsolute { get; set; }

        public float? MinYVelocity { get; set; }
        public float? MaxYVelocity { get; set; }

        public float? AbsoluteXVelocityAnimationSpeedMultiplier { get; set; }
        public float? AbsoluteYVelocityAnimationSpeedMultiplier { get; set; }

        public float? MaxSpeedXRatioMultiplier { get; set; }
        public float? MaxSpeedYRatioMultiplier { get; set; }

        public bool? OnGroundRequirement { get; set; }

        public string MovementName { get; set; }

        public AnimationSpeedAssignment AnimationSpeedAssignment { get; set; }

        public System.Func<bool> AdditionalPredicate;

        public override string ToString()
        {
            return AnimationName;
        }
    }

    public class PlatformerAnimationController : FlatRedBall.Graphics.Animation.AnimationController
    {
        IPlatformer PlatformerEntity;

        public bool IsActive { get; set; } = true;

        System.Collections.Generic.List<PlatformerAnimationConfiguration> platformerAnimationConfigurations;
        public System.Collections.ObjectModel.ReadOnlyCollection<PlatformerAnimationConfiguration> Configurations { get; private set; }

        public PlatformerAnimationController(IPlatformer platformerEntity)
        {
            PlatformerEntity = platformerEntity;
            platformerAnimationConfigurations = new System.Collections.Generic.List<PlatformerAnimationConfiguration>();
            Configurations = new System.Collections.ObjectModel.ReadOnlyCollection<PlatformerAnimationConfiguration>(platformerAnimationConfigurations);
        }

        public PlatformerAnimationConfiguration GetConfiguration(string animationName) =>
                    Configurations.First(item => item.AnimationName == animationName);

        public void AddLayer(PlatformerAnimationConfiguration configuration)
        {
            var layer = new FlatRedBall.Graphics.Animation.AnimationLayer();
            this.Layers.Add(layer);

            platformerAnimationConfigurations.Add(configuration);

";

            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AnimationLayerHasName)
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
                if(!IsActive)
                {
                    return null;
                }
                bool shouldSet = true;
                var absoluteXVelocity = System.Math.Abs(PlatformerEntity.XVelocity);
                var yVelocity = PlatformerEntity.YVelocity;
                if(shouldSet && !string.IsNullOrEmpty( configuration.MovementName))
                {
                    shouldSet = configuration.MovementName == PlatformerEntity.CurrentMovementName;
                }

                shouldSet = shouldSet && (absoluteXVelocity < configuration.MinXVelocityAbsolute) == false;
                shouldSet = shouldSet && (yVelocity < configuration.MinYVelocity) == false;
                shouldSet = shouldSet && (absoluteXVelocity > configuration.MaxXVelocityAbsolute) == false;
                shouldSet = shouldSet && (yVelocity > configuration.MaxYVelocity) == false;

                shouldSet = shouldSet &&
                    (configuration.OnGroundRequirement == null || PlatformerEntity.IsOnGround == configuration.OnGroundRequirement);

                if(shouldSet && configuration.AdditionalPredicate != null)
                {
                    shouldSet = configuration.AdditionalPredicate();
                }

                if (shouldSet)
                {
                    switch(configuration.AnimationSpeedAssignment)
                    {
                        case AnimationSpeedAssignment.ForceTo1:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;

                                if(asSprite != null)
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

                                if(configuration.AbsoluteXVelocityAnimationSpeedMultiplier != null)
                                {
                                    asSprite.AnimationSpeed = configuration.AbsoluteXVelocityAnimationSpeedMultiplier.Value *
                                        absoluteXVelocity;
                                }
                                else if(configuration.AbsoluteYVelocityAnimationSpeedMultiplier != null)
                                {
                                    asSprite.AnimationSpeed = configuration.AbsoluteYVelocityAnimationSpeedMultiplier.Value * 
                                        System.Math.Abs(yVelocity);
                                }
                            }
                            break;
                        case AnimationSpeedAssignment.BasedOnMaxSpeedRatioMultiplier:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;
                                if(asSprite != null)
                                {
                                    if(configuration.MaxSpeedXRatioMultiplier != null)
                                    {
                                        if(PlatformerEntity.MaxAbsoluteXVelocity == 0)
                                        {
                                            asSprite.AnimationSpeed = 1;
                                        }
                                        else
                                        {
                                            asSprite.AnimationSpeed = configuration.MaxSpeedXRatioMultiplier.Value * absoluteXVelocity / PlatformerEntity.MaxAbsoluteXVelocity;
                                        }
                                    }
                                    else if(configuration.MaxSpeedYRatioMultiplier != null)
                                    {
                                        if (PlatformerEntity.MaxAbsoluteYVelocity == 0)
                                        {
                                            asSprite.AnimationSpeed = 1;
                                        }
                                        else
                                        {
                                            asSprite.AnimationSpeed = configuration.MaxSpeedYRatioMultiplier.Value * System.Math.Abs(yVelocity) / PlatformerEntity.MaxAbsoluteYVelocity;
                                        }
                                    }
                                }
                            }
                            break;
                    }


                    var toReturn = configuration.AnimationName;
                    if(configuration.HasLeftAndRight)
                    {
                        if(PlatformerEntity.DirectionFacing == HorizontalDirection.Left)
                        {
                            toReturn += ""Left"";
                        }
                        else
                        {
                            toReturn += ""Right"";
                        }
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
}
