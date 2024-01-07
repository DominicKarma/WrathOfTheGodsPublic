sampler projectionTexture : register(s1);

bool horizontalFlip;
float globalTime;
float heightRatio;
float lengthRatio;
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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    coords.x *= lengthRatio;
    coords.y = saturate((coords.y - 0.5) * heightRatio + 0.5) * 1.2 - 0.1;
    if (coords.x >= 1)
        return 0;
    
    if (abs(coords.x - 0.5) >= 0.49)
        return 0;
    if (abs(coords.y - 0.5) >= 0.49)
        return 0;
    
    coords.y = abs(horizontalFlip - coords.y);
    
    return tex2D(projectionTexture, coords.yx) * color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
