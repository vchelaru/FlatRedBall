float4x4 World : WORLD;
shared float4x4 View : VIEW;
shared float4x4 Projection : PROJECTION;

// Lighting
float3 DiffuseColor = float3(1.0f, 1.0f, 1.0f);
float3 SpecularColor = float3(1.0f, 1.0f, 1.0f);
float3 EmissiveColor = float3(0.0f, 0.0f, 0.0f);
float SpecularPower = 256.0f;

shared bool LightingEnable = false;

// Ambient Light
shared bool   AmbLight0Enable;
shared float3 AmbLight0DiffuseColor;

// Directional Lights
shared bool   DirLight0Enable = false;
shared bool   DirLight1Enable = false;
shared bool   DirLight2Enable = false;
shared float3 DirLight0Direction;
shared float3 DirLight1Direction;
shared float3 DirLight2Direction;
shared float3 DirLight0DiffuseColor;
shared float3 DirLight1DiffuseColor;
shared float3 DirLight2DiffuseColor;
shared float3 DirLight0SpecularColor;
shared float3 DirLight1SpecularColor;
shared float3 DirLight2SpecularColor;

// Point light
shared bool   PointLight0Enable;
shared float3 PointLight0Position;
shared float3 PointLight0DiffuseColor;
shared float3 PointLight0SpecularColor;
shared float PointLight0Range;

// Shared variables
shared float2 PixelSize;
shared float4x4 InvViewProj;
shared float NearClipPlane;
shared float FarClipPlane;
shared float3 CameraPosition;
shared float2 ViewportSize;

// Texturing
bool TextureEnabled = false;
texture2D DiffuseTexture;

sampler TextureSampler = sampler_state
{
   Texture = (DiffuseTexture);
};



struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float3 Normal : TEXCOORD1;
    float3 ViewVector : TEXCOORD2;
    float3 WorldPosition : TEXCOORD3;
    float Depth : TEXCOORD4;
};


static inline float3 ComputeDiffuseComponent(
	float3 normal, float3 ambient, float3 worldPosition)
	{
	
	float PointLight0Intensity = saturate( 1.0f - distance( worldPosition, PointLight0Position ) / PointLight0Range );
	float3 PointLight0Direction = normalize( worldPosition - PointLight0Position );
	
		return saturate((
			  ( DirLight0Enable ? saturate(dot( -DirLight0Direction, normal) * DirLight0DiffuseColor) : 0 ) +
			  ( DirLight1Enable ? saturate(dot( -DirLight1Direction, normal) * DirLight1DiffuseColor) : 0 ) +
			  ( DirLight2Enable ? saturate(dot( -DirLight2Direction, normal) * DirLight2DiffuseColor) : 0 ) +
			  ( PointLight0Enable ? saturate(dot( -PointLight0Direction, normal) * PointLight0Intensity * PointLight0DiffuseColor) : 0) +
			  ambient));
			
	}

VertexShaderOutput VertexShaderDefault(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = mul( input.Position, mul( World, mul( View, Projection ) ) );
	output.TexCoord = input.TexCoord;
	output.Normal = normalize( mul( input.Normal, World ));
	output.Depth = output.Position.z / output.Position.w;

	output.Color = saturate( float4( DiffuseColor, 1.0f ) );

    return output;
}

VertexShaderOutput VertexShaderAmbient(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	
	output.Color = float4(saturate((AmbLight0Enable ? AmbLight0DiffuseColor : 0)), 1.0f);
	
	return output;
}

VertexShaderOutput VertexShaderDiffuse(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	output.Normal = normalize( mul( input.Normal, World));
	output.WorldPosition = mul( input.Position, World );
	
	output.Color = float4(
			  ComputeDiffuseComponent(output.Normal, 0, output.WorldPosition), 1.0f);
			  
	return output;
}

VertexShaderOutput VertexShaderSpecular(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	output.Normal = mul( input.Normal, World);
	output.WorldPosition = mul( input.Position, World );
	output.ViewVector = CameraPosition - mul(input.Position, World);
	
	output.Color = float4(0.0f, 0.0f, 0.0f, 0.0f);
	
	return output;
}

