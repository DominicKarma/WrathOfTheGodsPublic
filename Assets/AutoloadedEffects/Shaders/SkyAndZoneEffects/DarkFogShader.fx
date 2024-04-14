sampler fogNoiseTexture : register(s1);

float uOpacity;
float globalTime;
float fogTravelDistance;
float2 fogCenter;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Use FBM to calculate complex warping offsets in the noise texture that will be used to create the fog noise.
    float2 noiseOffset = 0;
    float noiseAmplitude = 0.03;
    float2 noiseZoom = 3.187;
    for (float i = 0; i < 2; i++)
    {
        float2 scrollOffset = float2(globalTime * 0.285 - i * 0.338, globalTime * -1.233 + i * 0.787) * noiseZoom.y * 0.04;
        noiseOffset += (tex2D(fogNoiseTexture, coords * noiseZoom + scrollOffset) - 0.5) * noiseAmplitude;
        noiseZoom *= 2;
        noiseAmplitude *= 0.5;
    }
    
    // Calculate how far the fog should be going.
    float sourceDistanceWarp = tex2D(fogNoiseTexture, coords * 3 - float2(0, globalTime * 0.33));
    float distanceFromSource = distance(coords + noiseOffset, fogCenter) + (sourceDistanceWarp - 0.5) * 0.13;
    float distanceFadeInterpolant = InverseLerp(fogTravelDistance, fogTravelDistance * 0.8, distanceFromSource);
    
    noiseOffset.x += globalTime * 0.04;
    float noise = tex2D(fogNoiseTexture, coords * 2 + noiseOffset) * distanceFadeInterpolant;
    return sampleColor * noise * 1.26;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}