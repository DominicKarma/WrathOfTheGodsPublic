sampler baseTexture : register(s0);

float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float uDirection;
float2 uWorldPosition;
float2 uTargetPosition;
float2 uImageSize0;
float2 uImageSize1;
float2 uLegacyArmorSheetSize;
float3 uLightSource;
float3 uColor;
float3 uSecondaryColor;
float4 uLegacyArmorSourceRect;
float4 uSourceRect;

// Corresponds to Clip Studio Pant's Add (Glow) blending function.
float AddGlowBlend(float a, float b)
{
    return min(1, a + b);
}

float3 AddGlowBlend(float3 a, float3 b)
{
    return float3(AddGlowBlend(a.r, b.r), AddGlowBlend(a.g, b.g), AddGlowBlend(a.b, b.b));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the 0-1 coords value relative to whatever frame in the texture is being used.
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    
    // Get the pixel's color on the base texture.
    float4 color = tex2D(baseTexture, coords);
    
    float brightness = dot(color.rgb, float3(0.3, 0.59, 0.11)) - 0.5;
    float redInterpolant = smoothstep(0.6, 0.4, framedCoords.x);
    
    if (uDirection == -1)
        redInterpolant = 1 - redInterpolant;
    float3 blend = lerp(uColor, uSecondaryColor, redInterpolant) * uSaturation + brightness;
    float4 blendedColor = float4(AddGlowBlend(blend, color.rgb), 1) * color.a;
    return blendedColor * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}