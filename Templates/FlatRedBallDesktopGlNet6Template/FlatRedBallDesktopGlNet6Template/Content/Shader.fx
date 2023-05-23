float4x4 ViewProj : VIEWPROJ; //our world view projection matrix
uniform extern texture CurrentTexture;


 sampler linearTextureSampler = sampler_state
 {
     Texture = <CurrentTexture>;
     MipFilter = Linear;
     MinFilter = Linear;
     MagFilter = Linear;
 };


 sampler pointTextureSampler = sampler_state
 {
     Texture = <CurrentTexture>;
     MipFilter = Point;
     MinFilter = Point;
     MagFilter = Point;
 };

// Application to vertex structure
struct a2v
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float4 texCoord : TEXCOORD0;
    
};

// Vertex to pixel processing structure
struct v2p
{
    float4 position : POSITION0;
    float4 color : COLOR0; 
    float4 texCoord : TEXCOORD0;
};

// Vertex shader
void vs( in a2v IN, out v2p OUT )
{
    // Transforming our position from object space to screen space
    OUT.position = mul(IN.position, ViewProj);
    OUT.color = IN.color;
    OUT.texCoord = IN.texCoord;
}

/////////////////////////////////Pixel Shaders///////////////////////////////////

float4 TexturePixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    
	// Premultiplied alpha requires this
    color *= IN.color[3];
	
	clip(color[3] - .001);
	return color;
}

float4 AddPixelShader_Point(a2v IN) : COLOR
{
    float4 fromTexture = tex2D(pointTextureSampler, IN.texCoord.xy);
    fromTexture[0] = (fromTexture[0] + IN.color[0] * fromTexture[3]) * IN.color[3];
    fromTexture[1] = (fromTexture[1] + IN.color[1] * fromTexture[3]) * IN.color[3];
    fromTexture[2] = (fromTexture[2] + IN.color[2] * fromTexture[3]) * IN.color[3];
    fromTexture[3] = fromTexture[3] * IN.color[3];
	clip(fromTexture[3] - .001);
	return fromTexture;
}

