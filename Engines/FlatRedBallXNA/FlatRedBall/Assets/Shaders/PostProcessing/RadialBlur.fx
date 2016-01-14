// Directional Blur shader

// Shared variables
shared float2 pixelSize;

// Samplers
sampler inputSampler : register(s0);

// Blur sampling weights
#define MAX_SAMPLES 9
float sampleWeights[MAX_SAMPLES];
float2 radialSource;
float sampleScale;

// Radial blur shader
float4 BlurShader(float2 texCoord : TEXCOORD0, uniform int sampleCount) : COLOR
{
	float4 c = 0;
	
	for (int i = 0; i < sampleCount; i++)
	{
		float2 sampleDirection = normalize(texCoord - radialSource) * pixelSize;
		float sampleDistance = i * sampleScale / sampleCount;
		float2 samplePoint = (sampleDirection * sampleDistance);
		c += tex2D(inputSampler, texCoord + samplePoint) * sampleWeights[i];
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