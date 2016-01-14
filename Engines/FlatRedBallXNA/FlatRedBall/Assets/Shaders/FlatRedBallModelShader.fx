// Matrices
float4x4 ViewProj;
float4x4 World;

// Shared variables
shared float2 PixelSize;
shared float4x4 InvViewProj;
shared float NearClipPlane;
shared float FarClipPlane;
shared float3 CameraPosition;
shared float2 ViewportSize;

// Texture Sampler
bool TextureEnabled = false;

texture2D DiffuseTexture;

sampler TextureSampler = sampler_state
{
   Texture = (DiffuseTexture);
};

float3 DiffuseColor = float3(1,1,1);


// Structures
struct VS_IN
{
    float4 position : POSITION0;
    float2 texcoord : TEXCOORD0;
    float3 normal : NORMAL0;
};

struct VS_OUT
{
    float4 position : POSITION0;
    float2 texcoord : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float depth : TEXCOORD2;
    float3 worldPosition : TEXCOORD3;
};


struct PS_IN_COLOR
{
	float2 texcoord : TEXCOORD0;
};

struct PS_IN_NORMAL
{
	float3 normal : TEXCOORD1;
};

struct PS_IN_DEPTH
{
	float depth : TEXCOORD2;
};

struct PS_IN_POSITION
{
	float3 worldPosition : TEXCOORD3;
};


// Vertex Shaders
VS_OUT vs_main(in VS_IN Input)
{
	VS_OUT Output = (VS_OUT)0;
	
	Output.position = mul( Input.position, mul( World, ViewProj ) );
	Output.texcoord = Input.texcoord;
	Output.normal = normalize( mul( Input.normal, World ) - mul( float3(0,0,0), World ) );
	Output.depth = Output.position.z / Output.position.w;
	Output.worldPosition = Output.position;
	
	return Output;
}

// Pixel Shaders
float4 ps_color(in PS_IN_COLOR Input) : COLOR
{
	if (TextureEnabled)
	{
		return tex2D( TextureSampler, Input.texcoord ) * float4( DiffuseColor, 1.0f );
	}
	else
	{
		return float4( DiffuseColor, 1.0f );
	}
}

float4 ps_normals(in PS_IN_NORMAL Input) : COLOR
{
	return float4( normalize( Input.normal.xyz ), 1.0f );
}

float4 ps_depth(in PS_IN_DEPTH Input) : COLOR
{
	return Input.depth;
}

float4 ps_position(in PS_IN_POSITION Input) : COLOR
{
	return float4(Input.worldPosition, 1.0f);
}


// Techniques
technique RenderColor
{
	pass p0
	{
		VertexShader = compile vs_1_1 vs_main();
		PixelShader = compile ps_2_0 ps_color();
	}
};

technique RenderPosition
{
	pass p0
	{
		VertexShader = compile vs_1_1 vs_main();
		PixelShader = compile ps_2_0 ps_position();
	}
};

technique RenderNormals
{
	pass p0
	{
		VertexShader = compile vs_1_1 vs_main();
		PixelShader = compile ps_2_0 ps_normals();
	}
};

technique RenderDepth
{
	pass p0
	{
		VertexShader = compile vs_1_1 vs_main();
		PixelShader = compile ps_2_0 ps_depth();
	}
};