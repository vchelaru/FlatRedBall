using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.CodeGenerators
{
    public class CollisionHistoryCodeGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "DataTypes/CollisionHistory.Generated.cs";

        static CollisionHistoryCodeGenerator mSelf;
        public static CollisionHistoryCodeGenerator Self => mSelf ??= new CollisionHistoryCodeGenerator();

        protected override string GenerateFileContents()
        {
            string projectNamespace = GlueState.Self.ProjectNamespace + ".DataTypes";

            var toReturn = @"
namespace " + projectNamespace + @"
{
    public class CollisionHistory
    {
        public double LastReverseSendingCollisionTime { get; set; }
 
        public double LastBrakePressedTime { get; set; }
 
        public double LastTimeMovingForward { get; set; }
 
        public bool GetIfShouldApplyForwardTurningControls()
        {
            if(LastReverseSendingCollisionTime == 0)
            {
                return false;
            }
            else
            {
                bool hasUserPressedBrakeSinceCollision =
                    LastBrakePressedTime > LastReverseSendingCollisionTime;
 
                bool hasCarMovedForwardSinceCollision =
                    LastTimeMovingForward > LastReverseSendingCollisionTime;
 
                return hasUserPressedBrakeSinceCollision == false &&
                    hasCarMovedForwardSinceCollision == false;
            }
        }
    }
}
";
            return toReturn;
        }
    }
}
