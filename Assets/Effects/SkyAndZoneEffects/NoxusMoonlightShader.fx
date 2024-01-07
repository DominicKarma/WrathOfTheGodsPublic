sampler baseTexture : register(s0);
sampler noiseShimmerTexture : register(s1);
sampler purpleIncreaseTexture : register(s2);
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
float maxShineBrightnessFactor;
float zoom;
float2 unscaledScreenArea;
float3 sparkleColor1;
float3 sparkleColor2;
float3 glowColor;

float CalculateSparkle(float2 coords)
{
    // Calculate a local time value for the rest of this method.
    float time = uTime * 2;

    // Calculate the first sample position and use it to get a base brightness value.
    // The way the scrolls are implemented, this ends up looking like one parallax layer.
    float2 samplePosition = coords + float2(sin(time * 0.2) * 0.01, time * 0.347);
    float result = tex2D(noiseShimmerTexture, samplePosition * 0.54 + time * float2(-1, -1.04) * 0.048).r;

    // Do the same as above, albeit with a different sample position and scroll direction.
    // This serves as the second parallax layer, and helps make the particles look like they're moving in place in the background.
    samplePosition = coords + float2(cos(time * 0.1) * 0.003, time * 0.246);
    result += tex2D(noiseShimmerTexture, samplePosition * 0.9 + time * float2(1, -1) * 0.048).r;
    
    // And a third time. This one is considerably weaker, with finer particles appearing.
    samplePosition = coords + float2(sin(time * 0.3) * 0.002, time * 0.4);
    result += tex2D(noiseShimmerTexture, samplePosition * 5.6 + time * float2(1, -1) * 0.048).r * 0.45;

    // Return the resulting brightness.
    return pow(result, 8);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    // Calculate the greyscale version of the color and interpolate towards it based on how intense the effect currently is, slightly washing out the colors.
    float greyscale = dot(color.rgb, float3(0.3, 0.59, 0.11));
    color = lerp(color, greyscale, uIntensity * 0.1);
    
    // Calculate the coordinate for the shine. This will also be used for the sparkle overlays.
    // This shine will be used to tint the screen and add periodic lights from above.
    float2 shineCoords = ((coords - 0.5) / zoom + 0.5f + uScreenPosition / unscaledScreenArea) * 0.85;
    
    // Calculate the intensity of the sparkles. These are really only noticeable on the aforementioned downward sky lights.
    float sparkle = CalculateSparkle(shineCoords * 13.5) * uIntensity;
    
    // Calculate the max brightness of the shine.
    float maxShineBrightness = (sparkle * 3.05 + 7.6) * maxShineBrightnessFactor * uIntensity;
    
    // Calculate the shine based on aforementioned variables. This takes into account two things:
    // 1. X position. This ensures that the effect is periodic, and thusly creates the downward lights.
    // 2. The greyscale value. This ensures that dark pixels are not lit up, since they were never lit up before.
    float lightInterpolant = pow(sin(uTime * 0.04 + shineCoords), 64);
    float shineBrightness = lightInterpolant * greyscale * maxShineBrightness;
    
    // Apply the shine to the resulting color.
    float glowInterpolant = saturate(pow(greyscale, 0.64) - color.g * 0.6 + shineBrightness) * uIntensity;
    color.rgb += glowColor * glowInterpolant * 0.6;
    
    // Apply purple increases.
    float2 purpleCoordsOffset = tex2D(purpleIncreaseTexture, shineCoords * 1.5 + uTime * float2(0.56, -0.22)) * 0.12;
    float purpleInterpolant = tex2D(purpleIncreaseTexture, shineCoords * 0.6 + float2(uTime * 0.01, 0) + purpleCoordsOffset).r;
    color.rgb += float3(1, -0.4, 2) * (purpleInterpolant - shineBrightness * 0.04) * uIntensity * pow(greyscale, 0.4) * 0.26;
    
    // Apply the sparkles to the resulting colors.
    float3 sparkleColor = lerp(sparkleColor1, sparkleColor2, sin(uTime * 0.7 + shineCoords.x * 93.2)) * pow(glowInterpolant, 2.2) * sparkle * 0.19;
    color.rgb += sparkleColor;
    
    // Lastly, make things a bit darker overall.
    color.rgb = lerp(color.rgb, 0, (1 - lightInterpolant) * 0.3);
    
    return color;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}