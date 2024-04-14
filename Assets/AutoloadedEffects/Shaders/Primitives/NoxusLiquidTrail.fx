sampler noiseTexture : register(s0);

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Store primitive data in local variables.
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    // Account for texture distortion artifacts in accordance with the primitive distortion fixes.
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float2 originalCoords = coords;
    originalCoords.x = 1 - originalCoords.x;
    
    // Calculate the height of the fluid based on noise.
    float noiseOffsetAngle = tex2D(noiseTexture, coords * 0.8 + float2(globalTime * 0.6, 0)) * 12.5;
    float2 noiseOffset = float2(cos(noiseOffsetAngle), sin(noiseOffsetAngle)) * 0.01;
    float heightCut = 0.5 - tex2D(noiseTexture, coords.y * 0.34 + float2(globalTime * 0.12, 0) + noiseOffset);
    color *= smoothstep(2, 1.55, coords.x + heightCut * 4);
    
    // Make the color taper off at the bototm.
    color *= smoothstep(coords.x + noiseOffset.y * 3.5, 0, 0.1);
    coords.x -= heightCut;
    
    // Apply edges to the result.
    coords.y = lerp(coords.y, 0.5, abs(noiseOffset.x) * -6);
    
    // Calculate the edge colors, applying (obviously) to the edge of opaque pixels.
    float horizontalDistance = distance(coords.y, 0.5);
    float edgeInterpolant = smoothstep(0.48, 0.5, horizontalDistance) * smoothstep(0.85, 0.7, coords.x);
    color.r += (edgeInterpolant * color.a + smoothstep(0.2, 0.03, color.a) * color.a > 0) * smoothstep(0.3, 0.1, originalCoords.x);
    
    // Since horizontal distortions are applied to the texture, ensure that pixels beyond the natural horizontal boundary get erased.
    color *= horizontalDistance <= 0.5;
    
    // Apply a dithering mask near the bottom.
    // Colors/texturing are affected by this in the NoxusRiftColorShader.fx file.
    color.g += smoothstep(0.15, 0.4, originalCoords.x) * color.a;
    
    float topFade = smoothstep(0.16, 0.35, originalCoords.x + noiseOffset.x * 0.6);
    float bottomFade = smoothstep(0.75, 0, originalCoords.x - noiseOffset.x * 4);
    return color * bottomFade * topFade * 3;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
