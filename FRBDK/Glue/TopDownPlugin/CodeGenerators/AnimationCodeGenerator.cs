using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public class DirectionBasedAnimationLayer : FlatRedBall.Graphics.Animation.AnimationLayer
    {{
        public AnimationSet CurrentAnimationSet {{ get; set; }}
        public string LastAnimation {{ get; set; }}
        public Entities.TopDownDirection CurrentDirection {{ get; set; }}

        public DirectionBasedAnimationLayer() 
        {{
            this.EveryFrameAction = DoAnimationLogic;
        }}

        private string DoAnimationLogic()
        {{
            if(CurrentAnimationSet == null)
            {{
                return null;
            }}
            else
            {{
                string currentAnimation = null;
                switch(CurrentDirection)
                {{
                    case Entities.TopDownDirection.UpLeft:
                        currentAnimation = CurrentAnimationSet.UpLeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Up:
                        currentAnimation = CurrentAnimationSet.UpAnimationName;
                        break;
                    case Entities.TopDownDirection.UpRight:
                        currentAnimation = CurrentAnimationSet.UpRightAnimationName;
                        break;

                    case Entities.TopDownDirection.Left:
                        currentAnimation = CurrentAnimationSet.LeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Right:
                        currentAnimation = CurrentAnimationSet.RightAnimationName;
                        break;

                    case Entities.TopDownDirection.DownLeft:
                        currentAnimation = CurrentAnimationSet.DownLeftAnimationName;
                        break;
                    case Entities.TopDownDirection.Down:
                        currentAnimation = CurrentAnimationSet.DownAnimationName;
                        break;
                    case Entities.TopDownDirection.DownRight:
                        currentAnimation = CurrentAnimationSet.DownRightAnimationName;
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
