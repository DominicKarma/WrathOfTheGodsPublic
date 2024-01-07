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

float seamAngle;
float seamSlope;
float seamBrightness;
float warpIntensity;
bool offsetsAreAllowed;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (from - to));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate values for the seam and the offset that both sides are shoved by.
    // The 1 - coords.y thing are there to account for the fact that the Y coordinate is inverted.
    float seamLengthFactor = uIntensity;
    float signedDistanceToLine = (seamSlope * (coords.x - 0.5) + (1 - coords.y) - 0.5) / sqrt(seamSlope * seamSlope + 1);
    float2 orthogonalSeamDirection = float2(sin(seamAngle), sin(seamAngle - 1.57));
    float downwardCutoff = InverseLerp(seamLengthFactor, seamLengthFactor * 1.1, coords.y);
    
    // Apply color effects. These are strongest at the seam, where the colors are basically a pure, vibrant white.
    float4 color = tex2D(baseTexture, frac(coords + orthogonalSeamDirection * sqrt(seamLengthFactor) * offsetsAreAllowed * -0.015));
    float2 warpOffset = tex2D(noiseTexture, coords * 0.8 + float2(uTime * 0.04, 0)).rg - 0.5;
    float4 scrollNoise = tex2D(noiseTexture, coords * 0.3 + float2(uTime * -0.1, 0) + warpOffset * warpIntensity);
    
    return color + seamBrightness * seamLengthFactor * downwardCutoff / abs(signedDistanceToLine) * scrollNoise * uOpacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}