sampler baseTexture : register(s0);
sampler paletteTexture : register(s1);

float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 colorData = tex2D(baseTexture, coords);
    float opacity = colorData.a;
    coords = float2(colorData.r * 0.8 - globalTime, globalTime * 0.3);
    
    return pow(tex2D(paletteTexture, coords), 3) * opacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}