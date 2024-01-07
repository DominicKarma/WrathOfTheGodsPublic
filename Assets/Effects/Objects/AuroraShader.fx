sampler outlineTexture : register(s0);
sampler distortionNoise : register(s1);
sampler accentNoiseTexture : register(s2);

float globalTime;
float verticalSquish;
float scrollSpeedFactor;
float accentApplicationStrength;
float2 parallaxOffset;
float3 bottomAuroraColor;
float3 topAuroraColor;
float3 auroraColorAccent;

float4 CalculateAuroraColor(float2 coords, float2 finalSamplingOffset, float2 zoom)
{
    // Calculate a localized time value for everything else in this method.
    // To give variety, the time varies slightly as a pixel approaches the bottom of the aurora effect.
    float time = globalTime * scrollSpeedFactor - coords.y * 0.05;
    
    // Calculate offset values that are used in the sampling of pixels from the outline noise texture which composes the aurora's shape.
    // Like usual, this is used to ensure that the resulting aurora can "roll over" itself and appear dynamic rather than just being a static, scrolling texture.
    float samplingOffsetX = tex2D(distortionNoise, coords * 0.7 + float2(time * 0.02, 0)) - 0.5;
    float samplingOffsetY = tex2D(distortionNoise, coords * 1.7 + float2(0, time * 0.01)) - 0.5;
    float2 samplingOffset = float2(samplingOffsetX, samplingOffsetY) * float2(0.08, 0.45);
    
    // Calculate a shorthand for the height of the given pixel position from the bottom of the texture. This makes calculations below slightly more intuitive.
    float height = 1 - coords.y;
    
    // Squish pixels as they approach the top of the texture to help make the effect look like aurora-like bands that have a generally (but not completely) vertical shape.
    float horizontalSquishInterpolant = pow(height, 1.2) * 0.53;
    coords.x = lerp(coords.x, 0.5, horizontalSquishInterpolant);
    
    // Apply the final sampling offset vector parameter and parallax as a last step before computing colors.
    coords += finalSamplingOffset + parallaxOffset * scrollSpeedFactor;
    
    // Calculate the base sampling coordinates by the input zoom parameter and accounting for the vertical squish (which intends to correct stretching/squishing artifacts).
    float2 zoomedSamplingCoords = coords * float2(0.4, verticalSquish) * zoom;
    
    // Calculate the scroll offset for the sampling coordinates.
    float2 samplingCoordsScroll = time * float2(-0.18, 0.024) * zoom + samplingOffset * horizontalSquishInterpolant;
    
    // Use the sampling coordinates on the noise texture to get the base for the texture.
    float auroraIntensity = tex2D(outlineTexture, (zoomedSamplingCoords + samplingCoordsScroll) * float2(0.8, 0.12)).r;
    
    // Calculate opacity based on the height of this pixel. This ensures that pixels at the top and bottom of the texture are more translucent.
    // Also, this uses a bit of the above sampling noise to add some slight variance to the opacity.
    float opacity = pow(height, 1.84) * pow(1 - height, 0.15) * (abs(samplingOffsetY) * 2 + 1.3);
    
    // Make the opacity weaker the faster the scroll speed is.
    opacity *= exp(scrollSpeedFactor * -5.4) * 1.35;
    
    // Calculate the final aurora color by combining based on the height, along with a little bit of noise.
    // The noise helps allow for "bands" of texture to form and combine, rather than being a completely continuous mist-like texture.
    float colorNoise = tex2D(distortionNoise, coords * 3 + float2(0, time * 0.21)) - 0.5;
    float4 auroraColor = lerp(float4(bottomAuroraColor, 0), float4(topAuroraColor, 0.37), height + colorNoise * 0.3);
    
    // Apply some secondary hues to the overall result.
    float accentNoise = tex2D(accentNoiseTexture, coords * zoom * 0.12 - samplingOffset * 0.3).r - 0.5;
    auroraColor += float4(auroraColorAccent, 0) * height * (accentNoise * 0.4 + 0.6) * accentApplicationStrength;
    
    return auroraColor * auroraIntensity * opacity;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return (CalculateAuroraColor(coords, 0, 0.6) * 1.3 + CalculateAuroraColor(coords * 0.95, 0.16, 0.75) * 0.8) * sampleColor;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}