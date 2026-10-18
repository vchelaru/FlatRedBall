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

            compilerDirectives += MakeSupportsEditModeDebugOnly(CodeBuildItemAdder.GetGlueVersionsString());

            var hasGumResponse = PluginManager.CallPluginMethod("Gum Plugin", "HasGum");
            var asBool = hasGumResponse as bool?;
            if (asBool == true)
            {
                compilerDirectives += "#define HasGum\r\n";
            }


            compilerDirectives += $"using {GlueState.Self.ProjectNamespace};\r\n";

            var isEditModeOnly = IsEditModeOnly(embeddedLocation);
            if (isEditModeOnly)
            {
                // Directives (#define, using) are fine ahead of this; the whole body is what Release drops.
                compilerDirectives += "#if DEBUG\r\n";
            }

            if(asString.Contains("{CompilerDirectives}"))
            {
                asString = asString.Replace("{CompilerDirectives}", compilerDirectives);

            }
            else
            {
                // still put it in, in case it got wiped out by a copy/paste
                asString = compilerDirectives + "\n" + asString;
            }

            if (isEditModeOnly)
            {
                asString = asString.TrimEnd() + "\r\n#endif\r\n";
            }
            return asString.Trim(new char[] { '\uFEFF', '\u200B' });
        }

        const string EmbeddedPrefix = "GameCommunicationPlugin.GlueControl.Embedded.";

        // The files that stay compiled in Release: the game's side of the Glue connection and the profiling
        // data it answers with (#2371). Every one of them keeps its edit mode parts behind `#if DEBUG` or
        // `#if SupportsEditMode`. The models are the plain data the DTOs are built from; the extension
        // methods next to them call into the edit mode managers, so they are not among them.
        static readonly HashSet<string> ReleaseFiles = new HashSet<string>
        {
            "CommandReceiver.cs",
            "Dtos.cs",
            "GameConnectionManager.cs",
            "GlueControlManager.cs",
            "Models.CustomVariable.cs",
            "Models.GlueElement.cs",
            "Models.GlueElementFileReference.cs",
            "Models.GlueProjectSave.cs",
            "Models.IElementExtensionMethods.cs",
            "Models.INamedObjectContainer.cs",
            "Models.NamedObjectSave.cs",
            "Models.ReferencedFileSave.cs",
            "Models.StateSave.cs",
            "Models.StateSaveCategory.cs",
        };

        /// <summary>
        /// True for an embedded file that only exists to make live edit work - selection, dragging, variable
        /// overlay, instance creation - and so is compiled into Debug builds only. Those files are mostly
        /// reflection by name and MakeGenericType, which Native AOT cannot trim or compile cleanly, and a
        /// shipped game has no use for them.
        /// </summary>
        public static bool IsEditModeOnly(string embeddedLocation)
        {
            if (!embeddedLocation.StartsWith(EmbeddedPrefix))
            {
                return false;
            }
            var fileName = embeddedLocation.Substring(EmbeddedPrefix.Length);
            return !ReleaseFiles.Contains(fileName);
        }

        /// <summary>
        /// Glue emits a `#define` for every GluxVersions member the project has reached, and `SupportsEditMode`
        /// is one of them. In the embedded files it stands for "edit mode code is compiled in", so it is only
        /// defined in Debug builds.
        /// </summary>
        public static string MakeSupportsEditModeDebugOnly(string versionDefines)
        {
            const string define = "#define SupportsEditMode\n";
            return versionDefines.Replace(define, "#if DEBUG\n" + define + "#endif\n");
        }
    }
}
