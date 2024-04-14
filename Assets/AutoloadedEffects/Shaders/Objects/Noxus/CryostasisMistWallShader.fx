sampler baseTexture : register(s0);
sampler mistNoiseTexture : register(s1);

float globalTime;
float mistInterpolant;
float2 textureSize;
float2 worldPosition;
float3 mistColor;

float2 AspectRatioCorrect(float2 coords)
{
    return (coords - 0.5) * float2(textureSize.x / textureSize.y, 1) + 0.5;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Ensure that the texture coordinates are stabilized.
    coords.y *= textureSize.y / textureSize.x;
    
    // Use FBM to calculate complex warping offsets in the noise texture that will be used to create the fog noise.
    float2 noiseOffset = 0;
    float noiseAmplitude = 0.03;
    float2 noiseZoom = 3.187;
    float2 worldAdjustedCoords = coords + worldPosition * -0.00014;
    for (float i = 0; i < 2; i++)
    {
        float2 scrollOffset = float2(globalTime * 0.185 - i * 0.338, globalTime * -0.533 + i * 0.787) * noiseZoom.y * 0.04;
        noiseOffset += (tex2D(mistNoiseTexture, worldAdjustedCoords * noiseZoom + scrollOffset) - 0.5) * noiseAmplitude;
        noiseZoom *= 2;
        noiseAmplitude *= 0.5;
    }
    
    // Calculate warp noise values.
    float sourceDistanceWarp = tex2D(mistNoiseTexture, worldAdjustedCoords * 3 - float2(0, globalTime * 0.67));
    
    // Calculate noise values.
    noiseOffset.x += globalTime * 0.11;
    float noise = lerp(tex2D(mistNoiseTexture, worldAdjustedCoords * 0.61 + noiseOffset).r, 0.5, 0.4);
    float circleEdgeNoise = tex2D(mistNoiseTexture, worldAdjustedCoords * float2(1.7, 5) + noiseOffset).r;
    float darknessEdgeNoise = tex2D(mistNoiseTexture, worldAdjustedCoords * 4.1 + noiseOffset * 3).r;
    
    // Calculate the base color and its brightness.
    float4 baseColor = tex2D(baseTexture, coords);
    float brightness = dot(baseColor.rgb, float3(0.3, 0.59, 0.11));
    
    // Combine the base color with the fog.
    float4 result = lerp(baseColor, noise * float4(mistColor, 1), pow(mistInterpolant, 1.25) * 0.96);
    result += result.a * sampleColor.a * length(mistColor) * 0.3 / darknessEdgeNoise;
    
    float frontOpacity = smoothstep(1, 0.9, coords.x + circleEdgeNoise * 0.11);
    float backOpacity = smoothstep(0.12, 0.4, coords.x + circleEdgeNoise * 0.16);
    float opacity = frontOpacity * backOpacity;
    
    return result * opacity * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}