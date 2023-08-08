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
void vs(in a2v IN, out v2p OUT)
{
    // Transforming our position from object space to screen space
    OUT.position = mul(IN.position, ViewProj);
    OUT.color = IN.color;
    OUT.texCoord = IN.texCoord;
}


float4 Linearize(float4 color)
{
    return float4(pow(color.rgb, 2.2), color.a);
}

#define SAMPLE(textureSampler, a2v) tex2D(textureSampler, a2v.texCoord.xy)
#define SAMPLE_LINEARIZE(textureSampler, a2v) Linearize(tex2D(textureSampler, a2v.texCoord.xy))


float4 PremultiplyAlpha(float4 textureColor, a2v IN)
{
    return textureColor *= IN.color.a;
}

float4 Add(float4 textureColor, a2v IN)
{
    textureColor.rgb += IN.color.rgb;
    textureColor.rgb *= textureColor.a;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 Subtract(float4 textureColor, a2v IN)
{
    textureColor.rgb -= IN.color.rgb;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 Modulate(float4 textureColor, a2v IN)
{
    textureColor.rgb *= IN.color.rgb;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 Modulate2X(float4 textureColor, a2v IN)
{
    textureColor.rgb *= 2 * IN.color.rgb;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 Modulate4X(float4 textureColor, a2v IN)
{
    textureColor.rgb *= 4 * IN.color.rgb;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 Inverse(float4 textureColor, a2v IN)
{
    textureColor.rgb = 1 - textureColor.rgb;
    textureColor *= IN.color.a;
    return textureColor;
}

float4 ColorTextureAlpha(float textureAlpha, a2v IN)
{
    float4 color = IN.color;
    color.rgb *= IN.color.a;
    color *= textureAlpha;
    return color;
}

float4 InterpolateColor(float4 textureColor, a2v IN)
{
    float4 color = IN.color;
    color = (color.a * textureColor) + (1 - color.a) * (color);
    color.a = textureColor.a;
    return color;
}


/////////////////////////////////Pixel Shaders///////////////////////////////////

float4 TexturePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, IN);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Add(color, IN);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Subtract(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate2X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate4X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Inverse(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ColorPixelShader(a2v IN) : COLOR
{
    clip(IN.color.a - .001);
    return IN.color;
}

float4 ColorTextureAlphaPixelShader_Point(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(pointTextureSampler, IN).a, IN);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(pointTextureSampler, IN), IN);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, IN);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Add(color, IN);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Subtract(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate2X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate4X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Inverse(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(pointTextureSampler, IN).a, IN);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point_Linearize(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(pointTextureSampler, IN), IN);
    clip(color.a - .001);
    return color;
}


//------------------------------Linear-------------------------------------------------------

float4 TexturePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, IN);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Add(color, IN);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Subtract(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate2X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate4X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Inverse(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(linearTextureSampler, IN).a, IN);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(linearTextureSampler, IN), IN);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, IN);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Add(color, IN);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Subtract(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate2X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate4X(color, IN);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Inverse(color, IN);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(linearTextureSampler, IN).a, IN);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear_Linearize(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(linearTextureSampler, IN), IN);
    clip(color.a - .001);
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


technique Texture_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 TexturePixelShader_Point_Linearize();
    }
}

technique Add_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 AddPixelShader_Point_Linearize();
    }
}

technique Subtract_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 SubtractPixelShader_Point_Linearize();
    }
}

technique Modulate_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ModulatePixelShader_Point_Linearize();
    }
}

technique Modulate2X_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 Modulate2XPixelShader_Point_Linearize();
    }
}

technique Modulate4X_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 Modulate4XPixelShader_Point_Linearize();
    }
}

technique InverseTexture_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 InversePixelShader_Point_Linearize();
    }
}

technique Color_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ColorPixelShader();
    }
}

technique ColorTextureAlpha_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader_Point_Linearize();
    }
}

technique InterpolateColor_Point_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 InterpolateColorPixelShader_Point_Linearize();
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


technique Texture_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 TexturePixelShader_Linear_Linearize();
    }
}

technique Add_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 AddPixelShader_Linear_Linearize();
    }
}

technique Subtract_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 SubtractPixelShader_Linear_Linearize();
    }
}

technique Modulate_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ModulatePixelShader_Linear_Linearize();
    }
}

technique Modulate2X_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 Modulate2XPixelShader_Linear_Linearize();
    }
}

technique Modulate4X_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 Modulate4XPixelShader_Linear_Linearize();
    }
}

technique InverseTexture_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 InversePixelShader_Linear_Linearize();
    }
}

technique Color_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ColorPixelShader();
    }
}

technique ColorTextureAlpha_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 ColorTextureAlphaPixelShader_Linear_Linearize();
    }
}

technique InterpolateColor_Linear_Linearize
{
    pass p0
    {
        vertexshader = compile vs_1_1 vs();
        pixelshader = compile ps_2_0 InterpolateColorPixelShader_Linear_Linearize();
    }

}