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

#define TECHNIQUE(name, psname) \
	technique name { pass { VertexShader = compile vs_1_1 vs(); PixelShader = compile ps_2_0 psname(); } }

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


// Techniques:

// Point filtering
TECHNIQUE(Texture_Point, TexturePixelShader_Point);
TECHNIQUE(Add_Point, AddPixelShader_Point);
TECHNIQUE(Subtract_Point, SubtractPixelShader_Point);
TECHNIQUE(Modulate_Point, ModulatePixelShader_Point);
TECHNIQUE(Modulate2X_Point, Modulate2XPixelShader_Point);
TECHNIQUE(Modulate4X_Point, Modulate4XPixelShader_Point);
TECHNIQUE(InverseTexture_Point, InversePixelShader_Point);
TECHNIQUE(Color_Point, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Point, ColorTextureAlphaPixelShader_Point);
TECHNIQUE(InterpolateColor_Point, InterpolateColorPixelShader_Point);

// Point filtering, linearize texture sampling
TECHNIQUE(Texture_Point_Linearize, TexturePixelShader_Point_Linearize);
TECHNIQUE(Add_Point_Linearize, AddPixelShader_Point_Linearize);
TECHNIQUE(Subtract_Point_Linearize, SubtractPixelShader_Point_Linearize);
TECHNIQUE(Modulate_Point_Linearize, ModulatePixelShader_Point_Linearize);
TECHNIQUE(Modulate2X_Point_Linearize, Modulate2XPixelShader_Point_Linearize);
TECHNIQUE(Modulate4X_Point_Linearize, Modulate4XPixelShader_Point_Linearize);
TECHNIQUE(InverseTexture_Point_Linearize, InversePixelShader_Point_Linearize);
TECHNIQUE(Color_Point_Linearize, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Point_Linearize, ColorTextureAlphaPixelShader_Point_Linearize);
TECHNIQUE(InterpolateColor_Point_Linearize, InterpolateColorPixelShader_Point_Linearize);

// Linear filtering
TECHNIQUE(Texture_Linear, TexturePixelShader_Linear);
TECHNIQUE(Add_Linear, AddPixelShader_Linear);
TECHNIQUE(Subtract_Linear, SubtractPixelShader_Linear);
TECHNIQUE(Modulate_Linear, ModulatePixelShader_Linear);
TECHNIQUE(Modulate2X_Linear, Modulate2XPixelShader_Linear);
TECHNIQUE(Modulate4X_Linear, Modulate4XPixelShader_Linear);
TECHNIQUE(InverseTexture_Linear, InversePixelShader_Linear);
TECHNIQUE(Color_Linear, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Linear, ColorTextureAlphaPixelShader_Linear);
TECHNIQUE(InterpolateColor_Linear, InterpolateColorPixelShader_Linear);

// Linear filtering, linearize texture sampling
TECHNIQUE(Texture_Linear_Linearize, TexturePixelShader_Linear_Linearize);
TECHNIQUE(Add_Linear_Linearize, AddPixelShader_Linear_Linearize);
TECHNIQUE(Subtract_Linear_Linearize, SubtractPixelShader_Linear_Linearize);
TECHNIQUE(Modulate_Linear_Linearize, ModulatePixelShader_Linear_Linearize);
TECHNIQUE(Modulate2X_Linear_Linearize, Modulate2XPixelShader_Linear_Linearize);
TECHNIQUE(Modulate4X_Linear_Linearize, Modulate4XPixelShader_Linear_Linearize);
TECHNIQUE(InverseTexture_Linear_Linearize, InversePixelShader_Linear_Linearize);
TECHNIQUE(Color_Linear_Linearize, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Linear_Linearize, ColorTextureAlphaPixelShader_Linear_Linearize);
TECHNIQUE(InterpolateColor_Linear_Linearize, InterpolateColorPixelShader_Linear_Linearize);