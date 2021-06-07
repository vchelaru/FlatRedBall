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

        public static string GetGlueControlManagerContents()
        {
            return GetEmbeddedStringContents( 
                "OfficialPlugins.Compiler.Embedded.GlueControlManager.cs");
        }

        public static string GetEditingManagerContents()
        {
            return GetEmbeddedStringContents( 
                "OfficialPlugins.Compiler.Embedded.Editing.EditingManager.cs");
        }

        public static string GetSelectionLogicContents()
        {
            return GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Editing.SelectionLogic.cs");
        }

        public static string GetSelectionMarkerContents()
        {
            return GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Editing.SelectionMarker.cs");
        }

        internal static string GetDtosContents()
        {
            return GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Dtos.cs");
        }

        internal static string GetInstanceLogicContents()
        {

            return GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.InstanceLogic.cs");
        }

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
            asString = asString.Replace("{CompilerDirectives}", compilerDirectives);
            return asString;
        }
    }
}
