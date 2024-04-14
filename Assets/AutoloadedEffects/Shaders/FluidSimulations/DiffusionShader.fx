sampler baseTarget : register(s0);

float deltaTime;
float2 simulationSize;
float2 stepSize;
float diffusionFactorTop;
float diffusionFactorBottom;
float fadeOutDecay;

bool xAxisOnly;
bool yAxisOnly;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate cutoff as necessary. These ensure that only specific axes are updated at the end of this shader.
    float4 axisCutoff = 1;
    if (xAxisOnly)
        axisCutoff *= float4(1, 0, 0, 0);
    if (yAxisOnly)
        axisCutoff *= float4(0, 1, 0, 0);
    
    // Combine the center and neighboring values based on diffusion.
    float diffusion = lerp(diffusionFactorTop, diffusionFactorBottom, coords.y) * deltaTime;
    float4 center = tex2D(baseTarget, coords);
    float4 left = tex2D(baseTarget, coords + float2(-1, 0) * stepSize);
    float4 right = tex2D(baseTarget, coords + float2(1, 0) * stepSize);
    float4 top = tex2D(baseTarget, coords + float2(0, -1) * stepSize);
    float4 bottom = tex2D(baseTarget, coords + float2(0, 1) * stepSize);
    
    // Update everything.
    float4 newValue = (center + diffusion * (left + right + top + bottom)) / (diffusion * 4 + 1) * fadeOutDecay;
    float4 valueDifference = newValue - center;
    return center + valueDifference * axisCutoff;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}