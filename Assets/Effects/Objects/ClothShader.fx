sampler clothTexture : register(s0);

bool flipHorizontally;
float brightnessPower;
float3 lightDirection;
float2 pixelationZoom;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float3 Normal : NORMAL0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    output.Position.z = 0;
    
    output.Normal = input.Normal;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float2 adjustedCoords = abs(flipHorizontally - coords);
    
    // Read the texture on the cloth at the given pixel. This uses pixelation.
    float3 baseColor = tex2D(clothTexture, round(adjustedCoords / pixelationZoom) * pixelationZoom * 0.5);
    
    // Calculate diffuse lighting for the given pixel on the cloth. This determines how dark the pixel is at the point.
    float brightness = pow(saturate(dot(lightDirection, normalize(input.Normal))), brightnessPower) * InverseLerp(0.1, 0.2, 1 - coords.x) * 1.2;
    brightness = clamp(brightness, 0.5, 10) * 1.3;
    
    if (adjustedCoords.y < 0.5)
    {
        return float4(0.9, 0.86, 0.78, 1) - float4(baseColor, 0) * 0.45;
    }
    else if (coords.y < 0.5 - adjustedCoords.x * 1.4)
        return 0;
    else if (adjustedCoords.y > 0.5 + coords.x * 1.4)
        return 0;
    
    return float4(baseColor * brightness, 1);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
