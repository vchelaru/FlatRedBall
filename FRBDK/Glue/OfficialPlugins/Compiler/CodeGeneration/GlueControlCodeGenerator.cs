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
    public class GlueControlCodeGenerator
    {


        public static string GetGlueControlManagerContents(bool generateFull)
        {
            var embeddedLocation = "OfficialPlugins.Compiler.Embedded.GlueControlManager.cs";

            string asString = GetEmbeddedStringContents(generateFull, embeddedLocation);

            return asString;
        }

        public static string GetEditingManagerContents(bool generateFull)
        {
            var embeddedLocation = "OfficialPlugins.Compiler.Embedded.Editing.EditingManager.cs";

            string asString = GetEmbeddedStringContents(generateFull, embeddedLocation);

            return asString;
        }

        private static string GetEmbeddedStringContents(bool generateFull, string embeddedLocation)
        {
            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(
                typeof(GlueControlCodeGenerator).Assembly,
                embeddedLocation);

            var asString = System.Text.Encoding.UTF8.GetString(byteArray);

            asString = asString.Replace("{ProjectNamespace}", GlueState.Self.ProjectNamespace);

            var compilerDirectives = String.Empty;
            if (generateFull)
            {
                compilerDirectives += "#define IncludeSetVariable\r\n";
            }
            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode)
            {
                compilerDirectives += "#define SupportsEditMode\r\n";
            }
            asString = asString.Replace("{CompilerDirectives}", compilerDirectives);
            return asString;
        }
    }
}
