sampler baseTexture : register(s0);
sampler shapeTexture : register(s1);

float globalTime;
float appearanceCutoff;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the shape noise. This will be used chiefly to decide which parts of the smoke are drawn or not.
    float shapeNoise = tex2D(shapeTexture, coords * 0.7 + float2(0, globalTime * 0.4));
    
    // Calculate positional cutoff values.
    // These will make the smoke less likely to appear at the left/right boundaries and top of the texture.
    // -4x^2 + 4x is a quadratic bump curve, and 4x(-x + 1) is its factored form.
    float horizontalBump = coords.x * (-coords.x + 1) * 4;
    float horizontalCutoff = (1 - horizontalBump) * 0.5;
    float verticalCutoff = 1 - coords.y;
    
    // Combine everything together for the final decision on whether the pixel will be erased.
    float eraseCutoff = step(shapeNoise + verticalCutoff + horizontalCutoff + appearanceCutoff, 0.89);
    
    // Apply the cutoff.
    return tex2D(baseTexture, coords) * sampleColor * eraseCutoff;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}