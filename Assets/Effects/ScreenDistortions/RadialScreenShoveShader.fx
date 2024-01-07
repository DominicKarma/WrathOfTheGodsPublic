sampler baseTexture : register(s0);
sampler uImage1 : register(s1);
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

float blurPower;
float distortionPower;
float2 distortionCenter;
float pulseTimer;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 distortionOffset = (distortionCenter - coords) * (sin(pulseTimer) * 0.55) * distortionPower;
    float offsetLength = length(distortionOffset);
    
    // Ensure that the offset does not exceed a certain intensity, to prevent it from being ridiculous due to the player
    // running far away from the source.
    if (offsetLength > 0.02)
        distortionOffset = 0.02 * distortionOffset / offsetLength;
    
    // Apply radial blur effects.
    float4 color = 0;
    for (int i = 0; i < 8; i++)
        color += tex2D(baseTexture, coords + distortionOffset + distortionOffset * i * blurPower * 0.15) * 0.125;
            
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}