float4 SubtractPixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    color[0] = (color[0] - IN.color[0]) * IN.color[3];
    color[1] = (color[1] - IN.color[1]) * IN.color[3];
    color[2] = (color[2] - IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ModulatePixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    color[0] = (color[0] * IN.color[0]) * IN.color[3];
    color[1] = (color[1] * IN.color[1]) * IN.color[3];
    color[2] = (color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate2XPixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    color[0] = (2 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (2 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (2 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate4XPixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    color[0] = (4 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (4 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (4 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 InversePixelShader_Point(a2v IN) : COLOR
{
    float4 color =  tex2D(pointTextureSampler, IN.texCoord.xy);
    color[0] = (1 - color[0]) * IN.color[3];
    color[1] = (1 - color[1]) * IN.color[3];
    color[2] = (1 - color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ColorPixelShader(a2v IN) : COLOR
{
	clip(IN.color[3] - .001);
    return IN.color;
}

float4 ColorTextureAlphaPixelShader_Point(a2v IN) : COLOR
{
	float4 color = IN.color;
	float alphaFromTexture = tex2D(pointTextureSampler, IN.texCoord.xy).a;
	color[0] = color[0] * IN.color[3] * alphaFromTexture;
	color[1] = color[1] * IN.color[3] * alphaFromTexture;
	color[2] = color[2] * IN.color[3] * alphaFromTexture;
	color[3] = color[3] * alphaFromTexture;
	clip(color[3] - .001);
	return color;
}

float4 InterpolateColorPixelShader_Point(a2v IN) :COLOR
{
	float4 color = IN.color;
	color = (color[3] * tex2D(pointTextureSampler, IN.texCoord.xy)) + (1 - color[3])*(color);
	color[3] = tex2D(pointTextureSampler, IN.texCoord.xy).a;
	clip(color[3] - .001);
	return color;
}


//------------------------------Linear-------------------------------------------------------

float4 TexturePixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
	
	// Premultiplied alpha requires this
    color *= IN.color[3];
	
	clip(color[3] - .001);  
	return color;
}

float4 AddPixelShader_Linear(a2v IN) : COLOR
{
    float4 fromTexture =  tex2D(linearTextureSampler, IN.texCoord.xy);
    fromTexture[0] = (fromTexture[0] + IN.color[0] * fromTexture[3]) * IN.color[3];
    fromTexture[1] = (fromTexture[1] + IN.color[1] * fromTexture[3]) * IN.color[3];
    fromTexture[2] = (fromTexture[2] + IN.color[2] * fromTexture[3]) * IN.color[3];
    fromTexture[3] = fromTexture[3] * IN.color[3];
	clip(fromTexture[3] - .001);
	return fromTexture;
}

float4 SubtractPixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
    color[0] = (color[0] - IN.color[0]) * IN.color[3];
    color[1] = (color[1] - IN.color[1]) * IN.color[3];
    color[2] = (color[2] - IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ModulatePixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
    color[0] = (color[0] * IN.color[0]) * IN.color[3];
    color[1] = (color[1] * IN.color[1]) * IN.color[3];
    color[2] = (color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate2XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
    color[0] = (2 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (2 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (2 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 Modulate4XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
    color[0] = (4 * color[0] * IN.color[0]) * IN.color[3];
    color[1] = (4 * color[1] * IN.color[1]) * IN.color[3];
    color[2] = (4 * color[2] * IN.color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 InversePixelShader_Linear(a2v IN) : COLOR
{
    float4 color =  tex2D(linearTextureSampler, IN.texCoord.xy);
    color[0] = (1 - color[0]) * IN.color[3];
    color[1] = (1 - color[1]) * IN.color[3];
    color[2] = (1 - color[2]) * IN.color[3];
    color[3] = color[3] * IN.color[3];
	clip(color[3] - .001);
	return color;
}

float4 ColorTextureAlphaPixelShader_Linear(a2v IN) : COLOR
{
	float4 color = IN.color;
	float alphaFromTexture = tex2D(linearTextureSampler, IN.texCoord.xy).a;
	color[0] = color[0] * IN.color[3] * alphaFromTexture;
	color[1] = color[1] * IN.color[3] * alphaFromTexture;
	color[2] = color[2] * IN.color[3] * alphaFromTexture;
	color[3] = color[3] * alphaFromTexture;
	clip(color[3] - .001);
	return color;
}

float4 InterpolateColorPixelShader_Linear(a2v IN) :COLOR
{
	float4 color = IN.color;
	color = (color[3] * tex2D(linearTextureSampler, IN.texCoord.xy)) + (1 - color[3])*(color);
	color[3] = tex2D(linearTextureSampler, IN.texCoord.xy).a;
	clip(color[3] - .001);
	return color;
}


////////////////////////////////////Techniques//////////////////////////////////////////////////

technique Texture_Point
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 TexturePixelShader_Point();
    }
}

technique Add_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 AddPixelShader_Point();
	}
}

technique Subtract_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 SubtractPixelShader_Point();
	}
}

technique Modulate_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ModulatePixelShader_Point();
	}
}

technique Modulate2X_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate2XPixelShader_Point();
	}
}

technique Modulate4X_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate4XPixelShader_Point();
	}
}

technique InverseTexture_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 InversePixelShader_Point();
	}
}

technique Color_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorPixelShader();
	}
}

technique ColorTextureAlpha_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader_Point();
	}
}

technique InterpolateColor_Point
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();	
		pixelshader = compile ps_2_0 InterpolateColorPixelShader_Point();			
	}
}

//------------------------Linear-----------------------------------------



technique Texture_Linear
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 TexturePixelShader_Linear();
    }
}

technique Add_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 AddPixelShader_Linear();
	}
}

technique Subtract_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 SubtractPixelShader_Linear();
	}
}

technique Modulate_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ModulatePixelShader_Linear();
	}
}

technique Modulate2X_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate2XPixelShader_Linear();
	}
}

technique Modulate4X_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 Modulate4XPixelShader_Linear();
	}
}

technique InverseTexture_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 InversePixelShader_Linear();
	}
}

technique Color_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorPixelShader();
	}
}

technique ColorTextureAlpha_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader_Linear();
	}
}

technique InterpolateColor_Linear
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();	
		pixelshader = compile ps_2_0 InterpolateColorPixelShader_Linear();			
	}

}