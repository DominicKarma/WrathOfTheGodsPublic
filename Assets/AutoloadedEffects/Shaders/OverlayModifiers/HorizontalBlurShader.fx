sampler baseTexture : register(s0);

float maxBlurOffset;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Sample the texture multiple times in the horizontal direction and average the results to give the blur.
    float4 color = 0;
    for (int i = -5; i < 6; i++)
        color += tex2D(baseTexture, coords + float2(maxBlurOffset * i, 0)) * 0.1;
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}