sampler baseTexture : register(s0);

float animationTime;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float2 ComplexSquare(float2 v)
{
    return float2(v.x * v.x - v.y * v.y, v.x * v.y * 2);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Re-arrange coords to go from 0 to 1 to -1.5 to 1.5.
    coords = (coords - 0.5) * 3;
    
    float2 seed = float2(0.37, -0.5 + animationTime * 0.25);
    float totalChange = exp(-length(coords));
    
    // Perform julia fractal iterations.
    for (int i = 0; i < 32; i++)
    {
        coords = seed + ComplexSquare(coords);
        totalChange += exp(-length(coords));
        if (dot(coords, coords) > 8)
            break;
    }
    
    // Combine colors based on the fractal equation.
    float r = (sin(totalChange * 0.5 + animationTime * 4.63)) * 0.3 + 0.7;
    float g = r * lerp(1, 0.56, animationTime);
    float b = r * g + lerp(-0.25, 0.03, animationTime);
    return saturate(float4(r, g, b, 1)) * InverseLerp(1.84, 3.93, totalChange) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
