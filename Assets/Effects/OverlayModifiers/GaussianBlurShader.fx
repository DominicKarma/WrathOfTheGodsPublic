sampler baseTexture : register(s0);

float blurOffset;
float4 colorMask;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    sampleColor *= colorMask;
    
    // Sample the texture multiple times in radial directions and average the results to give the blur.
    float4 color = 0;
    color += tex2D(baseTexture, coords + float2(1, 0) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(0.866025, 0.5) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(0.5, 0.866025) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(0, 1) * blurOffset) * sampleColor;
    
    color += tex2D(baseTexture, coords + float2(-0.5, 0.866025) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(-0.866025, 0.5) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(-1, 0) * blurOffset) * sampleColor;
    
    color += tex2D(baseTexture, coords + float2(-0.866025, -0.5) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(-0.5, -0.866025) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(0, -1) * blurOffset) * sampleColor;
    
    color += tex2D(baseTexture, coords + float2(0.5, -0.866025) * blurOffset) * sampleColor;
    color += tex2D(baseTexture, coords + float2(0.866025, -0.5) * blurOffset) * sampleColor;
    
    return color * 0.08333333;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}