sampler maskTexture : register(s0);
sampler baseTexture : register(s1);

float2 zoomFactor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return any(tex2D(maskTexture, coords)) * tex2D(baseTexture, coords * zoomFactor);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}