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

internal class FxContentsGenerator
{
    internal static void ReplaceContents(string rfsName, ShaderContents shaderContents)
    {
        var fxFilePath = GlueCommands.Self.GetAbsoluteFilePath(rfsName);

        GlueCommands.Self.TryMultipleTimes(() =>
        {
            // We can't use the actual .fx file because the actual .fx file cannot
            // contain strings for replacement - if it did then it would initially fail
            // to compile. Therefore, we need to read the embedded resource and then replace it:
            //var contents = System.IO.File.ReadAllText(fxFilePath.FullPath);

            var assembly = typeof(FxContentsGenerator).Assembly;
            var resourceName = "OfficialPlugins.EffectPlugin.Content.FxTemplate.txt";
            using var stream =
                assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string contents = reader.ReadToEnd();


                var builder = new StringBuilder();
                builder.Append(contents);

                builder
                    .Replace("ReplaceExternalParameters", shaderContents.ExternalParameters);
                builder
                    .Replace("ReplaceVertexShader", shaderContents.VertexShader);
                builder
                    .Replace("ReplacePixelShader", shaderContents.PixelShader);

                // Use FRB's saving to minimize the number of file notifications
                FlatRedBall.IO.FileManager.SaveText(builder.ToString(), fxFilePath.FullPath);

            }
        });
    }
}
