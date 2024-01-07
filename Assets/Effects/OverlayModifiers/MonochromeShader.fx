sampler baseTexture : register(s0);

float contrastInterpolant;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(baseTexture, coords);
    
    // Snap the color to a nearby value to posterize it.
    float precision = 0.4;
    float4 posterizedColor = float4(round(baseColor.rgb / baseColor.a / precision) * precision, 1) * baseColor.a;
    float4 highlightedColor = sampleColor * baseColor.a + posterizedColor;
    
    return lerp(highlightedColor, sampleColor * baseColor.a, 1 - contrastInterpolant);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}