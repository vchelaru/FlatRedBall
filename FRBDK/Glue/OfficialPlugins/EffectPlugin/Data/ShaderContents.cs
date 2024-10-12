using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

enum ShaderContentsType
{
    GradientColors,
    Saturation,
    Bloom,
    Crt
}

internal class ShaderContents
{
    public ShaderContentsType ShaderContentsType { get; protected set; }

    /// <summary>
    /// Contains the entire .fx contents. If this is null, then the individual fx properties are used
    /// </summary>
    public string EntireFxFileContents { get; protected set; }
    public string ExternalParameters { get; protected set; }
    public string VertexShader { get; protected set; }
    public string PixelShader { get; protected set; }

    /// <summary>
    /// Contains the entire .cs contents. If this is null, then the individual cs properties are used
    /// </summary>
    public string EntireCsFileContents { get; protected set; }
    public string ClassMembers { get; protected set; }
    public string ApplyBody { get; protected set; }
}

