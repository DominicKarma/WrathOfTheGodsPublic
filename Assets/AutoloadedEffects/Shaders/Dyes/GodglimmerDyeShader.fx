sampler baseTexture : register(s0);

float swapHarshness;
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

float OverlayBlend(float a, float b)
{
    if (a < 0.5)
        return a * b * 2;
    
    return 1 - (1 - a) * (1 - b) * 2;
}

float3 OverlayBlend(float3 a, float3 b)
{
    return float3(OverlayBlend(a.r, b.r), OverlayBlend(a.g, b.g), OverlayBlend(a.b, b.b));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords) * float4(sampleColor.rgb, 1);
    
    float3 goldPixelColor = color.rgb;
    float3 goldFactor = float3(1, 0.67, 0);
    return lerp(color, float4(OverlayBlend(color.rgb, goldFactor), 1) * sampleColor.a * color.a, swapHarshness);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}