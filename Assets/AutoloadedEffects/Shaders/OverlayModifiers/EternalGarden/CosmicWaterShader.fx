sampler baseTexture : register(s0);
sampler cosmicTexture : register(s1);
sampler samplingNoiseTexture : register(s2);

float globalTime;
float2 screenPosition;
float2 targetSize;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 worldStableCoords = coords - screenPosition / targetSize;
    float4 color = tex2D(baseTexture, coords);
    float brightness = InverseLerp(0, 0.35, saturate(dot(color.rgb, 0.333) * 1.25));
    
    // Use FBM to calculate complex warping offsets in the noise texture that will be used to create the fog noise.
    float noiseAmplitude = 0.051 + pow(brightness, 1.4) * 0.16;
    float noiseScrollTime = globalTime * 0.4;
    float2 noiseOffset = 0;
    float2 noiseZoom = 3.187;
    for (float i = 0; i < 2; i++)
    {
        float2 scrollOffset = float2(noiseScrollTime * 0.67 - i * 0.838, noiseScrollTime * 1.21 + i * 0.6125) * noiseZoom.y * 0.04;
        noiseOffset += (tex2D(samplingNoiseTexture, worldStableCoords * noiseZoom + scrollOffset) - 0.5) * noiseAmplitude;
        noiseZoom *= 2;
        noiseAmplitude *= 0.5;
    }
    
    // Combine samples from the cosmic texture for a final color.
    float4 cosmicColor1 = pow(tex2D(cosmicTexture, worldStableCoords * 3 + float2(0, globalTime * (noiseOffset.x * 0.0007 + 0.04)) + noiseOffset), 3) * 3;
    float4 cosmicColor2 = pow(tex2D(cosmicTexture, worldStableCoords * 4 + float2(0, globalTime * 0.06) - noiseOffset), 0.6) * float4(1.2, 0.6, 0.97, 1);
    float4 cosmicColor = (cosmicColor1 + cosmicColor2) * (1 + abs(noiseOffset.x) * 10);
    
    // Make various color components in the cosmic color vary based on noise and original brightness.
    cosmicColor.rgb *= float3(0.9, 1.25 + noiseOffset.y * 10, 1 - pow(brightness, 2) * 0.95);
    
    return saturate(color + cosmicColor * color.a) * sampleColor * (0.45 + brightness * 0.55);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}