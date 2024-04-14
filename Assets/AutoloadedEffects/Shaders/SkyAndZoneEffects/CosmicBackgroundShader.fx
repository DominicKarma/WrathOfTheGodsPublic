sampler kalisetFractal : register(s1);
sampler noiseTexture : register(s2);

float zoom;
float scrollSpeedFactor;
float brightness;
float globalTime;
float colorChangeStrength1;
float colorChangeStrength2;
float detailIterations;
float3 frontStarColor;
float3 backStarColor;
float3 colorChangeInfluence1;
float3 colorChangeInfluence2;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = 0;
    float volumetricLayerFade = 1.0;
    float distanceFromBottom = distance(coords.y, 1);
    float detailIterationsClamped = clamp(detailIterations, 1, 20);
    float2 offset = float2(0.375, 0.12);
    for (int i = 0; i < detailIterationsClamped; i++)
    {
        float time = globalTime * pow(volumetricLayerFade, 2) * 3;
        float2 p = (coords - offset) * zoom + offset;
        p.y += 1.5;

        // Perform scrolling behaviors. Each layer should scroll a bit slower than the previous one, to give an illusion of 3D.
        p += float2(time * scrollSpeedFactor, time * scrollSpeedFactor);
        p /= volumetricLayerFade;

        float totalChange = tex2D(kalisetFractal, p);
        float4 layerColor = float4(lerp(frontStarColor, backStarColor, i / detailIterationsClamped), 1.0);
        result += layerColor * totalChange * volumetricLayerFade;

        // Make the next layer exponentially weaker in intensity.
        volumetricLayerFade *= 0.92;
    }
    
    // Apply color change interpolants. This will be used later.
    float colorChangeBrightness1 = tex2D(noiseTexture, coords * 1.5);
    float colorChangeBrightness2 = tex2D(noiseTexture, coords * 1.65 + globalTime * scrollSpeedFactor);
    float totalColorChange = colorChangeBrightness1 + colorChangeBrightness2;

    // Account for the accumulated scale from the fractal noise.
    result.rgb = pow(result.rgb * 0.010714, 2.64 - totalColorChange * 1.4 + pow(distanceFromBottom, 3) * 3.9) * brightness;
    
    // Apply color changing accents to the result, to keep it less homogenous.
    result.rgb += colorChangeInfluence1 * dot(result.rgb, 0.3333) * colorChangeBrightness1 * colorChangeStrength1;
    result.rgb += colorChangeInfluence2 * dot(result.rgb, 0.3333) * pow(colorChangeBrightness2, 4) * colorChangeStrength2;
    
    return result * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
