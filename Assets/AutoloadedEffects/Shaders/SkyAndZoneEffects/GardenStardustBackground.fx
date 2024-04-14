sampler noiseTexture : register(s0);

float globalTime;
float zoom;
float2 parallaxOffset;
float3 brightColor;
float3 spaceColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float noiseOffsetX = tex2D(noiseTexture, coords * zoom * 1.1 + globalTime * 0.02 + parallaxOffset).r;
    float noiseOffsetY = tex2D(noiseTexture, coords * zoom * 1.2 - globalTime * 0.003 + parallaxOffset).r;
    float result = tex2D(noiseTexture, coords * zoom + float2(noiseOffsetX, noiseOffsetY) * 0.2 + parallaxOffset).r;
    float3 color = lerp(spaceColor, brightColor * 2, pow(result, 1.6)) * pow(result, 2);
    
    return float4(color, 1) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
