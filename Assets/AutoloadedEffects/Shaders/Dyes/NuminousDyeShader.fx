sampler baseTexture : register(s0);
sampler colorMapTexture : register(s1);

float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float2 uTargetPosition;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the 0-1 coords value relative to whatever frame in the texture is being used.
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    float2 pixelationFactor = 4 / uSourceRect.zw;
    float2 snappedFramedCoords = round(framedCoords / pixelationFactor) * pixelationFactor;
    
    // Get the pixel's color on the base texture.
    float4 color = tex2D(baseTexture, coords);
    
    // Calculate the pixel's brightness. This is biased towards zero barring very strong brightness values to increase contrast.
    float brightness = pow((color.r + color.g + color.b) / 3, 2.8);
    
    // Calculate a brightness increase value. This is dependent on both time and distance from the center. The further a pixel is from the center, the brighter it is.
    // This helps create a bit of a cool purple-ish "glow" at the edges of the result.
    // Also, the time depends on one of the initial color's channels, in order to make more interesting and varied patterns.
    float brightnessIncrease = sin(uTime * -5 + color.b * 100) * brightness + distance(snappedFramedCoords, 0.5) * 0.8;
    
    // Use the brightness value to sample a color on the map texture. Colors to the left are to be accessed by darker colors, colors to the right are to be accessed by brighter colors.
    // The Y axis acts as a form of noise-like "accent", similar to the entropic dye visual.
    float2 mapCoords = float2(brightness * 1.4 + brightnessIncrease, color.b);
    
    // Calculate the result based on the map texture color.
    float4 result = tex2D(colorMapTexture, clamp(mapCoords, float2(0.01, 0), float2(0.96, 1)));
    
    // Significantly increase the constrast of the result. This results in bright colors being exaggerated while dark colors shoot away towards a pitch black, similar to Nameless's censor.
    result.rgb *= lerp(0.6, 2.4, brightness);
    result.rgb = pow(result.rgb, 1.6);
    
    return result * color.a * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}