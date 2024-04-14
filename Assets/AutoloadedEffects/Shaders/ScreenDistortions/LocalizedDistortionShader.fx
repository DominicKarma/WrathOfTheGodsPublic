sampler baseTexture : register(s0);
sampler uvOffsettingNoiseTexture : register(s1);
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

float vignetteIntensityInterpolant;
float darknessFactor;
float2 distortionCenter;
float2 distortionCenters[6];

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate a UV offset from noise. This is used to give offsets to the edges of the portal.
    float uvOffsetAngle = tex2D(uvOffsettingNoiseTexture, coords) * 16 + uTime * 9;
    float2 uvOffset = float2(cos(uvOffsetAngle), sin(uvOffsetAngle)) * uIntensity * 0.0016;
    
    float distanceFadeoff = 0;
    float distanceFromIdealPosition = 100;
    for (float i = 0; i < 6; i++)
    {
        float localDistanceFromIdealPosition = distance(coords + uvOffset * 1.4, distortionCenters[i]);
        float localDistanceFadeoff = InverseLerp(0.15, 0.11, localDistanceFromIdealPosition);
        distanceFadeoff = max(distanceFadeoff, localDistanceFadeoff);
        distanceFromIdealPosition = min(distanceFromIdealPosition, localDistanceFromIdealPosition);
    }
    
    float4 color = tex2D(baseTexture, coords + uvOffset * distanceFadeoff);
    
    // Darken the space around the distortion point.
    color *= lerp(1, darknessFactor, InverseLerp(0.26, 0, distanceFromIdealPosition) * InverseLerp(0.31, 0.11, distanceFromIdealPosition));
    
    // Create a vignette effect.
    coords *= 1 - coords.yx;
    float vignetteIntensity = coords.x * coords.y * (30 - uIntensity * 17);
    
    return color * lerp(1, pow(vignetteIntensity, 0.176), min(1, vignetteIntensityInterpolant));
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}