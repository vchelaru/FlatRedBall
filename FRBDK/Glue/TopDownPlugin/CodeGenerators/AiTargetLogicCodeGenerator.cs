using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownPlugin.CodeGenerators
{
    public class AiTargetLogicCodeGenerator : FullFileCodeGenerator
    {
        static AiTargetLogicCodeGenerator mSelf;
        public static AiTargetLogicCodeGenerator Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new AiTargetLogicCodeGenerator();
                }
                return mSelf;
            }
        }

        public override string RelativeFile => "TopDown/TopDownAiTargetLogic.Generated.cs";

        protected override string GenerateFileContents()
        {
            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(
                typeof(AiTargetLogicCodeGenerator).Assembly,
                "TopDownPlugin.Embedded.TopDownAiTargetLogic.Generated.cs");

            string toReturn = System.Text.Encoding.UTF8.GetString(byteArray);

            toReturn = toReturn.Replace("$NAMESPACE$", GlueState.Self.ProjectNamespace);
            toReturn = GlueCommands.Self.GenerateCodeCommands.ReplaceGlueVersionString(toReturn);
            return toReturn;
        }
    }
}
