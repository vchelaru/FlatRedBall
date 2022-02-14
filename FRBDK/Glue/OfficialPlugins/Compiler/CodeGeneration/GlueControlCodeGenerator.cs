using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.Compiler.CodeGeneration
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
            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode)
            {
                compilerDirectives += "#define SupportsEditMode\r\n";
            }

            var hasGumResponse = PluginManager.CallPluginMethod("Gum Plugin", "HasGum");
            var asBool = hasGumResponse as bool?;
            if (asBool == true)
            {
                compilerDirectives += "#define HasGum\r\n";
            }


            compilerDirectives += $"using {GlueState.Self.ProjectNamespace};\r\n";

            asString = asString.Replace("{CompilerDirectives}", compilerDirectives);
            return asString;
        }
    }
}
