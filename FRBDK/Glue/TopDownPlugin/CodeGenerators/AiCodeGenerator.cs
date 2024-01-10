using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace TopDownPlugin.CodeGenerators
{
    public class AiCodeGenerator : FullFileCodeGenerator
    {
        static AiCodeGenerator mSelf;
        public static AiCodeGenerator Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new AiCodeGenerator();
                }
                return mSelf;
            }
        }

        public override string RelativeFile =>
            GlueState.Self.CurrentGlueProject?.FileVersion > (int)GluxVersions.PreVersion
            ? "TopDown/TopDownAiInput.Generated.cs"
            : "TopDown/TopDownAiInput.cs"
            ;

        protected override string GenerateFileContents()
        {
            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(
                typeof(AiCodeGenerator).Assembly,
                "TopDownPluginCore.Embedded.TopDownAiInput.Generated.cs");

            string toReturn = System.Text.Encoding.UTF8.GetString(byteArray);

            toReturn = toReturn.Replace("$NAMESPACE$", GlueState.Self.ProjectNamespace);
            toReturn = GlueCommands.Self.GenerateCodeCommands.ReplaceGlueVersionString(toReturn);
            return toReturn;
        }
    }
}
