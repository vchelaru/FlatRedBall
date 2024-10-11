using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.EffectPlugin.Data;
using OfficialPlugins.EffectPlugin.ViewModels;

namespace OfficialPlugins.EffectPlugin.CodeGenerators;

internal class PostProcessCodeGenerator
{
    internal static void ReplaceContents(string rfsName, ShaderContents shaderContents)
    {
        string newDirectory = Path.Combine("Graphics", Path.GetDirectoryName(rfsName) ?? "");
        string newFileName = Path.GetFileName(Path.ChangeExtension(rfsName, "cs") ?? "");
        string newFileNameOnly = Path.GetFileNameWithoutExtension(Path.GetFileName(Path.ChangeExtension(rfsName, "cs")) ?? "");
        string newFileRelativePath = Path.Combine(newDirectory, newFileName);
        FilePath destinationFile = GlueState.Self.CurrentGlueProjectDirectory + newFileRelativePath;

        var assemblyContainingResource = typeof(PostProcessCodeGenerator).Assembly;

        var resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.PostProcessTemplate.cs";

        using var stream =
                assemblyContainingResource.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            string fileContent = reader.ReadToEnd();

            fileContent = fileContent.Replace("ReplaceNamespace", $"{GlueState.Self.ProjectNamespace}.{newDirectory.Replace('\\', '.')}");

            if (newFileNameOnly.Length < 1)
            {
                throw new ArgumentException("FX file name must have at least 1 character");
            }

            char firstLetter = char.ToUpper(newFileNameOnly[0]);
            fileContent = fileContent.Replace("ReplaceClassName", firstLetter + newFileNameOnly[1..]);

            fileContent = fileContent.Replace("ReplaceClassMembers", shaderContents.ClassMembers);
            fileContent = fileContent.Replace("ReplaceApplyBody", shaderContents.ApplyBody);

            var directory = destinationFile.GetDirectoryContainingThis();
            if (!System.IO.Directory.Exists(directory.FullPath))
            {
                System.IO.Directory.CreateDirectory(directory.FullPath);
            }

            System.IO.File.WriteAllText(destinationFile.FullPath, fileContent);

            GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(destinationFile.FullPath);
        }
    }
}
