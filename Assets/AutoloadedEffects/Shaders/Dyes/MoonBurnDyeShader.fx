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

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords) * sampleColor;
    
    // Determine how strong a color is to being pure red and pure blue.
    // This differs from just checking the channel values because that doesn't take into consideration other colors.
    // If you looked a white color (Which has a red and blue intensity of 1) no sane person would look at it and say it's
    // red or blue.
    float redIntensity = dot(color.rgb, float3(1, 0, 0));
    float blueIntensity = dot(color.rgb, float3(0, 0, 1));
    
    // Use the intensity differences to effectively swap the two color channels.
    color.r += (blueIntensity - redIntensity) * swapHarshness;
    color.b += (redIntensity - blueIntensity) * swapHarshness;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}