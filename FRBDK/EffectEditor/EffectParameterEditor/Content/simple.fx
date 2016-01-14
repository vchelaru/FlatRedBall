int gp : SasGlobal
<
  int3 SasVersion = {1,0,0};
  bool SasUiVisible = false;
>;

float4x4 World
<
  bool SasUiVisible = false;
>;

float4x4 View
<
  bool SasUiVisible = false;
>;

float4x4 Projection
<
  bool SasUiVisible = false;
>;

float3 Color
<
  string SasUiDescription = "The color of the object";
>;

float4 VertexShaderFunction(float4 Position : POSITION0) : POSITION0
{
    return mul(Position,mul(World,mul(View,Projection)));
}

float4 PixelShaderFunction() : COLOR0
{
    return float4(Color,1);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_1_1 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
