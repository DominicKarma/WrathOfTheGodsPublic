sampler baseTexture : register(s0);
sampler fogNoiseTexture : register(s1);
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

float centerBrightnesses[2];
float2 sourcePositions[2];
float4 windColor1;
float4 windColor2;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float2 AspectRatioCorrect(float2 coords)
{
    return (coords - 0.5) * float2(uScreenResolution.x / uScreenResolution.y, 1) + 0.5;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Use FBM to calculate complex warping offsets in the noise texture that will be used to create the fog noise.
    float2 noiseOffset = 0;
    float noiseAmplitude = 0.03;
    float2 noiseZoom = 3.187;
    float2 worldAdjustedCoords = coords + uScreenPosition * 0.0003;
    for (float i = 0; i < 2; i++)
    {
        float2 scrollOffset = float2(uTime * 0.285 - i * 0.338, uTime * -1.233 + i * 0.787) * noiseZoom.y * 0.04;
        noiseOffset += (tex2D(fogNoiseTexture, worldAdjustedCoords * noiseZoom + scrollOffset) - 0.5) * noiseAmplitude;
        noiseZoom *= 2;
        noiseAmplitude *= 0.5;
    }
    
    // Calculate warp noise values.
    float sourceDistanceWarp = tex2D(fogNoiseTexture, worldAdjustedCoords * 3 - float2(0, uTime * 0.33)) * 0.08;
    
    // Calculate noise values.
    noiseOffset.x += uTime * 0.08;
    float noise = lerp(tex2D(fogNoiseTexture, worldAdjustedCoords * 0.61 + noiseOffset).r, 0.5, 0.63);
    float circleEdgeNoise = tex2D(fogNoiseTexture, worldAdjustedCoords * 1.7 + noiseOffset).r;
    float darknessEdgeNoise = tex2D(fogNoiseTexture, worldAdjustedCoords * 1.1 + noiseOffset).r;
    
    // Calculate the base for the fog intensity.
    float fogInterpolant = uIntensity;
    
    // Calculate the base color and its brightness.
    float4 baseColor = tex2D(baseTexture, coords);
    float brightness = dot(baseColor.rgb, float3(0.3, 0.59, 0.11));
    
    // Calculate the distance from the center of the screen, taking into account aspect ratio.
    float distanceFromCenter = distance(coords, 0.5);
    for (int j = 0; j < 2; j++)
    {
        float distanceFromSource = distance(AspectRatioCorrect(coords), AspectRatioCorrect(sourcePositions[j]));
        float centerBrightness = centerBrightnesses[j];
        if (centerBrightness <= 0)
            continue;
        
        float centerFadeoutIntensity = lerp(0.24, 0.32, centerBrightness);
        float distortedDistanceFromSource = distanceFromSource - circleEdgeNoise * lerp(0.09, 0.27, centerBrightness) - centerBrightness * 0.02;
        fogInterpolant = saturate(fogInterpolant - InverseLerp(0.12, 0.057, distortedDistanceFromSource / uIntensity) * centerFadeoutIntensity);
    }
    
    // Calculate the appearance of red/sky blue colors.
    noise = pow(noise, 1.5);
    float innerCircleInterpolant = uIntensity - fogInterpolant;
    float skyBlueNoise = tex2D(fogNoiseTexture, worldAdjustedCoords * float2(0.65, 2.06) + sourceDistanceWarp + float2(uTime * -0.2, 0));
    float skyBlueInterpolant = smoothstep(0.3, 0.25, noise + innerCircleInterpolant) * pow(skyBlueNoise, 4.1) * 1.25;
    float redNoise = tex2D(fogNoiseTexture, worldAdjustedCoords * float2(0.7, 1.86) + sourceDistanceWarp + float2(uTime * -0.05, 0));
    float redInterpolant = smoothstep(0.47, 0.496, noise - innerCircleInterpolant) * pow(redNoise, 3.5) * 1.4;
    
    // Combine the base color with the fog.
    float4 result = lerp(baseColor, noise * float4(uColor, 1), pow(fogInterpolant, 1.25) * 0.96);
    
    // Make the base color get a bit darker the further out it is from the center.
    result.rgb *= saturate(1 - (distanceFromCenter + darknessEdgeNoise * 0.1) * uIntensity * 0.56);
    
    return result + windColor1 * skyBlueInterpolant + windColor2 * redInterpolant;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}