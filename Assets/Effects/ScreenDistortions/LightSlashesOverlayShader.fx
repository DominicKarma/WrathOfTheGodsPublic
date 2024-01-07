sampler baseTexture : register(s0);
sampler slashTargetTexture : register(s1);
sampler behindSplitTexture : register(s2);
sampler noiseTexture : register(s3);
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

float splitBrightnessFactor;
float splitTextureZoomFactor;
float vignetteInterpolant;
float2 backgroundOffset;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Retrieve data from the render target.
    // R = Brightness.
    // GB = Distortion direction in a 0-1 range.
    // A = Distortion intensity.
    // GB must be converted back to -1 to 1 in this shader, since colors don't permit values outside of 0-1.
    float4 targetData = tex2D(slashTargetTexture, coords + backgroundOffset);
    float backgroundDimensionBrightness = targetData.r;
    float distortionIntensity = targetData.a * 5;
    float2 distortionOffset = targetData.gb * 2 - 1;
    
    // Sample from the base texture, taking distortion into account.
    float2 distortionPosition = coords + distortionOffset * lerp(0.85, 1.15, sin(uTime * 10) * 0.5 + 0.5) * distortionIntensity * 0.013;
    float4 color = tex2D(baseTexture, distortionPosition);
    
    // Sample colors from the "behind" texture.
    float4 backgroundDimensionColor1 = tex2D(behindSplitTexture, coords * splitTextureZoomFactor + float2(uTime, 0) * -0.23) * splitBrightnessFactor;
    float4 backgroundDimensionColor2 = tex2D(behindSplitTexture, coords + backgroundDimensionColor1.rb * splitTextureZoomFactor * 0.12) * splitBrightnessFactor * 0.5;
    
    // Combine the aforementioned colors together, taking into account the overall brightness of them at the given pixel.
    float4 backgroundDimensionColor = (backgroundDimensionColor1 + backgroundDimensionColor2) * backgroundDimensionBrightness;
    
    // Apply a vignette to the background. If distortion or brightness effects are in place, this effect is undone.
    float2 noiseOffset = float2(tex2D(noiseTexture, coords * 1.8 + float2(uTime * 0.169, uTime * -0.32)).r, tex2D(noiseTexture, coords * 1.2 + float2(0, uTime * 0.15)).r);
    float vignetteOffset = tex2D(noiseTexture, coords * 3.6 + noiseOffset * 0.09 + float2(uTime * -0.76, 0)).r * 0.08;
    float distanceFromCenter = distance((coords - 0.5) * float2(uScreenResolution.x / uScreenResolution.y, 1) + 0.5, 0.5);
    float vignetteIntensity = InverseLerp(0.32, 0.56, distanceFromCenter - (1 - vignetteInterpolant) * 0.1 - vignetteOffset);
    color = lerp(color, float4(0, 0, 0, 1), vignetteIntensity * vignetteInterpolant);
    
    // Perform additive blending.
    return color + backgroundDimensionColor + targetData.a * 0.1;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}