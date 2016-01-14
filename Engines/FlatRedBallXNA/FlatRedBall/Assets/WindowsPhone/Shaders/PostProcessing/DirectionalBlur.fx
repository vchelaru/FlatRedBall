// Directional Blur shader

// Shared variables
shared float2 pixelSize;

// Samplers
sampler inputSampler : register(s0);

// Blur sampling weights
#define MAX_SAMPLES 9
float sampleWeights[MAX_SAMPLES];
float2 sampleOffsets[MAX_SAMPLES];

// Directional blur shader
float4 BlurShader(float2 texCoord : TEXCOORD0, uniform int sampleCount) : COLOR
{
	float4 c = 0;
	
	for (int i = 0; i < sampleCount; i++)
	{
		c += tex2D(inputSampler, texCoord + sampleOffsets[i]) * sampleWeights[i];
	}
	
	return c;
}

// Techniques

technique BlurHi
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(9);
    }
}

technique BlurMed
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(7);
    }
}

technique BlurLow
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 BlurShader(5);
    }
}