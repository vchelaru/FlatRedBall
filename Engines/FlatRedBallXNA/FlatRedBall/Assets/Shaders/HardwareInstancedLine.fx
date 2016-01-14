float4x4 ViewProj : VIEWPROJ; //our world view projection matrix
uniform extern texture CurrentTexture;

sampler textureSampler = sampler_state
{
    Texture = <CurrentTexture>;
    AddressU  = CLAMP;        
    AddressV  = CLAMP;
    AddressW  = CLAMP;
    mipfilter = LINEAR; 
};


//application to vertex structure
struct a2v
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float2 texCoord : TEXCOORD0;
    
};

//vertex to pixel processing structure
struct v2p
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float2 texCoord : TEXCOORD0;
};

struct VSOutput
{
	float4 position : Position;
	float4 color : Color0;
	float2 texCoord: TEXCOORD0;
	
};

VSOutput VertShader(
	float4 position : Position0,
	float4 color : Color0,
	float2 texCoord: TEXCOORD0,
	float3 instancePosition : Position1
	)
{
	VSOutput output;
	
	// compute position
	output.texCoord.x = texCoord.x;
	output.texCoord.y = texCoord.y;
	
	output.position.xyz = position.xyz + instancePosition.xyz;
	
	output.position.w = 1;
	
	output.position = mul(output.position, ViewProj);

	output.color.xyzw = float4(1,1,1,1);// = color;
	return output;
}

float4 TexturePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[3] = color[3] * IN.color[3];
	return color;
}

float4 AddPixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = color[0] + IN.color[0];
    color[1] = color[1] + IN.color[1];
    color[2] = color[2] + IN.color[2];
    color[3] = color[3] * IN.color[3];
	return color;
}

float4 SubtractPixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = color[0] - IN.color[0];
    color[1] = color[1] - IN.color[1];
    color[2] = color[2] - IN.color[2];
    color[3] = color[3] * IN.color[3];
	return color;
}

float4 ModulatePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = color[0] * IN.color[0];
    color[1] = color[1] * IN.color[1];
    color[2] = color[2] * IN.color[2];
    color[3] = color[3] * IN.color[3];
	return color;
}

float4 InversePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = 1 - color[0];
    color[1] = 1 - color[1];
    color[2] = 1 - color[2];
    color[3] = color[3] * IN.color[3];
	return color;
}

float4 ColorPixelShader(a2v IN ) : COLOR
{
    return IN.color;
}

float4 ColorTextureAlphaPixelShader(a2v IN ) : COLOR
{
	float4 color = IN.color;
	color[3] = color[3] * tex2D(textureSampler, IN.texCoord).a;
	return color;
}

technique Texture
{
    pass p0
    {
        
		VertexShader = compile vs_2_0 VertShader();
        pixelshader = compile ps_2_0 TexturePixelShader();
    }
}

technique Add
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 AddPixelShader();
	}
}

technique Subtract
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 SubtractPixelShader();
	}
}

technique Modulate
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 ModulatePixelShader();
	}
}

technique InverseTexture
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 InversePixelShader();
	}
}

technique Color
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 ColorPixelShader();
	}
}

technique ColorTextureAlpha
{
	pass p0
	{
		
		VertexShader = compile vs_2_0 VertShader();
		pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader();
	}
}