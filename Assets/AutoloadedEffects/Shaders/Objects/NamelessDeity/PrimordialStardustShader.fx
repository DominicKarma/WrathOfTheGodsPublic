sampler fogNoiseTexture : register(s0);
sampler accentNoiseTexture : register(s1);
sampler uvOffsetNoiseTexture : register(s2);

float globalTime;
float greenBias;
float scrollSpeed;
float2 scrollOffset;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float CalculateOpacity(float distanceFromCenterSqr, float time, float2 coords)
{
    float2 zoomedCoords = coords * 6;
    float inputNoise = tex2D(uvOffsetNoiseTexture, zoomedCoords * 0.27 + float2(0, time * 0.9)) * 0.03;
    float edgeCutIn = tex2D(uvOffsetNoiseTexture, zoomedCoords * 0.3 + float2(0, time * 1.87) + inputNoise) * 0.4;
    return InverseLerp(0.5, 0.28, distanceFromCenterSqr - edgeCutIn);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate time based on scroll speed.
    float time = globalTime * scrollSpeed;
    
    // Calculate the distance to the center of the field. This is magnified a bit for intensity purposes in later equations.
    float2 coordsNormalizedToCenter = coords * 2 - 1;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    float fogOpacity = CalculateOpacity(distanceFromCenterSqr, time, coords);
    
    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.2;
    float2 sphereCoords = coords * lerp(spherePinchFactor, 1, 0.5) + float2(time * 0.9, 0) + scrollOffset;
    
    // Calculate the fog brightness texture from the sphere coordinates.
    float fogCoordsOffset = tex2D(fogNoiseTexture, sphereCoords).r * 0.21 - time * 0.3;
    float2 fogCoords = sphereCoords + float2(fogCoordsOffset, 0);
    float fogBrightnessTexture1 = tex2D(fogNoiseTexture, fogCoords).r;
    float fogBrightnessTexture2 = tex2D(fogNoiseTexture, fogCoords * 0.8).r;
    float fogBrightnessTexture3 = tex2D(fogNoiseTexture, fogCoords * 0.51).r;
    float4 fogBrightnessTexture4 = pow(tex2D(uvOffsetNoiseTexture, fogCoords * 1.51), 3) * 5;
    
    // Calculate the fog color, using noise textures to have variance between blues, violets, and greens.
    float3 fogColor = lerp(0, float3(0.12, 0.011, 0.129), fogBrightnessTexture1);
    fogColor += float3(0.32, 0.28, 0.67) * fogBrightnessTexture2;
    fogColor += float4(0.9 + fogBrightnessTexture2 * 2, 0.94 + greenBias, 0.99 - greenBias, 1) * fogBrightnessTexture4;
    fogColor -= fogBrightnessTexture3 * 1.1;
    
    return float4(fogColor * 0.4, 1) * sampleColor * fogOpacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}