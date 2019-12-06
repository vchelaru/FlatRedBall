using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.CodeGenerators
{
    public class AnimationCodeGenerator : Singleton<AnimationCodeGenerator>
    {
        public const string DirectionBasedAnimationLayerFile = "TopDown/DirectionBasedAnimationLayer.Generated.cs";
        public const string AnimationSetFileName = "TopDown/AnimationSet.Generated.cs";

        public void GenerateAndSave()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateDirectionBasedAnimationLayerFile();

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(DirectionBasedAnimationLayerFile);

                var fullPath = GlueState.Self.CurrentGlueProjectDirectory + DirectionBasedAnimationLayerFile;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullPath, contents));

            }, $"Adding {DirectionBasedAnimationLayerFile}");

            TaskManager.Self.Add(() =>
            {
                var contents = GenerateAnimationSetFile();

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(AnimationSetFileName);

                var fullPath = GlueState.Self.CurrentGlueProjectDirectory + AnimationSetFileName;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullPath, contents));

            }, $"Adding {AnimationSetFileName}");
        }

        private string GenerateDirectionBasedAnimationLayerFile()
        {
            var toReturn = $@"
using System.Linq;

namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public class DirectionBasedAnimationLayer : FlatRedBall.Graphics.Animation.AnimationLayer
    {{
        public string LastAnimation {{ get; set; }}
        public TopDown.ITopDownEntity TopDownEntity {{ get; set; }}

        public DirectionBasedAnimationLayer() 
        {{
            this.EveryFrameAction = DoAnimationLogic;
        }}

        private string DoAnimationLogic()
        {{
            var currentSpeed = System.Math.Sqrt(TopDownEntity.XVelocity * TopDownEntity.XVelocity + TopDownEntity.YVelocity * TopDownEntity.YVelocity);

            AnimationSet setToUse = null;
            var animationSets = TopDownEntity.AnimationSets;

            for(int i = animationSets.Count - 1; i > -1; i--)
            {{
                var possibleSet = animationSets[i];
                if(possibleSet.MovementValueName == TopDownEntity.CurrentMovement?.Name && possibleSet.MinSpeed <= currentSpeed)
                {{
                    setToUse = possibleSet;
                    break;
                }}
            }}

            if(setToUse == null)
            {{
                for (int i = animationSets.Count - 1; i > -1; i--)
                {{
                    var possibleSet = animationSets[i];
                    if (possibleSet.MovementValueName == ""Base Animations"" && possibleSet.MinSpeed <= currentSpeed)
                    {{
                        setToUse = possibleSet;
                        break;
                    }}
                }}
            }}


            if(setToUse == null)
            {{
                return null;
            }}
            else
            {{
                string currentAnimation = null;
                switch(TopDownEntity.DirectionFacing)
                {{
                    case Entities.TopDownDirection.UpLeft:
                        currentAnimation = setToUse.UpLeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Up:
                        currentAnimation = setToUse.UpAnimationName;
                        break;
                    case Entities.TopDownDirection.UpRight:
                        currentAnimation = setToUse.UpRightAnimationName;
                        break;

                    case Entities.TopDownDirection.Left:
                        currentAnimation = setToUse.LeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Right:
                        currentAnimation = setToUse.RightAnimationName;
                        break;

                    case Entities.TopDownDirection.DownLeft:
                        currentAnimation = setToUse.DownLeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Down:
                        currentAnimation = setToUse.DownAnimationName;
                        break;
                    case Entities.TopDownDirection.DownRight:
                        currentAnimation = setToUse.DownRightAnimationName;
                        break;
                }}
                currentAnimation = currentAnimation ?? LastAnimation;

                LastAnimation = currentAnimation;

                return currentAnimation;
            }}
        }}
    }}
}}
";

            return toReturn;
        }

        private string GenerateAnimationSetFile()
        {
            var toReturn = $@"

namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public class AnimationSet
    {{
        public float MinSpeed {{ get; set; }}
        public string MovementValueName {{ get; set; }}

        public string UpLeftAnimationName {{ get; set; }}
        public string UpAnimationName {{ get; set; }}
        public string UpRightAnimationName {{ get; set; }}

        public string LeftAnimationName {{ get; set; }}
        public string RightAnimationName {{ get; set; }}

        public string DownLeftAnimationName {{ get; set; }}
        public string DownAnimationName {{ get; set; }}
        public string DownRightAnimationName {{ get; set; }}
    }}
}}
";
            return toReturn;
        }

    }





}
