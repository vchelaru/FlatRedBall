#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

//==============================================================================
// External Parameters
//==============================================================================
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

texture ScreenTexture; // Texture data for the whole game screen
sampler screenTextureSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Wrap; // X direction sampling
    AddressV = Wrap; // Y direction sampling
    // MipFilter = Linear;
    // MinFilter = Linear;
    // MagFilter = Linear;
    // AddressU = Clamp;
    // AddressV = Clamp;
};

//==============================================================================
// Shader Stage Parameters
//==============================================================================
struct AssemblerToVertex
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
    float4 Normal : NORMAL0;
};

struct VertexToPixel
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
    float4 ScreenPosition : TEXCOORD1;
    float4 WorldPosition : TEXCOORD2;
};

//==============================================================================
// Vertex Shaders
//==============================================================================
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

//==============================================================================
// Pixel Shaders
//==============================================================================
float4 PsMain(VertexToPixel input) : SV_TARGET
{
    float4 textureSample = tex2D(screenTextureSampler,  input.TexCoord.xy);
 
    return (TexWeight * textureSample
        + PixelPosWeight * float4(input.Position.xy, 0, 1)
        + ScreenPosWeight * float4(input.ScreenPosition.xyz, 1)
        + WorldPosWeight * float4(input.WorldPosition.xyz, 1)
        + ColorWeight * input.Color
        + UvWeight * input.TexCoord);
}

//==============================================================================
// Techniques
//==============================================================================
technique DrawPrimitives
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL VsMain();
        PixelShader = compile PS_SHADERMODEL PsMain();
    }
}

technique SpriteBatch
{
    pass Pass0
    {
        PixelShader = compile PS_SHADERMODEL PsMain();
    }
}

