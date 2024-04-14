sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler noiseTexture2 : register(s2);
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
float2 screenPosition;
float2 noxusPosition;

float2 distortionCenters[6];
float distortionIntensities[6];

float2 AspectRatioAdjust(float2 coords)
{
    return (coords - 0.5) * float2(uScreenResolution.x / uScreenResolution.y, 1) + 0.5;
}

float CalculateDistortionNoise(float2 worldAdjustedCoords)
{
    float2 noiseOffset = 0;
    float noiseAmplitude = 0.03;
    float2 noiseZoom = 1.087;
    for (float i = 0; i < 2; i++)
    {
        float2 scrollOffset = float2(uTime * 0.285 - i * 0.338, uTime * -1.233 + i * 0.787) * noiseZoom.y * 0.04;
        noiseOffset += (tex2D(noiseTexture2, worldAdjustedCoords * noiseZoom + scrollOffset) - 0.5) * noiseAmplitude;
        noiseZoom *= 2;
        noiseAmplitude *= 0.5;
    }
    
    float noise = lerp(tex2D(noiseTexture, worldAdjustedCoords * 0.61 + noiseOffset).r, 0.5, 0.4);
    return noise + 0.15;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_POSITION) : COLOR0
{
    // Calculate the distance from the current pixel to Noxus.
    float distanceFromNoxus = distance(AspectRatioAdjust(screenPosition + coords), AspectRatioAdjust(noxusPosition));
    
    // Calculate the base distortion power based on the pixel's distance to Noxus.
    // The subtraction and lower bound ensures that it takes a considerable amount of distance before distortion effects become noticeable.
    float2 worldAdjustedCoords = position.xy / uScreenResolution;
    float distortionNoise = CalculateDistortionNoise(worldAdjustedCoords / uZoom);
    float distortionBase = max(distanceFromNoxus - distortionNoise * 0.2 - 1, 0) * uIntensity;
    
    // Apply distortion effects to the apparent angle of the coordinates.
    // The intensity of the distortion is exponential.
    float distortion = (exp(distortionBase) - 1) * distortionNoise;
    
    float angle = atan2(coords.y - 0.5, coords.x - 0.5) + (distortion) * 0.19;
    float2 polar = float2(angle, length(coords - 0.5));
    coords = float2(cos(polar.x), sin(polar.x)) * polar.y + 0.5;
    
    float darkness = saturate(distortion * 0.34) * 1.7;
    float darknessEdge = smoothstep(0.03, 0.4, distortion) * smoothstep(2.4, 0.4, distortion);
    return tex2D(baseTexture, coords) - float4(0.24, 1, 1, 0) * darkness + float4(0.7, 0, 0.46, 1) * darknessEdge;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}