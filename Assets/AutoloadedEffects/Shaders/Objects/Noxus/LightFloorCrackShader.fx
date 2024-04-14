sampler baseTexture : register(s0);
sampler crackedTexture : register(s1);
sampler noiseTexture : register(s2);

float globalTime;
float shatterInterpolant;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate position fades at the sides and bottom of the floor.
    float horizontalFade = smoothstep(0.15, 0.4, coords.x) * smoothstep(0.85, 0.6, coords.x);
    float verticalFade = smoothstep(1, 0, coords.y) * smoothstep(0, 0.05, coords.y) * 1.5;
    float positionFade = pow(verticalFade, 1 / shatterInterpolant) * horizontalFade;
    
    // Calculate crack color intensities.
    float2 crackCoords = (coords - 0.5) * float2(3, 1) * 0.7 + 0.5;
    float voronoiCrack = 0.2 / (tex2D(crackedTexture, crackCoords * 0.8) + 0.003);
    float noiseCrack = tex2D(noiseTexture, crackCoords * 4 + float2(0, globalTime * -0.1)) * tex2D(noiseTexture, crackCoords * 3 + float2(0, globalTime * -0.2));
    float crack = voronoiCrack * noiseCrack;
    
    // Combine everything together.
    float4 color = sampleColor * positionFade * crack * (shatterInterpolant * 0.6 + 1);
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}