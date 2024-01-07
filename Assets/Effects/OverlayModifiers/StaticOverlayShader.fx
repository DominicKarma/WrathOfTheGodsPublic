sampler baseTexture : register(s0);
sampler staticTexture : register(s1);

float globalTime;
float staticInterpolant;
float staticZoomFactor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float staticOverlay = tex2D(staticTexture, coords * (staticZoomFactor + 1) * 0.5 + globalTime * 21.494754);
    float4 color = tex2D(baseTexture, coords);
    float staticAdditive = pow(staticOverlay, 2) * color.a * sampleColor.a;
    float4 staticColor = color * sampleColor + staticAdditive * 0.45;
    return lerp(staticColor, float4(staticAdditive, staticAdditive, staticAdditive, 1), staticInterpolant);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}