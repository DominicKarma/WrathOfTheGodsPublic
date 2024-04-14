sampler baseTexture : register(s0);

float splitIntensity;
float2 impactPoint;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    // Split colors into their composite RGB values.
    float splitDistance = splitIntensity * 0.048 / (distance(coords, impactPoint) + 1);
    color.r = tex2D(baseTexture, coords + float2(-0.707, -0.707) * splitDistance).r;
    color.g = tex2D(baseTexture, coords + float2(0.707, -0.707) * splitDistance).g;
    color.b = tex2D(baseTexture, coords + float2(0, 1) * splitDistance).b;
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}