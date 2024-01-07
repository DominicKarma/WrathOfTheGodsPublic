sampler baseTexture : register(s0);

float2 pixelationFactor;
float2 textureSize;
float3x3 horizontalEdgeKernel;
float3x3 verticalEdgeKernel;

float CalculateSobelEdgeFactor(float2 coords)
{
    float2 step = 1.2 / textureSize;
    
    // Convolution in X and Y directions.
    float3 sumX = 0;
    float3 sumY = 0;
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            float3 color = tex2D(baseTexture, coords + float2(i, j) * step);
            sumX += horizontalEdgeKernel[i + 1][j + 1] * color;
            sumY += verticalEdgeKernel[i + 1][j + 1] * color;
        }
    }

    // Combine X and Y gradients to get the final gradient magnitude.
    return saturate(length(sumX) + length(sumY));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    // Calculate the edge filter. This is used to give the resulting image harsh black outlines
    float edge = 1 - pow(CalculateSobelEdgeFactor(coords), 350);
    float4 edgeFilter = float4(edge, edge, edge, 1);
    
    return color * sampleColor * edgeFilter;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}