sampler baseTexture : register(s0);
sampler fireNoiseTexture1 : register(s1);
sampler fireNoiseTexture2 : register(s2);
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

float2 clockCenter;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(baseTexture, coords);
    float2 resolutionCorrection = float2(1, uScreenResolution.y / uScreenResolution.x);
    
    // Calculate the intensity of the burn based on how far away it is from the clock.
    float distanceFromCenter = distance(coords * resolutionCorrection, clockCenter * resolutionCorrection);
    float fireIntensity = InverseLerp(0.25, 1.03, distanceFromCenter * uIntensity / uZoom.x) * 7;
    
    // Calculate fire textures for the burn edges.
    float2 fireTexture1Coords = (coords * resolutionCorrection * 3 - uScreenPosition / uScreenResolution * 0.05) / uZoom.x + float2(uTime * 0.007, uTime * -0.04);
    float fireTexture1 = tex2D(fireNoiseTexture1, fireTexture1Coords);
    float2 fireTexture2Coords = (coords * resolutionCorrection * 2 - uScreenPosition / uScreenResolution * 0.05) / uZoom.x + float2(uTime * -0.1, 0);
    float fireTexture2 = tex2D(fireNoiseTexture2, frac(fireTexture2Coords));
    
    // Calculate the resulting fire based on the additive blend of the two aforementioned fire textures.
    float3 fireColor = (uColor * fireTexture1 + uSecondaryColor * fireTexture2) * fireIntensity;
    
    return baseColor + float4(fireColor, 1) * uIntensity + fireIntensity * 0.085;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}