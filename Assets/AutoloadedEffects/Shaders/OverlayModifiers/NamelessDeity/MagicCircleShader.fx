sampler baseTexture : register(s0);

bool flip;
float orientationRotation;
float spinRotation;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 Coordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 Coordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float2 coords = input.Coordinates - 0.5;
    float rotationSine = sin(orientationRotation);
    float rotationCosine = cos(orientationRotation);
    float rotationOriginSine = sin(spinRotation);
    float rotationOriginCosine = cos(spinRotation);
    float2x2 rotationMatrix = float2x2(rotationCosine, -rotationSine, rotationSine, rotationCosine);
    float2x2 circularRotationMatrix = float2x2(rotationOriginCosine, -rotationOriginSine, rotationOriginSine, rotationOriginCosine);
    float2x2 scalingMatrix = float2x2(3, 0, 0, 1);
    
    output.Color = input.Color;
    
    // Rotate based on direction, squash the result, and then rotate the squashed result by the circular rotation.
    output.Coordinates = mul(input.Coordinates - 0.5, rotationMatrix) + 0.5;
    output.Coordinates = mul(output.Coordinates - 0.5, scalingMatrix) + 0.5;
    output.Coordinates = mul(output.Coordinates - 0.5, circularRotationMatrix) + 0.5;
    output.Position = mul(input.Position, uWorldViewProjection);

    return output;
}

float4 PixelFunction(VertexShaderOutput input) : COLOR0
{
    float2 updatedCoords = input.Coordinates;
    
    // Adjust for horizontal rotation.
    if (flip)
        updatedCoords.y = 1 - updatedCoords.y;
    return tex2D(baseTexture, updatedCoords) * input.Color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelFunction();
    }
}