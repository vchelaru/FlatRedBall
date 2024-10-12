
////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  VARIABLES
////////////////////////////////////////////////////////////////////////////////////////////////////////////

float2 HalfPixel;
float2 DownsampleOffset;
float2 UpsampleOffset;

// Strength of the effect.
float Strength;

// The threshold of pixels that are brighter than that.
float Threshold = 0.8f;

// Input texture
Texture2D ScreenTexture;

SamplerState LinearSampler
{
    Texture = <ScreenTexture>;

    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;

    AddressU = CLAMP;
    AddressV = CLAMP;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  STRUCTS
////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  FUNCTIONS
////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  VERTEX SHADER
////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = float4(input.Position, 1);
    output.TexCoord = input.TexCoord;
    return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  PIXEL SHADER
////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Just an average of 4 values.
float4 Box4(float4 p0, float4 p1, float4 p2, float4 p3)
{
    return (p0 + p1 + p2 + p3) * 0.25f;
}

// Extracts the pixels we want to blur
float4 ExtractPS(float4 pos : SV_POSITION, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
    float4 color = ScreenTexture.Sample(LinearSampler, texCoord + HalfPixel);
    
    float avg = (color.r + color.g + color.b) / 3;

    if (avg > Threshold)
    {
        return color * ((avg - Threshold) / (1 - Threshold)); // * (avg - Threshold);
    }

    return float4(0, 0, 0, 0);
}

// Extracts the pixels we want to blur, but considers luminance instead of average rgb
float4 ExtractLuminancePS(float4 pos : SV_POSITION, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
    float4 color = ScreenTexture.Sample(LinearSampler, texCoord + HalfPixel);
    
    float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;

    if (luminance > Threshold)
    {
        return color * ((luminance - Threshold) / (1 - Threshold)); // * (luminance - Threshold);
        //return saturate((color - Threshold) / (1 - Threshold));
    }

    return float4(0, 0, 0, 0);
}

// Downsample to the next mip, blur in the process
float4 DownsamplePS(float4 pos : SV_POSITION, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
    float4 c0 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-2, -2) * DownsampleOffset + HalfPixel);
    float4 c1 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, -2) * DownsampleOffset + HalfPixel);
    float4 c2 = ScreenTexture.Sample(LinearSampler, texCoord + float2(2, -2) * DownsampleOffset + HalfPixel);
    float4 c3 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, -1) * DownsampleOffset + HalfPixel);
    float4 c4 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, -1) * DownsampleOffset + HalfPixel);
    float4 c5 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-2, 0) * DownsampleOffset + HalfPixel);
    float4 c6 = ScreenTexture.Sample(LinearSampler, texCoord + HalfPixel);
    float4 c7 = ScreenTexture.Sample(LinearSampler, texCoord + float2(2, 0) * DownsampleOffset + HalfPixel);
    float4 c8 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, 1) * DownsampleOffset + HalfPixel);
    float4 c9 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, 1) * DownsampleOffset + HalfPixel);
    float4 c10 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-2, 2) * DownsampleOffset + HalfPixel);
    float4 c11 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, 2) * DownsampleOffset + HalfPixel);
    float4 c12 = ScreenTexture.Sample(LinearSampler, texCoord + float2(2, 2) * DownsampleOffset + HalfPixel);

    return Box4(c0, c1, c5, c6) * 0.125f +
    Box4(c1, c2, c6, c7) * 0.125f +
    Box4(c5, c6, c10, c11) * 0.125f +
    Box4(c6, c7, c11, c12) * 0.125f +
    Box4(c3, c4, c8, c9) * 0.5f;
}

