sampler planetoidEdgeTexture : register(s1);
sampler disintegrationNoise : register(s2);
sampler crackNoise : register(s3);

float globalTime;
float heightRatio;

bool disintegrateOnlyAtHighlight;
float disintegrationCompletion;
float3 disintegrationColor;

float glowHighlightIntensity;
float2 glowHighlightDirection;
float3 glowHighlightColor;

float2 pixelationFactor;

float InverseLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}

float ScreenBlend(float a, float b)
{
    return a + b;
}

float3 ScreenBlend(float3 a, float3 b)
{
    return float3(ScreenBlend(a.r, b.r), ScreenBlend(a.g, b.g), ScreenBlend(a.b, b.b));
}

float4 ApplyHighlightBurnEffects(float4 color, float2 coords, float2 planetCoords, float distanceFromHighlight)
{
    float highlightBrightness = exp(distanceFromHighlight * -lerp(12, 5, glowHighlightIntensity)) * pow(glowHighlightIntensity, 0.6);
    
    // Apply an especially bright color effect near the highlight itself.
    color = lerp(color, float4(glowHighlightColor, 1) * color.a, highlightBrightness);
    
    // Calculate cracks for upcoming effects.
    float crack = tex2D(crackNoise, coords * 2 + float2(globalTime * 0.15, 0));
    float heatNoise = tex2D(disintegrationNoise, coords * 1.7 + float2(0, globalTime * 0.15));
    float distortedCrack = 1 / lerp(crack / heatNoise, crack * heatNoise, glowHighlightIntensity * 0.7);
    
    // Apply orange-red effects further away from the highlight.
    float crackOffsetNoise = (tex2D(disintegrationNoise, coords * 9) - 0.5) * 0.1;
    float moltenCrackInterpolant = smoothstep(0.06, 0, distanceFromHighlight - glowHighlightIntensity * 0.4 + crackOffsetNoise);
    float3 burnColor = float3(0.16, 0.04, 0.03) + float3(1, 0.32, 0.2) * distortedCrack * moltenCrackInterpolant;
    color = lerp(color, float4(burnColor, 1) * color.a, smoothstep(0.4, 0, highlightBrightness) * glowHighlightIntensity);
    
    return color;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate disintegration values.
    float2 disintegrationCoords = coords * 18;
    
    // Calculate the highlight position for later.
    float2 highlightPosition = 0.5 + glowHighlightDirection * 0.32;
    float distanceFromHighlight = distance(coords, highlightPosition);
    
    // Pixelate coords.
    coords = round(coords / pixelationFactor) * pixelationFactor;
    disintegrationCoords = round(disintegrationCoords / pixelationFactor) * pixelationFactor;
    
    // Use polar coordinates to determine the sampling of the planet strip.
    // This has a bit of distortion to it, but whatever.
    float radius = distance(coords, 0.5);
    float angle = atan2(coords.y - 0.5, coords.x - 0.5);
    float2 polarCoords = float2((angle + 3.141) / 6.283, radius);
    float2 planetCoords = float2(polarCoords.x, smoothstep(0.5, 0.5 - heightRatio * 2, polarCoords.y));
        
    // Apply heat distortion based on the distance from the highlight.
    float heatDistortionWave = sin(globalTime * 12 + polarCoords.x * 332) + sin(globalTime * 13 + polarCoords.x * 219 + 0.948);
    float heatDistortionIntensity = exp(distanceFromHighlight * -9) * pow(glowHighlightIntensity, 2);
    planetCoords.y += heatDistortionWave * heatDistortionIntensity * 0.023;
    
    // Ignore pixels beyond the boundary of the planet.
    if (planetCoords.y <= 0.01)
        return 0;
    
    // Apply jittering based on the amount of disintegration.
    planetCoords += float2(-sin(angle), cos(angle)) * sin(globalTime * 90 + angle * 4) * pow(disintegrationCompletion, 1.8) * 0.003;
    
    // Fade colors to black close to the center of the planet.
    float disintegrationNoiseValue = round(tex2D(disintegrationNoise, disintegrationCoords) * 6) * 0.1;
    float disintegrationRadius = radius + disintegrationNoiseValue * 0.04;
    float disintegration = disintegrationRadius < 1 - disintegrationCompletion;
    float blackInterpolant = smoothstep(0.9, 1, planetCoords.y);
    float4 color = tex2D(planetoidEdgeTexture, planetCoords) * sampleColor * disintegration;
    
    // Make colors get hot when disintegration is ongoing.
    float heatHighlight = exp(distanceFromHighlight * -lerp(18, 4.2, glowHighlightIntensity));
    float3 brightColor = ScreenBlend(color.rgb, disintegrationColor * pow(disintegrationCompletion, 2));
    brightColor += disintegrationNoiseValue * disintegrationCompletion * distanceFromHighlight * 1.3;
    
    color = lerp(color, float4(brightColor, 1) * color.a, disintegrateOnlyAtHighlight ? heatHighlight : 1);
    color = lerp(color, float4(0, 0, 0, 1), blackInterpolant);
    
    // Apply highlight glow effects.
    color = ApplyHighlightBurnEffects(color, coords, planetCoords, distanceFromHighlight);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}