sampler noiseGrainTexture : register(s1);
sampler noiseRingTexture : register(s2);

float globalTime;
float identity;
float scale;
float3 edgeColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate glow and noise related values.
    float distanceFromCenter = distance(coords, 0.5);
    float noise = tex2D(noiseRingTexture, coords * 0.23);
    float edgeNoise = tex2D(noiseRingTexture, coords * 0.09 + globalTime * 0.05 + identity) - 0.7;
    float edgeGlowOpacity = pow(0.02 / distance(distanceFromCenter - edgeNoise * 0.091, 0.38) * sampleColor.a, 3);
    
    // Offset the texture a decent amount to give a grainy texture to the result.
    float grainOffset = 0.09 / edgeGlowOpacity;
    coords.x += tex2D(noiseGrainTexture, coords * 15) * grainOffset;
    coords.y += tex2D(noiseGrainTexture, coords * 15 - 0.518) * grainOffset;
    
    // Combine things together.
    float edgeCutoffOpacity = smoothstep(0.5, 0.49, distanceFromCenter);
    float4 result = noise * edgeCutoffOpacity * float4(edgeColor, 1) * clamp(edgeGlowOpacity, 0, 3);
    
    // Apply black to the center.
    float blackEdgeExpand = (1 - scale) * 0.34 + noise * (1 - scale) * 0.2;
    float black = tex2D(noiseGrainTexture, coords * 50) * 0.036;
    float blackInterpolant = smoothstep(0.38, 0.34, distanceFromCenter) * smoothstep(blackEdgeExpand - 0.01, blackEdgeExpand, distanceFromCenter);
    result = lerp(result, float4(black, black, black, 1), blackInterpolant);
    
    return result * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}