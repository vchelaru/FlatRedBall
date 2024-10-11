using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

internal class SaturationContents : ShaderContents
{
    public SaturationContents()
    {
        ShaderContentsType = ShaderContentsType.Saturation;

        ExternalParameters =
@"
float Saturation;
";

        VertexShader =
@"
VertexToPixel VsMain(const in AssemblerToVertex input)
{
    VertexToPixel output;
    
    output.Position = input.Position;
    output.ScreenPosition = input.Position;
    output.WorldPosition = input.Normal;
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    
    return output;
}
";

        PixelShader =
@"
float4 PsMain(VertexToPixel input) : SV_TARGET
{
    float4 textureSample = tex2D(screenTextureSampler,  input.TexCoord.xy);
    
    // Y = 0.299 R + 0.587 G + 0.114 B
    float luminosity = textureSample.x * 0.299 + textureSample.y * 0.587 + textureSample.z * 0.114;

    float4 result = 
        float4(luminosity, luminosity, luminosity, 1) * (1-Saturation) + 
        textureSample * Saturation;
    
    return result;
}
";

        ClassMembers = @"protected FullscreenEffectWrapper Wrapper { get; set; } = new FullscreenEffectWrapper();
        public float Saturation { get; set; } = 0.5f;";

        ApplyBody =
           @"if(_effect?.IsDisposed == true) return;
            _effect.Parameters[""Saturation""]?.SetValue(Saturation);
            Wrapper.Draw(Camera.Main, _effect, sourceTexture);";
    }
}
