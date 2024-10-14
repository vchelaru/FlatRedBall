using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

internal class BloomContents : ShaderContents
{
    public BloomContents()
    {
        ShaderContentsType = ShaderContentsType.Bloom;
        AssignEntireCsFileContents();
        AssignEntireFxFileContents();
    }

    private void AssignEntireCsFileContents()
    {
        var assemblyContainingResource = GetType().Assembly;
        const string resourceName = "OfficialPlugins.EffectPlugin.EmbeddedCodeFiles.BloomPostProcessingClass.cs";
        using var stream = assemblyContainingResource.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var fileContents = reader.ReadToEnd();
        this.EntireCsFileContents = fileContents;
    }

    private void AssignEntireFxFileContents()
    {
        var assemblyContainingResource = GetType().Assembly;
        const string resourceName = "OfficialPlugins.EffectPlugin.Content.Bloom.fx";
        using var stream = assemblyContainingResource.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var fileContents = reader.ReadToEnd();
        this.EntireFxFileContents = fileContents;
    }
}
