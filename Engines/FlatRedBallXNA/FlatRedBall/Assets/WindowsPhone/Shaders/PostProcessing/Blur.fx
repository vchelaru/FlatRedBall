// Blur shader

// Shared variables
shared float2 pixelSize;

// Samplers
sampler inputSampler : register(s0);

// Blur sampling weights
#define MAX_SAMPLES 9
float sampleWeights[MAX_SAMPLES];
float2 sampleOffsetsHor[MAX_SAMPLES];
float2 sampleOffsetsVer[MAX_SAMPLES];

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

// Techniques

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