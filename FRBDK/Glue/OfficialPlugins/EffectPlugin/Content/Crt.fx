#define luminance(c) (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b)

uniform extern texture SourceTexture : register(s0);
sampler SourceTextureSampler = sampler_state
{
    Texture = <SourceTexture>;
    Filter = LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

static float3 VibranceRGBBalance = float3(1.00, 1.00, 1.00);

uniform extern float4 OriginalSize;
uniform extern float4 OutputSize;
uniform extern float PixelWidth;
uniform extern float PixelHeight;

uniform extern float Exposure = 1.0;
uniform extern float Vibrance = 0.0;

uniform extern float SmoothingWeightB = 0.7;
uniform extern float SmoothingWeightS = 0.3;

uniform extern float CrtSmoothingWeightP1B = 0.7;
uniform extern float CrtSmoothingWeightP1H = 0.15;
uniform extern float CrtSmoothingWeightP1V = 0.15;
uniform extern float CrtSmoothingWeightP2B = 0.3;
uniform extern float CrtSmoothingWeightP2H = 0.5;
uniform extern float CrtSmoothingWeightP2V = 0.2;

uniform extern float ScanMaskStrenght = 0.5;
uniform extern float ScanScale = -8.0;
uniform extern float ScanKernelShape = 2.0;
uniform extern float ScanBrightnessBoost = 1.0;

uniform extern float WarpX = 0.01;
uniform extern float WarpY = 0.02;

uniform extern float CaRedOffset = 0.0006;
uniform extern float CaBlueOffset = 0.0006;

float3 ApplyVibrance(float3 colorInput)
{
    float3 vibranceCoeff = float3(VibranceRGBBalance * Vibrance);

    float3 color = colorInput; // Original input color
    float3 lumCoeff = float3(0.3, 0.59, 0.11); // Values to calculate luma with

    float luma = dot(lumCoeff, color.rgb); // Calculate luma (grey)

    float maxColor = max(colorInput.r, max(colorInput.g, colorInput.b)); // Find the strongest color
    float minColor = min(colorInput.r, min(colorInput.g, colorInput.b)); // Find the weakest color

    float colorSaturation = maxColor - minColor; // The difference between the two is the saturation

    color.rgb = lerp(luma, color.rgb, (1.0 + (vibranceCoeff * (1.0 - (sign(vibranceCoeff) * colorSaturation))))); // Extrapolate between luma and original by 1 + (1-saturation)

    return color;
}

float2 ApplyWarp(float2 pos)
{
    pos = pos * 2.0 - 1.0;
    pos *= float2(1.0 + (pos.y * pos.y) * WarpX, 1.0 + (pos.x * pos.x) * WarpY);
    
    return pos * 0.5 + 0.5;
}

float2 GetScanDistance(float2 pos)
{
    pos = pos * OriginalSize.xy;
    
    return -((pos - floor(pos)) - float2(0.5, 0.5));
}

float GetScanGaussian(float pos, float scale)
{
    return exp2(scale * pow(abs(pos), ScanKernelShape));
}

float GetScanWeight(float2 pos, float off)
{
    float dst = GetScanDistance(pos).y;

    return GetScanGaussian(dst + off, ScanScale);
}

float GetThreeLinesScanWeight(float2 pos)
{
    float wa = GetScanWeight(pos, -1.0);
    float wb = GetScanWeight(pos, 0.0);
    float wc = GetScanWeight(pos, 1.0);
    
    return wa + wb + wc;
}

float3 ApplyScanMask(float3 source, float scanMask)
{
    source *= ScanBrightnessBoost;
    float3 effect = source * scanMask;
    return ((source * (1.0 - ScanMaskStrenght)) + (effect * ScanMaskStrenght));
}

float4 BaseShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float3 source = tex2D(SourceTextureSampler, texCoord).rgb;
    
    source *= Exposure;
    source = ApplyVibrance(source);
    
    return float4(source, 1.0);
}

float4 BaseAndSmoothingShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float3 c = tex2D(SourceTextureSampler, texCoord).rgb;
    float3 n = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y - PixelHeight)).rgb;
    float3 s = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y + PixelHeight)).rgb;
    float3 w = tex2D(SourceTextureSampler, float2(texCoord.x - PixelWidth, texCoord.y)).rgb;
    float3 e = tex2D(SourceTextureSampler, float2(texCoord.x + PixelWidth, texCoord.y)).rgb;
    
    c = pow(c, 2.2);
    n = pow(n, 2.2);
    s = pow(s, 2.2);
    w = pow(w, 2.2);
    e = pow(e, 2.2);
    
    float3 output = c * SmoothingWeightB + ((n + s + w + e) / 4.0) * SmoothingWeightS;
    
    output = pow(output, 1 / 2.2);
    
    output *= Exposure;
    output = ApplyVibrance(output);
    
    return float4(output, 1.0);
}

