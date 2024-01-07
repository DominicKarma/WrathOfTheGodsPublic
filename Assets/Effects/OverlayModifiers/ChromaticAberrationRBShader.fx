sampler baseTexture : register(s0);

float splitIntensity;
float maxOpacity;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    // Split colors into their composite RB values. Green is unaffected.
    float splitDistance = splitIntensity * 0.09;
    float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    color.r += tex2D(baseTexture, coords + float2(-1, -0.2) * splitDistance).r * lerp(0.12, maxOpacity, 1 - color.a);
    color.b += tex2D(baseTexture, coords + float2(1, 0.2) * splitDistance).g * lerp(0.12, maxOpacity, 1 - color.a);
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}