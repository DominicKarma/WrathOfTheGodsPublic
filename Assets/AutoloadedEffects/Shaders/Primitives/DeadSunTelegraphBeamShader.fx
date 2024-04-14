sampler streakHighlightTexture : register(s1);

float globalTime;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    
    // Account for texture distortion artifacts in accordance with the primitive distortion fixes.
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float centerInterpolant = QuadraticBump(coords.y) + sin(globalTime * 50) * 0.05;
    
    color = lerp(float4(1, 1, 1, 1), color, smoothstep(0, 0.2, coords.x));
    
    // Make the telegraph fade in.
    color *= smoothstep(0.01, 0.1, coords.x);
    
    // Apply additive blending to the center.
    color += centerInterpolant * color.a;
    
    // Bias towards red colors after the initial burst.
    color.r += smoothstep(0.1, 0.4, coords.x) * 2.3;
    
    // Make the color fade out at the edges.
    color *= pow(centerInterpolant, 2);
    
    // Make the telegraph fade out as it approaches the end.
    color *= pow(smoothstep(0.9, 0.05, coords.x), 2);
    
    // Sharpen everything. This helps reduce stray patches of visible pixels at the start when the laser is super large.
    color = pow(color, 2.2);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