float4 CrtBaseAndSmoothingPass1Shader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float3 c = tex2D(SourceTextureSampler, texCoord).rgb;
    float3 n = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y - PixelHeight)).rgb;
    float3 s = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y + PixelHeight)).rgb;
    float3 w = tex2D(SourceTextureSampler, float2(texCoord.x - PixelWidth, texCoord.y)).rgb;
    float3 e = tex2D(SourceTextureSampler, float2(texCoord.x + PixelWidth, texCoord.y)).rgb;
    
    c = pow(c, 2.2);
    n = pow(n, 2.2);
    s = pow(s, 2.2);
    w = pow(w, 2.2);
    e = pow(e, 2.2);
    
    float3 output = c * CrtSmoothingWeightP1B + ((w + e) / 2.0) * CrtSmoothingWeightP1H + ((n + s) / 2.0) * CrtSmoothingWeightP1V;
    
    output = pow(output, 1 / 2.2);
    
    output *= Exposure;
    output = ApplyVibrance(output);
    
    return float4(output, 1.0);
}

float4 CrtScanShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    texCoord = ApplyWarp(texCoord);
    
    float3 source;
    
    if (texCoord.x > 1.0 || texCoord.x < 0 || texCoord.y > 1.0 || texCoord.y < 0)
    {
        // Dark corners due to warping
        source = 0.0;
    }
    else
    {
        source = tex2D(SourceTextureSampler, texCoord).rgb;
    }
    
    float scanMask = GetThreeLinesScanWeight(texCoord);
    source = ApplyScanMask(source, scanMask);
    
    return float4(source, 1.0f);
}

float4 CrtScanCaShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    texCoord = ApplyWarp(texCoord);
    
    float3 source;
    
    if (texCoord.x > 1.0 || texCoord.x < 0 || texCoord.y > 1.0 || texCoord.y < 0)
    {
        // Dark corners due to warping
        source = 0.0;
    }
    else
    {
        // Do chromatic aberration
        float red = tex2D(SourceTextureSampler, texCoord - CaRedOffset).r;
        float green = tex2D(SourceTextureSampler, texCoord).g;
        float blue = tex2D(SourceTextureSampler, texCoord - CaBlueOffset).b;
        source = float3(red, green, blue);
    }
    
    float scanMask = GetThreeLinesScanWeight(texCoord);
    source = ApplyScanMask(source, scanMask);
    
    return float4(source, 1.0f);
}

float4 CrtSmoothingPass2Shader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float3 c = tex2D(SourceTextureSampler, texCoord).rgb;
    float3 n = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y - PixelHeight)).rgb;
    float3 s = tex2D(SourceTextureSampler, float2(texCoord.x, texCoord.y + PixelHeight)).rgb;
    float3 w = tex2D(SourceTextureSampler, float2(texCoord.x - PixelWidth, texCoord.y)).rgb;
    float3 e = tex2D(SourceTextureSampler, float2(texCoord.x + PixelWidth, texCoord.y)).rgb;
    
    c = pow(c, 2.2);
    n = pow(n, 2.2);
    s = pow(s, 2.2);
    w = pow(w, 2.2);
    e = pow(e, 2.2);
    
    float3 output = c * CrtSmoothingWeightP2B + ((w + e) / 2.0) * CrtSmoothingWeightP2H + ((n + s) / 2.0) * CrtSmoothingWeightP2V;

    output = pow(output, 1 / 2.2);
    return float4(output, 1.0);
}

float4 DummyShader(float4 pos : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float3 source = tex2D(SourceTextureSampler, texCoord).rgb;
    return float4(source * OutputSize * OriginalSize * ScanMaskStrenght * ScanBrightnessBoost, 1.0);
}

technique Base
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 BaseShader();
    }
}

technique BaseAndSmoothing
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 BaseAndSmoothingShader();
    }
}

technique CrtBaseAndSmoothingPass1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 CrtBaseAndSmoothingPass1Shader();
    }
}

technique CrtScan
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 CrtScanShader();
    }
}

technique CrtScanCa
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 CrtScanCaShader();
    }
}

technique CrtSmoothingPass2
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 CrtSmoothingPass2Shader();
    }
}

technique Dummy
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 DummyShader();
    }
}
