using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Data;

internal class GradientColorContents : ShaderContents
{
    public GradientColorContents()
    {
        ShaderContentsType = ShaderContentsType.GradientColors;
        ExternalParameters =
            @"";

        ExternalParameters =
@"
float Time; // Total game time elapsed in seconds
float NormalizedTime; // The percentage of this shader's animation that has elapsed. 0 is the beginning, 1 is the end.
float2 UVPerPixel; // The distance between each pixel on screen in texture coordinates.
float2 Resolution; // Screen resolution

// Debug parameters for visualizing the shader and its parameters. Usually wanna stick to only setting one of them to 1f at a time.
float TexWeight;
float PixelPosWeight;
float ScreenPosWeight;
float WorldPosWeight;
float ColorWeight;
float UvWeight;
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
 
    return (TexWeight * textureSample
        + PixelPosWeight * float4(input.Position.xy, 0, 1)
        + ScreenPosWeight * float4(input.ScreenPosition.xyz, 1)
        + WorldPosWeight * float4(input.WorldPosition.xyz, 1)
        + ColorWeight * input.Color
        + UvWeight * input.TexCoord);
}";

        ApplyBody =
           @"_effect.Parameters[""TexWeight""].SetValue(1.0f);
            _effect.Parameters[""PixelPosWeight""].SetValue(0.0f);
            _effect.Parameters[""ScreenPosWeight""].SetValue(1.0f);
            _effect.Parameters[""WorldPosWeight""].SetValue(0.0f);
            _effect.Parameters[""ColorWeight""].SetValue(0.0f);
            _effect.Parameters[""UvWeight""].SetValue(0.0f);
            
            Wrapper.Draw(Camera.Main, _effect, sourceTexture);";
    }
}
