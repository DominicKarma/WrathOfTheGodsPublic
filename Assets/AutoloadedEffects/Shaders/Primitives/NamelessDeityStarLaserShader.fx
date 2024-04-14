sampler noiseTexture : register(s1);

float globalTime;
float uStretchReverseFactor;
float scrollOffset;
float2 uCorrectionOffset;
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
    output.Position = pos + float4(uCorrectionOffset.x, uCorrectionOffset.y, 0, 0);
    output.Position.z = 0;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    // Account for texture distortion artifacts in accordance with the primitive distortion fixes.
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    // Manipulate coordinates based on the stretch factor. The main purpose of this is to ensure that coordinates are not
    // artifically squished just because the laser has not yet reached its maximum length.
    float adjustedCompletionRatio = coords.x * uStretchReverseFactor + scrollOffset;
    
    // Calculate time relative to the stretch factor for the purposes of texture scrolling.
    float time = globalTime * uStretchReverseFactor * 4;
    
    // Read the noise texture as a streak. The first same serves as the primary texture of the laser, while the second serves as an additively blended highlight that adds a bit of
    // bright whites to the overall result.
    float laserTexture = tex2D(noiseTexture, float2(frac(adjustedCompletionRatio * 20 - time * 2.2), coords.y));
    float brightnessHighlight = tex2D(noiseTexture, float2(frac(adjustedCompletionRatio * 7 - time * 1.4), coords.y * 0.5));
    
    // Calculate coordinate based fade opacities. Pixels to the far edge of the laser and at its tip fade away.
    float horizontalEdgeFade = pow(QuadraticBump(coords.y), 4);
    float verticalEndFade = pow(InverseLerp(0.94, 0.7, coords.x), 3);
    
    // Calculate the overall opacity of the main texture. This value is not applied to the additive highlight, though the vertical end fade is.
    float opacity = (0.5 + laserTexture) * horizontalEdgeFade;
    if (coords.x < 0.023)
        opacity *= pow(coords.x / 0.023, 6);
    if (coords.x > 0.95)
        opacity *= pow(1 - (coords.x - 0.95) / 0.05, 6);
    
    // Calculate the final color.
    float4 finalColor = color * opacity * 6;
    
    // Apply the highlight to the result.
    finalColor += brightnessHighlight * finalColor.a;
    
    return finalColor * verticalEndFade;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
