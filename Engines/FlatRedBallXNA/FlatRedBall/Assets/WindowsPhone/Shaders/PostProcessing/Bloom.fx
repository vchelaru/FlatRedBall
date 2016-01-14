// Bloom shader

// Shared variables
shared float2 pixelSize;

// Textures Samplers
sampler inputSampler : register(s0);
sampler bloomSampler : register(s1);

// Bloom Threshold
float threshold;

// Bloom Combination settings
float baseSaturation;
float bloomSaturation;
float baseIntensity;
float bloomIntensity;

float horizontalSampleMultiplier = 1;
float verticalSampleMultiplier = 1;

// Blur sampling weights
#define MAX_SAMPLES 9
float sampleWeights[MAX_SAMPLES];
float2 sampleOffsetsHor[MAX_SAMPLES];
float2 sampleOffsetsVer[MAX_SAMPLES];

// Extract Filter
// Extracts and normalizes values above the bloom threshold
float4 BloomExtractShader(float2 texCoord : TEXCOORD0) : COLOR
{
	float4 c = tex2D(inputSampler, texCoord);

	return saturate((c - threshold) / (1 - threshold));
}


// First pass of blur filter - blurs horizontally
float4 BlurShader(float2 texCoord : TEXCOORD0, uniform bool horizontal,
											   uniform int sampleCount) : COLOR
{
	float4 c = 0;
	
	for (int i = 0; i < sampleCount; i++)
	{
		// This code will compile differently depending on the horizontal parameter
		if (horizontal)
		{
			c += tex2D(inputSampler, texCoord + sampleOffsetsHor[i]) * sampleWeights[i];
		}
		else // vertical
		{
			c += tex2D(inputSampler, texCoord + sampleOffsetsVer[i]) * sampleWeights[i];
		}
	}
	
	return c;
}

float4 SetSaturation(float4 color, float saturation)
{
	// Multiply original color value by gray-weightings
	// (weights represent the eye's sensitivity to certain
	//  colors)
	return lerp(
		dot(color, float3(0.299f, 0.587f, 0.114f)),
		color,
		saturation);
}

// Combine
// Combines the bloom image with the original image
float4 BloomCombineShader(float2 texCoord : TEXCOORD0) : COLOR
{
	

	float4 base = tex2D(inputSampler, texCoord);
	
	float2 scaledTextureCoordinates = texCoord;
	scaledTextureCoordinates.x *= horizontalSampleMultiplier;
	scaledTextureCoordinates.y *= verticalSampleMultiplier;	
	float4 bloom = tex2D(bloomSampler, scaledTextureCoordinates);
	
	base = SetSaturation(base, baseSaturation) * baseIntensity;
	bloom = SetSaturation(bloom, bloomSaturation) * bloomIntensity;

	base *= (1 - saturate(bloom));

	return base + bloom;
}

// Techniques

technique BloomExtract
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BloomExtractShader();
    }
}

technique BlurHorizontalHi
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(true, 9);
    }
}

technique BlurVerticalHi
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(false, 7);
    }
}

technique BlurHorizontalMed
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(true, 7);
    }
}

technique BlurVerticalMed
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(false, 5);
    }
}

technique BlurHorizontalLow
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(true, 5);
    }
}

technique BlurVerticalLow
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(false, 5);
    }
}

technique BloomCombine
{
	pass Pass0
	{
		pixelShader = compile ps_2_0 BloomCombineShader();
	}
}