using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

internal class CrtContents : ShaderContents
{
    public CrtContents()
    {
        ShaderContentsType = ShaderContentsType.Crt;
        AssignEntireCsFileContents();
        AssignEntireFxFileContents();
    }

    private void AssignEntireCsFileContents()
    {
        var assemblyContainingResource = GetType().Assembly;
        const string resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.CrtPostProcessingClass.cs";
        using var stream = assemblyContainingResource.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var fileContents = reader.ReadToEnd();
        this.EntireCsFileContents = fileContents;
    }

    private void AssignEntireFxFileContents()
    {
        var assemblyContainingResource = GetType().Assembly;
        const string resourceName = "OfficialPlugins.EffectPlugin.Content.Crt.fx";
        using var stream = assemblyContainingResource.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var fileContents = reader.ReadToEnd();
        this.EntireFxFileContents = fileContents;
    }
}