VertexShaderOutput VertexShaderAmbientDiffuse(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	output.Normal = normalize( mul( input.Normal, World));
	output.WorldPosition = mul( input.Position, World );
	output.ViewVector = CameraPosition - mul(input.Position, World);
	
	output.Color = float4(
		ComputeDiffuseComponent(output.Normal, 
		(AmbLight0Enable ? AmbLight0DiffuseColor : 0), output.WorldPosition), 1.0f);
		
	return output;
}

VertexShaderOutput VertexShaderAmbientSpecular(VertexShaderInput input)
{
	
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	output.Normal = normalize(mul( input.Normal, World));
	output.WorldPosition = mul( input.Position, World );
	output.ViewVector = CameraPosition - mul(input.Position, World);
	
	output.Color = float4((AmbLight0Enable ? AmbLight0DiffuseColor : 0), 1.0f);
	
	return output;
}

VertexShaderOutput VertexShaderDiffuseSpecular(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, mul(World, mul(View, Projection)));
	output.TexCoord = input.TexCoord;
	output.Normal = normalize(mul( input.Normal, World));
	output.WorldPosition = mul( input.Position, World );
	output.ViewVector = CameraPosition - mul(input.Position, World);
	
	output.Color = float4(ComputeDiffuseComponent(output.Normal, 0, output.WorldPosition), 1.0f);
	
	return output;
}

static inline float3 ComputeSpecularComponent(float3 normal, float3 viewVector, float3 lightDir)
{
	float3 halfVector = normalize(lightDir + normalize(viewVector));
	return pow(
			saturate(
				dot(normal, halfVector))
			,50);
}

static inline float3 ComputeTotalSpecular(float3 normal, float3 viewVector, float3 worldPosition)
{
	float3 totalSpecular = 0;
	float PointLight0Intensity = saturate( 1.0f - distance( worldPosition, PointLight0Position ) / PointLight0Range );
	float3 PointLight0Direction = normalize( worldPosition - PointLight0Position );
	
	totalSpecular += (DirLight0Enable ? ComputeSpecularComponent(normal, viewVector, -DirLight0Direction) * DirLight0SpecularColor : 0);
	
	totalSpecular += (DirLight1Enable ? ComputeSpecularComponent(normal, viewVector, -DirLight1Direction) * DirLight1SpecularColor : 0);
	
	totalSpecular += (DirLight2Enable ? ComputeSpecularComponent(normal, viewVector, -DirLight2Direction) * DirLight2SpecularColor : 0);

	totalSpecular += (PointLight0Enable ? ComputeSpecularComponent(normal, viewVector, -PointLight0Direction) * PointLight0Intensity * PointLight0SpecularColor : 0);

	return totalSpecular;
}

static inline float4 CombineWithTexture(float4 color, float2 texCoord)
{
	float4 textureValue = tex2D(TextureSampler, texCoord);
	return float4( saturate(
		color.xyz * textureValue.xyz), textureValue.w);	
}

float4 PixelShaderDefault(VertexShaderOutput input) : COLOR0
{
	if (TextureEnabled)
	{
		float4 textureValue = tex2D(TextureSampler, input.TexCoord);
		return float4( saturate(
			DiffuseColor * textureValue.xyz +
			 EmissiveColor ), textureValue.w );
		//return textureValue;
	}
	else
	{
		return float4( saturate(DiffuseColor + EmissiveColor), 1.0f );
	}
}


float4 PixelShaderAmbientDiffuse(VertexShaderOutput input) : COLOR
{
	if (TextureEnabled)
	{
		return CombineWithTexture(input.Color,input.TexCoord);
	}
	else
	{
		return float4(saturate ( input.Color.xyz * DiffuseColor + EmissiveColor), 1.0f);
	}
}

