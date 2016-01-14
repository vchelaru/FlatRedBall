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

technique InverseTexture
{
	pass p0
	{
		vertexshader = compile vs_1_1 vs();
		pixelshader = compile ps_2_0 InversePixelShader();
	}
}
