// Blur shader

// Shared variables
shared float2 pixelSize;

// Samplers
sampler inputSampler : register(s0);

// Strength of the pixellation
float strength;

// Pixellates the current texture
float4 PixellateShader(float2 texCoord : TEXCOORD0) : COLOR
{
	// Get the pixel coordinate to use
	float2 psize = (pixelSize * strength);
	float2 sampleCoord = (floor(texCoord / psize) + 0.5f) * psize;

	// Get the color	
	float4 c = tex2D(inputSampler, sampleCoord);

	return c;
}

// Techniques

technique Pixellate
{
    pass Pass0
    {
		pixelShader = compile ps_2_0 PixellateShader();
    }
}