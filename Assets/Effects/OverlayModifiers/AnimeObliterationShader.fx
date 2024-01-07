sampler baseTexture : register(s0);
sampler uvOffsetingNoiseTexture : register(s1);

float opacity;
float globalTime;
float disintegrationFactor;
float2 pixelationFactor;
float2 scatterDirectionBias;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelate coords.
    coords = round(coords / pixelationFactor) * pixelationFactor;
    
    // Calculate values for the warp noise.
    float warpNoise = tex2D(uvOffsetingNoiseTexture, coords * 7.3 + float2(globalTime * 0.1, 0)).r;
    float warpAngle = warpNoise * 16;
    float2 warpNoiseOffset = float2(cos(warpAngle), sin(warpAngle));
    
    // Warp and pixelate coords again.
    coords += scatterDirectionBias * (1 - warpNoise) * 0.02;
    coords = round((coords + warpNoiseOffset * disintegrationFactor * 0.01) / pixelationFactor) * pixelationFactor;
    
    // Make colors black to contrast the bright light.
    float4 color = tex2D(baseTexture, coords);
    color = float4(0, 0, 0, 1) * color.a;
    
    return color * sampleColor * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}