float4x4 ViewProj : VIEWPROJ; //our world view projection matrix
uniform extern texture CurrentTexture;


sampler textureSampler = sampler_state
{
    Texture = <CurrentTexture>;
    mipfilter = LINEAR; 
};


//application to vertex structure
struct a2v
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float4 texCoord : TEXCOORD0;
    
};

//vertex to pixel processing structure
struct v2p
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float4 texCoord : TEXCOORD0;
};
//VERTEX SHADER
void vs( in a2v IN, out v2p OUT )
{
    //transforming our position from object space to screen space.
    OUT.position = mul(IN.position, ViewProj);
    OUT.color = IN.color;
    OUT.texCoord = IN.texCoord;
}

float4 TexturePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    
	
	// premult requires this

	color[0] = color[0] * IN.color[3];  
	color[1] = color[1] * IN.color[3];
	color[2] = color[2] * IN.color[3];
	color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);    
	return color;
}

float4 AddPixelShader(a2v IN ) : COLOR
{
    float4 fromTexture =  tex2D(textureSampler, IN.texCoord).rgba;

    fromTexture[0] = (fromTexture[0] + IN.color[0] * fromTexture[3]) * IN.color[3];
    fromTexture[1] = (fromTexture[1] + IN.color[1] * fromTexture[3]) * IN.color[3];
    fromTexture[2] = (fromTexture[2] + IN.color[2] * fromTexture[3]) * IN.color[3];
    fromTexture[3] = fromTexture[3] * IN.color[3];
	clip(fromTexture[3] - .001);
	return fromTexture;
}

float4 SubtractPixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = (color[0] - IN.color[0]) * IN.color[3];
    color[1] = (color[1] - IN.color[1]) * IN.color[3];
    color[2] = (color[2] - IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ModulatePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = (color[0] * IN.color[0]) * IN.color[3];
    color[1] = (color[1] * IN.color[1]) * IN.color[3];
    color[2] = (color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate2XPixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = (2 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (2 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (2 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate4XPixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = (4 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (4 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (4 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 InversePixelShader(a2v IN ) : COLOR
{
    float4 color =  tex2D(textureSampler, IN.texCoord).rgba;
    color[0] = (1 - color[0]) * IN.color[3];
    color[1] = (1 - color[1]) * IN.color[3];
    color[2] = (1 - color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ColorPixelShader(a2v IN ) : COLOR
{
	clip(IN.color[3] - .001);
    return IN.color;
}

float4 ColorTextureAlphaPixelShader(a2v IN ) : COLOR
{
	float4 color = IN.color;
	float alphaFromTexture = tex2D(textureSampler, IN.texCoord).a;
	color[0] = color[0] * IN.color[3] * alphaFromTexture;

	color[1] = color[1] * IN.color[3] * alphaFromTexture;

	color[2] = color[2] * IN.color[3] * alphaFromTexture;

	color[3] = color[3] * alphaFromTexture;
	clip(color[3] - .001);
	return color;
}

float4 InterpolateColorPixelShader(a2v IN ) :COLOR
{
	float4 color = IN.color;

	color = (color[3] * tex2D(textureSampler, IN.texCoord)) + (1 - color[3])*(color);
	
	color[3] = tex2D(textureSampler, IN.texCoord).a;
	clip(color[3] - .001);
	//color[3] = color[3] * tex2D(textureSampler, IN.texCoord).a;
	return color;	

}

technique Texture
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 TexturePixelShader();
    }
}

technique Add
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 AddPixelShader();
	}
}

technique Subtract
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 SubtractPixelShader();
	}
}

technique Modulate
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ModulatePixelShader();
	}
}

technique Modulate2X
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate2XPixelShader();
	}
}

technique Modulate4X
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate4XPixelShader();
	}
}

technique InverseTexture
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 InversePixelShader();
	}
}

technique Color
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorPixelShader();
	}
}

technique ColorTextureAlpha
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader();
	}
}

technique InterpolateColor
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();	
		pixelshader = compile ps_2_0 InterpolateColorPixelShader();			
	}

}

