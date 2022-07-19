using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    public static class EmbeddedCodeManager
    {
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueCommunication/";

        public static void Embed(List<string> files)
        {
            foreach(var file in files)
            {
                SaveEmbeddedFile(file);
            }
        }

        private static void RemoveEmbeddedFile(string relativePath)
        {
            FilePath absoluteFile = GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/" + relativePath;

            GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteFile);
        }

        private static void SaveEmbeddedFile(string resourcePath)
        {
            var split = resourcePath.Split(".").ToArray();
            split = split.Take(split.Length - 1).ToArray(); // take off the .cs
            var combined = string.Join('/', split) + ".Generated.cs";
            var relativeDestinationFilePath = combined;

            var prefix = "GameCommunicationPlugin.Embedded.";
            string glueControlManagerCode = GetEmbeddedStringContents(prefix + resourcePath);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }

        private static string GetEmbeddedStringContents(string embeddedLocation)
        {
            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(
                typeof(EmbeddedCodeManager).Assembly,
                embeddedLocation);

            var asString = System.Text.Encoding.UTF8.GetString(byteArray);

            asString = asString.Replace("{ProjectNamespace}", GlueState.Self.ProjectNamespace);

            var compilerDirectives = String.Empty;
            if (true)
            {
                compilerDirectives += "#define IncludeSetVariable\r\n";
            }

            var fileVersion =
                GlueState.Self.CurrentGlueProject.FileVersion;

            if (fileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode)
            {
                compilerDirectives += "#define SupportsEditMode\r\n";
            }
            if (fileVersion >= (int)GlueProjectSave.GluxVersions.ScreenManagerHasPersistentPolygons)
            {
                compilerDirectives += "#define ScreenManagerHasPersistentPolygons\r\n";
            }
            if (fileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasTolerateMissingAnimations)
            {
                compilerDirectives += "#define SpriteHasTolerateMissingAnimations\r\n";
            }

            var hasGumResponse = PluginManager.CallPluginMethod("Gum Plugin", "HasGum");
            var asBool = hasGumResponse as bool?;
            if (asBool == true)
            {
                compilerDirectives += "#define HasGum\r\n";
            }


            compilerDirectives += $"using {GlueState.Self.ProjectNamespace};\r\n";

            if (asString.Contains("{CompilerDirectives}"))
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
