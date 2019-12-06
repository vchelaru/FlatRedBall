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
    public class InterfacesFileGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "TopDown/Interfaces.Generated.cs";

        static InterfacesFileGenerator mSelf;
        public static InterfacesFileGenerator Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new InterfacesFileGenerator();
                }
                return mSelf;
            }
        }

        protected override string GenerateFileContents()
        {
            var toReturn = $@"


namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public interface ITopDownEntity
    {{
        DataTypes.TopDownValues CurrentMovement {{ get; }}
        Entities.TopDownDirection DirectionFacing {{ get; }}
        System.Collections.Generic.List<TopDown.AnimationSet> AnimationSets {{ get; }}
        
        float XVelocity {{ get; set; }}
        float YVelocity {{ get; set; }}
    }}
}}
";
            return toReturn;
        }
    }
}
