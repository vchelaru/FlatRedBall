using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

enum ShaderContentsType
{
    GradientColors,
    Saturation
}

internal class ShaderContents
{
    public ShaderContentsType ShaderContentsType { get; protected set; }

    public string ExternalParameters { get; protected set; }
    public string VertexShader { get; protected set; }
    public string PixelShader { get; protected set; }

    public string ClassMembers { get; protected set; }
    public string ApplyBody { get; protected set; }
}

