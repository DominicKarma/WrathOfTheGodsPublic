sampler baseTexture : register(s0);

float globalTime;
float swapHarshness;

float OverlayBlend(float a, float b)
{
    if (a < 0.5)
        return a * b * 2;
    
    return 1 - (1 - a) * (1 - b) * 2;
}

float3 OverlayBlend(float3 a, float3 b)
{
    return float3(OverlayBlend(a.r, b.r), OverlayBlend(a.g, b.g), OverlayBlend(a.b, b.b));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    float3 goldPixelColor = color.rgb;
    float3 goldFactor = float3(1, 0.67, 0);
    return lerp(color * sampleColor, float4(OverlayBlend(color.rgb, goldFactor), 1) * sampleColor * color.a, swapHarshness);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}