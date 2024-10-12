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
        if (newFileNameOnly.Length < 1)
        {
            throw new ArgumentException("FX file name must have at least 1 character");
        }
        string newFileRelativePath = Path.Combine(newDirectory, newFileName);
        FilePath destinationFile = GlueState.Self.CurrentGlueProjectDirectory + newFileRelativePath;

        var assemblyContainingResource = typeof(PostProcessCodeGenerator).Assembly;

        var resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.PostProcessTemplate.cs";


        using var stream =
                assemblyContainingResource.GetManifestResourceStream(resourceName);

        if (stream != null)
        {
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                var newNamespace =
                    $"{GlueState.Self.ProjectNamespace}.{newDirectory.Replace('\\', '.')}";

                char firstLetter = char.ToUpper(newFileNameOnly[0]);
                var newClassName = firstLetter + newFileNameOnly[1..];

                string fileContents = null;
                if (shaderContents.EntireCsFileContents != null)
                {
                    fileContents = shaderContents.EntireCsFileContents;
                    if (fileContents.Contains("ReplaceNamespace"))
                    {
                        fileContents = fileContents.Replace("ReplaceNamespace", newNamespace);
                    }
                    fileContents = fileContents.Replace("ReplaceClassName", newClassName);

                }
                else
                {
                    using var reader = new StreamReader(stream);
                    fileContents = reader.ReadToEnd();

                    fileContents = fileContents.Replace("ReplaceNamespace", newNamespace);

                    fileContents = fileContents.Replace("ReplaceClassName", newClassName);

                    fileContents = fileContents.Replace("ReplaceClassMembers", shaderContents.ClassMembers);
                    fileContents = fileContents.Replace("ReplaceApplyBody", shaderContents.ApplyBody);

                }

                var directory = destinationFile.GetDirectoryContainingThis();
                if (!System.IO.Directory.Exists(directory.FullPath))
                {
                    System.IO.Directory.CreateDirectory(directory.FullPath);
                }

                System.IO.File.WriteAllText(destinationFile.FullPath, fileContents);

                GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(destinationFile.FullPath);
            });
        }
    }
}