// Upsample to the former MIP, blur in the process
float4 UpsamplePS(float4 pos : SV_POSITION, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
    float4 c0 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, -1) * UpsampleOffset + HalfPixel);
    float4 c1 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, -1) * UpsampleOffset + HalfPixel);
    float4 c2 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, -1) * UpsampleOffset + HalfPixel);
    float4 c3 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, 0) * UpsampleOffset + HalfPixel);
    float4 c4 = ScreenTexture.Sample(LinearSampler, texCoord + HalfPixel);
    float4 c5 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, 0) * UpsampleOffset + HalfPixel);
    float4 c6 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, 1) * UpsampleOffset + HalfPixel);
    float4 c7 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, 1) * UpsampleOffset + HalfPixel);
    float4 c8 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, 1) * UpsampleOffset + HalfPixel);
    
    // Tent filter = 0.0625f
    return 0.0625f * (c0 + 2 * c1 + c2 + 2 * c3 + 4 * c4 + 2 * c5 + c6 + 2 * c7 + c8) * Strength;
}

// Upsample to the former MIP, blur in the process, change offset depending on luminance
float4 UpsampleLuminancePS(float4 pos : SV_POSITION, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
    float4 c4 = ScreenTexture.Sample(LinearSampler, texCoord + HalfPixel); // Middle pixel
    
    /*float luminance = c4.r * 0.21f + c4.g * 0.72f + c4.b * 0.07f;
    luminance = max(luminance, 0.4f);*/
    
    float4 c0 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, -1) * UpsampleOffset + HalfPixel);
    float4 c1 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, -1) * UpsampleOffset + HalfPixel);
    float4 c2 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, -1) * UpsampleOffset + HalfPixel);
    float4 c3 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, 0) * UpsampleOffset + HalfPixel);
    float4 c5 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, 0) * UpsampleOffset + HalfPixel);
    float4 c6 = ScreenTexture.Sample(LinearSampler, texCoord + float2(-1, 1) * UpsampleOffset + HalfPixel);
    float4 c7 = ScreenTexture.Sample(LinearSampler, texCoord + float2(0, 1) * UpsampleOffset + HalfPixel);
    float4 c8 = ScreenTexture.Sample(LinearSampler, texCoord + float2(1, 1) * UpsampleOffset + HalfPixel);
    
    // Tent filter = 0.0625f
    return 0.0625f * (c0 + 2 * c1 + c2 + 2 * c3 + 4 * c4 + 2 * c5 + c6 + 2 * c7 + c8) * Strength;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  TECHNIQUES
////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Extract
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 ExtractPS();
    }
}

technique ExtractLuminance
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 ExtractLuminancePS();
    }
}

technique Downsample
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DownsamplePS();
    }
}

technique Upsample
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 UpsamplePS();
    }
}

technique UpsampleLuminance
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 UpsampleLuminancePS();
    }
}



// =====================Bloom Combine Below=====================
// Pixel shader combines the bloom image with the original
// scene, using tweakable intensity levels and saturation.
// This is the final step in applying a bloom postprocess.

sampler BloomSampler : register(s0)
{
    Filter = LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

sampler BaseSampler : register(s1)
{
    Texture = (BaseTexture);
    Filter = POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float BloomIntensity = 1;
float BloomSaturation = 1;

// Helper for modifying the saturation of a color.
float3 AdjustSaturation(float3 color, float saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    float grey = dot(color, float3(0.3, 0.59, 0.11));

    return lerp(grey, color, saturation);
}

float4 BloomCombineFunction(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the bloom and original base image colors.
    float3 bloom = tex2D(BloomSampler, texCoord).rgb;
    float3 base = tex2D(BaseSampler, texCoord).rgb;

    // Adjust color saturation and intensity and combine both images
    base += max(0, AdjustSaturation(bloom, BloomSaturation)) * BloomIntensity;
    
    return float4(base, 1.0f);
}

float4 BloomSaturateFunction(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the bloom and original base image colors.
    float3 bloom = tex2D(BloomSampler, texCoord).rgb;

    // Adjust color saturation and intensity and combine both images.
    // Multiply by vertex color so the effect can be turned off from outside.
    bloom = max(0, AdjustSaturation(bloom, BloomSaturation)) * BloomIntensity * color.rgb;
    
    return float4(bloom, 1.0f);
}

technique BloomCombine
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 BloomCombineFunction();
    }
}

technique BloomSaturate
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 BloomSaturateFunction();
    }
}
