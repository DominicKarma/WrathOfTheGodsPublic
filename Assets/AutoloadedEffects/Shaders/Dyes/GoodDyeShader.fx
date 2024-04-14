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

float ScreenBlend(float a, float b)
{
    return 1 - (1 - a) * (1 - b);
}

float3 ScreenBlend(float3 a, float3 b)
{
    return float3(ScreenBlend(a.r, a.r), ScreenBlend(a.g, a.g), ScreenBlend(a.b, a.b));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the 0-1 coords value relative to whatever frame in the texture is being used.
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    
    // Get the pixel's color on the base texture, and take the sample color into account.
    float4 color = tex2D(baseTexture, coords) * sampleColor;
    
    // Calculate the luminosity of the given pixel.
    float luminosity = dot(color.rgb, float3(0.3, 0.59, 0.11));
    
    // Calculate the pink interpolant. This will determine the general coloration of the shading and varies a bit (albeit not too much) over time.
    float pinkInterpolant = sin(luminosity * 6.283 + uTime * 1.1) * 0.25 + 0.97;
    
    // Calculate the blend interpolant. This will be used to determine whether colors use the pink-ish red coloration or the bright yellow apple one.
    float blendInterpolant = saturate(pow(color.r + color.g, 2.4));
    
    // Calculate the base apple color.
    float3 baseAppleColor = float3(lerp(0.85, 1.15, blendInterpolant), 0, lerp(0.05, 0.38, blendInterpolant));
    
    // Calculate the blend color based on the aforementioned blend interpolant.
    // uColor in this case should represent the bright yellow apple color.
    float3 blend = lerp(baseAppleColor, uColor, blendInterpolant * 0.85);
    
    // Blend the base and blend colors together and darken the results if the original luminosity was dark, to preserve edges.
    float edgeInterpolant = smoothstep(0, 0.3, luminosity);
    blend = pow(saturate(ScreenBlend(blend, color.rgb)), pinkInterpolant) * edgeInterpolant;
    
    // Combine things together.
    blendInterpolant = lerp(0.62, 1.01, color.r);
    return float4(lerp(color.rgb, blend, blendInterpolant), 1) * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}