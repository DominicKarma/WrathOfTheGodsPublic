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
    
    // Calculate the brightness of the base color.
    float brightness = (color.r + color.g + color.b) / 3;
    
    // Determine the map coordinates. In the map texture, the X axis corresponds to a hue value while the Y axis corresponds to a noise-induced accent that allows texture variance.
    float hueScrollSpeed = 0.8;
    float accentScrollSpeed = 0.7;
    float2 mapCoords = float2(brightness * 0.9 + uTime * hueScrollSpeed, uTime * accentScrollSpeed + length(snappedFramedCoords) * 0.5);
    float4 mapColor = tex2D(colorMapTexture, mapCoords);
    
    // Make the resulting color color approach a dark purple. This does a good job of eliminating isolated green colors, which aren't in line with Noxus' aesthetic.
    // The intensity of this interpolation is stronger the darker the original color but is weakened if the original map color's red value is high.
    float4 result = lerp(mapColor, float4(uColor, 1), (1 - brightness) * 0.9 - mapColor.r * 0.5);
    
    // Reduce red intensity depending on how much blue there is. This effect is a bit mild but it helps ensure that pink colors aren't too strong, since those aren't very common on Noxus.
    result.r -= result.b * 0.2;
    
    // Provide a blanket weakening of the blue input, to help darken the overall effect a bit.
    result.b *= 0.7;
    
    // Make the green input vanish relative to how much blue there is. If there's already a good amount of blue it's acceptable for green to be present in moderate amounts, since that results in
    // a cool cyan aesthetic. Otherwise, though, it'll just create undesirable green/yellow colors that are nowhere to be found on Noxus.
    result.g *= lerp(0.2, 1.09, pow(result.b, 1.7));
    
    // Make the color brighter, to make the aesthetic less muted and more consistent with what the brightness was like before the shader was applied.
    result *= brightness * 0.56 + 1.1;
    
    return result * color.a * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}