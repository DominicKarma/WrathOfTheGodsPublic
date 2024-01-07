sampler baseTexture : register(s0);
sampler uvOffsetingNoiseTexture : register(s1);

float globalTime;
float noiseOffsetFactor;
float noiseOverlayIntensityFactor;
float horrorInterpolant;
float2 zoom;
float3 noiseOverlayColor;
float3 eyeColor;

// Refer to the screen shader's C# code to find out more about this. It's far more efficient to precompute it on the CPU than to have
// the matrix remade for every single pixel on the screen based on an input float.
float4x4 contrastMatrix;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 zoomedCoords = (coords - 0.5) * zoom + 0.5;
    
    // Calculate values for the warp noise.
    float warpAngle = tex2D(uvOffsetingNoiseTexture, coords * 3.2 + float2(globalTime * 0.5, 0)).r * 16;
    float2 warpNoiseOffset = float2(cos(warpAngle), sin(warpAngle)) * noiseOffsetFactor;
    
    // Calculate the distance of the given pixel from the head of the player.
    float distanceFromHead = distance(zoomedCoords, float2(0.5, 0.46));
    float distanceFromHeadInterpolant = InverseLerp(0.05, 0.09, distanceFromHead + warpNoiseOffset.x * 0.01);
    
    // Make the warp noise weaker near the head.
    warpNoiseOffset *= 1 - distanceFromHeadInterpolant * 0.9;
    
    // Calculate the noise overlay intensity based on the luminosity of the offset color. This is used to give custom color variance to everything.
    float noiseOverlayIntensity = dot(tex2D(baseTexture, coords + warpNoiseOffset * 0.013).rgb, float3(0.3, 0.59, 0.11));
    
    // Calculate the darkness of the given pixel. This is strongest nearest to the head.
    float darkInterpolant = InverseLerp(0.05, 0.086, distanceFromHead + warpNoiseOffset.x * 0.01);
    
    // Calculate the base and contrasted colors.
    float4 baseColor = tex2D(baseTexture, coords - warpNoiseOffset * 0.006) * 1.33;
    float4 contrastedColor = mul(baseColor, contrastMatrix);
    
    // Combine the contrast and base color based on the noise overlay.
    float4 result = lerp(baseColor, contrastedColor, noiseOverlayIntensity) * float4(darkInterpolant, darkInterpolant, darkInterpolant, 1);
    
    // Additively apply the noise overlay colors.
    result += float4(noiseOverlayColor, 1) * lerp(0.03, 1, darkInterpolant) * noiseOverlayIntensity * noiseOverlayIntensityFactor;
    
    // C calculate and apply the eye colors.
    float brightEyeInterpolant = InverseLerp(0.016, 0.007, distance((zoomedCoords - 0.5) * float2(1.6, 3.2) + 0.5, float2(0.5, 0.415)) - noiseOverlayIntensity * 0.2) * 2;    
    result += float4(eyeColor, 1) * pow(horrorInterpolant, 2) * brightEyeInterpolant;
    
    return lerp(baseColor, result, horrorInterpolant) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}