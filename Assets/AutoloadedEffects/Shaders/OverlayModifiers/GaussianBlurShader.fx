sampler baseTexture : register(s0);

float blurOffset;
float4 colorMask;
bool invert;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    sampleColor *= colorMask;
    
    // Sample the texture multiple times in radial directions and average the results to give the blur.
    float4 color = 0;
    for (int i = 0; i < 4; i++)
    {
        float localBlurOffset = blurOffset * (i + 1) * 0.25;
        color += tex2D(baseTexture, coords + float2(1, 0) * localBlurOffset);
        color += tex2D(baseTexture, coords + float2(0, 1) * localBlurOffset);
        color += tex2D(baseTexture, coords + float2(0, -1) * localBlurOffset);
        color += tex2D(baseTexture, coords + float2(-1, 0) * localBlurOffset);
    }
    
    float4 result = color * 0.0625;
    if (invert && result.a > 0)
        result = float4(1 - result.rgb, 1) * result.a;
    
    return result * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}