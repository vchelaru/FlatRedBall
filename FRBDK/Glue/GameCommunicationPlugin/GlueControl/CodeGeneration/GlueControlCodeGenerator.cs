using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration
{
    public static class GlueControlCodeGenerator
    {
        public static bool GenerateFull { get; set; }

        public static string GetEmbeddedStringContents(string embeddedLocation)
        {
            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(
                typeof(GlueControlCodeGenerator).Assembly,
                embeddedLocation);

            var asString = System.Text.Encoding.UTF8.GetString(byteArray);

            asString = asString.Replace("{ProjectNamespace}", GlueState.Self.ProjectNamespace);

            var compilerDirectives = String.Empty;
            if (GenerateFull)
            {
                compilerDirectives += "#define IncludeSetVariable\r\n";
            }

            compilerDirectives += CodeBuildItemAdder.GetGlueVersionsString();

            var hasGumResponse = PluginManager.CallPluginMethod("Gum Plugin", "HasGum");
            var asBool = hasGumResponse as bool?;
            if (asBool == true)
            {
                compilerDirectives += "#define HasGum\r\n";
            }


            compilerDirectives += $"using {GlueState.Self.ProjectNamespace};\r\n";

            if(asString.Contains("{CompilerDirectives}"))
            {
                asString = asString.Replace("{CompilerDirectives}", compilerDirectives);

            }
            else
            {
                // still put it in, in case it got wiped out by a copy/paste
                asString = compilerDirectives + "\n" + asString;
            }
            return asString;
        }
    }
}
