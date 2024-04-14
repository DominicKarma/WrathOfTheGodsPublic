sampler screenTexture : register(s0);
sampler overlayTexture : register(s2);

float globalTime;
float intensity;
float4 palette[8];

float4 PaletteLerp(float interpolant)
{
    int startIndex = clamp(interpolant * 6, 0, 6);
    int endIndex = startIndex + 1;
    return lerp(palette[startIndex], palette[endIndex], frac(interpolant * 6));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Store the original color, before any distortions are applied.
    float4 baseColor = tex2D(screenTexture, coords);
    
    // Apply wavy distortions to the distorted color.
    float2 originalCoords = coords;
    coords.y += (sin(coords.x * 300 - coords.y * 32 + globalTime * 20) * 0.004 + sin(coords.x * 20 + coords.y * 105 + globalTime * 10) * 0.003) * intensity;
    
    // Calculate colors.
    float4 color = tex2D(screenTexture, coords);
    float blurInterpolant = smoothstep(0.2, 0.05, distance(coords, 0.5));
    float4 blurredColor = 0;
    for (int i = -6; i < 6; i++)
        blurredColor += tex2D(screenTexture, coords + float2(i, 0) * intensity * 0.001) / 13;
    color = lerp(color, blurredColor, blurInterpolant);
    
    // Interpolate between the palette based on the luminosity of the color, along with time.
    float luminosity = dot(color.rgb, float3(0.3, 0.6, 0.1));
    float4 evilColor = PaletteLerp(sin(luminosity * 3.141 - globalTime * 0.75) * 0.5 + 0.5);
    
    // Apply a vignette.
    evilColor -= distance(coords, 0.5) * 0.6;
    
    // Apply overlays.
    float4 overlayColor = tex2D(overlayTexture, lerp(coords, originalCoords, 0.6));
    evilColor = lerp(evilColor, overlayColor, overlayColor.a);
    
    return lerp(baseColor, evilColor, intensity);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
