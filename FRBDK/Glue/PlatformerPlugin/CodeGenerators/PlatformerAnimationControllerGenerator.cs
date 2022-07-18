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


namespace " + GlueState.Self.ProjectNamespace + @".Entities
{
    public enum AnimationSpeedAssignment
    {
        ForceTo1,
        NoAssignment,
        BasedOnMultiplier
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

        public bool? OnGroundRequirement { get; set; }

        public string MovementName { get; set; }

        public AnimationSpeedAssignment AnimationSpeedAssignment { get; set; }

        public override string ToString()
        {
            return AnimationName;
        }
    }

    public class PlatformerAnimationController : FlatRedBall.Graphics.Animation.AnimationController
    {
        IPlatformer PlatformerEntity;
        public PlatformerAnimationController(IPlatformer platformerEntity)
        {
            PlatformerEntity = platformerEntity;
        }


        public void AddLayer(PlatformerAnimationConfiguration configuration)
        {
            var layer = this.AddLayer();
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

                if (shouldSet)
                {
                    switch(configuration.AnimationSpeedAssignment)
                    {
                        case AnimationSpeedAssignment.ForceTo1:
                            {
                                var asSprite = this.AnimatedObject as FlatRedBall.Sprite;

                                asSprite.AnimationSpeed = 1;
                            }
                            break;
                        case AnimationSpeedAssignment.NoAssignment:
                            break;
                        case AnimationSpeedAssignment.BasedOnMultiplier:
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
