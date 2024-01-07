sampler noiseTexture1 : register(s1);
sampler noiseTexture2 : register(s2);
sampler edgeDistortionNoise : register(s3);

float generalOpacity;
float globalTime;
float scale;
float brightness;
float3 supernovaColor1;
float3 supernovaColor2;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise values for the supernova.
    float2 noiseCoords1 = (coords - 0.5) * scale * 0.2 + 0.5 + float2(globalTime * 0.14, 0);
    float2 noiseCoords2 = (coords - 0.5) * scale * 0.32 + 0.5 + float2(globalTime * -0.08, 0);
    float2 noiseCoords3 = (coords - 0.5) * scale * 0.14 + 0.5 + float2(0, globalTime * -0.06);
    float4 noiseColor1 = tex2D(noiseTexture1, noiseCoords1) * float4(supernovaColor1, 1) * sampleColor * 1.5;
    float4 noiseColor2 = tex2D(noiseTexture1, noiseCoords2) * float4(supernovaColor2, 1) * sampleColor * 1.5;
    float4 noiseColor3 = tex2D(noiseTexture2, frac(noiseCoords3)) * sampleColor;
    
    // Calculate edge fade values. These are used to make the supernova naturally fade at those edges.
    float2 edgeDistortion = tex2D(edgeDistortionNoise, noiseCoords1 * 2.5).rb * 0.0093;
    float distanceFromCenter = length(coords + edgeDistortion - 0.5) * 1.414;
    float distanceFade = InverseLerp(0.45, 0.39, distanceFromCenter);
    
    float4 result = (noiseColor1 + noiseColor2) * sampleColor.a;
    result.a = sampleColor.a * 1.25;
    return ((result - noiseColor3 * 0.15) * brightness + (brightness - 1) * 0.25) * distanceFade * generalOpacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}