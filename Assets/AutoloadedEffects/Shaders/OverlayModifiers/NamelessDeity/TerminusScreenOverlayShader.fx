sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

// !!! Screen shader, do not delete the above parameters !!!

float darknessInterpolantStart;
float darknessInterpolantEnd;
float darknessInterpolantNoiseFactor;
float2 noiseZoom;
float2 noiseScrollOffset;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the base color for the screen.
    float4 baseColor = tex2D(baseTexture, coords);
    
    // Calculate noise. This is used to give variance to the darkness.
    float offsetNoise = tex2D(noiseTexture, coords * noiseZoom + noiseScrollOffset) * darknessInterpolantNoiseFactor;
    
    // Calculate the darkness interpolant. This incorporates the noise from above.
    float distanceFromCenter = distance(coords, uTargetPosition);
    float darknessInterpolant = InverseLerp(darknessInterpolantStart, darknessInterpolantEnd, distanceFromCenter + offsetNoise) * uIntensity;
    
    // Approach a dark color based on the interpolant.
    return lerp(baseColor, float4(0, 0, 0, 1), darknessInterpolant);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}