float4 PixelShaderSpecular(VertexShaderOutput input) : COLOR
{
	float3 surfaceNormal = normalize(input.Normal);
	
	float3 totalSpecular = ComputeTotalSpecular(surfaceNormal, input.ViewVector, input.WorldPosition);
	
	if (TextureEnabled)
	{
		//float4 textureValue = tex2D(TextureSampler, input.TexCoord);
		//return float4(saturate((totalSpecular > .3f ? (textureValue + totalSpecular) : (totalSpecular * textureValue))), textureValue.w); 
		return float4(saturate(totalSpecular + EmissiveColor), 1.0f);
	}
	else
	{
		return float4(saturate(totalSpecular + EmissiveColor), 1.0f);
	}
	
}

float4 PixelShaderAmbientSpecular(VertexShaderOutput input) : COLOR
{
	float3 surfaceNormal = normalize(input.Normal);
	
	float3 totalSpecular = ComputeTotalSpecular(surfaceNormal, input.ViewVector, input.WorldPosition);
	
	if (TextureEnabled)
	{
		float4 textureValue = tex2D(TextureSampler, input.TexCoord);
		return float4(saturate(totalSpecular + (textureValue * input.Color * DiffuseColor) + EmissiveColor).xyz, textureValue.w); 
	}
	else
	{
		return float4(saturate(totalSpecular + input.Color * DiffuseColor + EmissiveColor), 1.0f);
	}
}

float4 PixelShaderColor(in float2 TexCoord : TEXCOORD0 ) : COLOR
{
	if (TextureEnabled)
	{
		return tex2D( TextureSampler, TexCoord ) * float4( DiffuseColor, 1.0f );
	}
	else
	{
		return float4( DiffuseColor, 1.0f );
	}
}

float4 PixelShaderNormals(in float3 Normal : TEXCOORD1) : COLOR
{
	return float4( normalize( Normal ), 1.0f );
}

float4 PixelShaderDepth(in float Depth : TEXCOORD4) : COLOR
{
	return Depth;
}

float4 PixelShaderPosition(in float3 WorldPosition : TEXCOORD3) : COLOR
{
	return float4( WorldPosition, 1.0f );
}
    
    
technique Default
{
    pass p0
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_1_1 VertexShaderDefault();
        PixelShader = compile ps_2_0 PixelShaderDefault();
    }
}

technique AmbientOnly
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderAmbient();
		PixelShader = compile ps_2_0 PixelShaderAmbientDiffuse();
	}
}

technique DiffuseOnly
{
	pass p0
	{
        VertexShader = compile vs_1_1 VertexShaderDiffuse();
        PixelShader = compile ps_2_0 PixelShaderAmbientDiffuse();
	}
};

technique SpecularOnly
{
	pass p0
	{
        VertexShader = compile vs_1_1 VertexShaderSpecular();
        PixelShader = compile ps_2_0 PixelShaderSpecular();
	}
};

technique AmbientAndDiffuse
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderAmbientDiffuse();
		PixelShader = compile ps_2_0 PixelShaderAmbientDiffuse();
	}
};

technique AmbientAndSpecular
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderAmbientSpecular();
		PixelShader = compile ps_2_0 PixelShaderAmbientSpecular();
	}
};

technique DiffuseAndSpecular
{
	pass p0
	{
        VertexShader = compile vs_1_1 VertexShaderDiffuseSpecular();
        PixelShader = compile ps_2_0 PixelShaderAmbientSpecular();
	}
};

technique AmbientDiffuseSpecular
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderAmbientDiffuse();
		PixelShader = compile ps_2_0 PixelShaderAmbientSpecular();
	}
};

technique RenderColor
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderDefault();
		PixelShader = compile ps_1_1 PixelShaderColor();
	}
};

technique RenderPosition
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderDefault();
		PixelShader = compile ps_2_0 PixelShaderPosition();
	}
};

technique RenderNormals
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderDefault();
		PixelShader = compile ps_2_0 PixelShaderNormals();
	}
};

technique RenderDepth
{
	pass p0
	{
		VertexShader = compile vs_1_1 VertexShaderDefault();
		PixelShader = compile ps_2_0 PixelShaderDepth();
	}
};