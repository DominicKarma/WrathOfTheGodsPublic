sampler streakTexture : register(s1);

float globalTime;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
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

// This is mechanically equivalent to the FadedUVMapStreak shader I wrote for Calamity a while back.
// It is duplicated so as to be usable by the custom ManagedShader system.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    // Read the fade map as a streak.
    float4 fadeMapColor = tex2D(streakTexture, float2(frac(coords.x - globalTime * 2.5), coords.y));
    float opacity = fadeMapColor.r;
    
    // Fade out at the edges of the trail to give a blur effect.
    // This is very important.
    float power = lerp(3, 10, coords.x);
    opacity = lerp(pow(sin(coords.y * 3.141), power), opacity, coords.x);
    
    // Fade out at the end of the streak.
    if (coords.x > 0.7)
        opacity *= pow(1 - (coords.x - 0.7) / 0.3, 6);
    return color * opacity * 1.5;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
