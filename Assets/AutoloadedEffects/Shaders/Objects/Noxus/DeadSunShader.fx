texture ironTexture;
sampler2D ironTextureSampler = sampler_state
{
    texture = <ironTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

sampler crackNoise : register(s1);
sampler surfaceNoise : register(s2);

float globalTime;
float sphereSpinTime;

float heatCrackEmergePower;
float heatCrackGlowIntensity;
float specialHeatCrackIntensity;
float heatDistortionMaxOffset;
float2 specialHeatCrackDirection;
float3 metalCrackColor;

float metalCrackIntensity;
float metalCrackSize;
float metalCrackHeatInterpolant;

float highlightSharpness;
float minHighlightBrightness;
float maxHighlightBrightness;
float2 highlightPosition;
float3 brightHighlightColor;
float3 dullHighlightColor;

float underglowStartInterpolant;
float underglowEndInterpolant;
float3 underglowColor;

float collapseInterpolant;

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 ApplyMetalCrackEffects(float4 baseColor, float2 sphereCoords, float crack)
{
    // Apply subtractive blending with the crack color.
    baseColor -= float4(1 - metalCrackColor, 0) * metalCrackIntensity * crack;
    
    // Brighten colors moderately darkened by the cracks.
    // This is done to help make it look like the contours of the cracks are natural, with parts that are naturally exposed to light.
    float luminosity = (baseColor.r + baseColor.g + baseColor.b) * 0.3333;
    float edge = QuadraticBump(smoothstep(0.01, 0.4, luminosity)) * lerp(0.25, 0.3, metalCrackHeatInterpolant);
    
    return baseColor + edge * baseColor.a;
}

float4 ApplyHeatCrackEffects(float4 baseColor, float2 sphereCoords, float metalCrack, float highlightBrightness, float specialCrackIntensity)
{
    // Sample two noise textures to calculate the crack's texture.
    float crackNoiseIntensity = saturate(tex2D(crackNoise, sphereCoords * 1.8) + tex2D(surfaceNoise, sphereCoords * 0.7));
    
    // Increase the crack noise near the special crack position.
    crackNoiseIntensity -= saturate(specialCrackIntensity) * specialHeatCrackIntensity;
    
    float metalInfluence = (smoothstep(0.8, 1.7, metalCrack) * metalCrack * metalCrackHeatInterpolant + sin(globalTime * 0.7 + sphereCoords.x * 20) * 0.2) * 0.25;
    
    float crack = crackNoiseIntensity * 0.75 + distance(sphereCoords, 0.5) * heatCrackEmergePower - metalInfluence;
    
    // Create the outer red aesthetic.
    baseColor.r += smoothstep(0.41, 0.37, crack) * heatCrackGlowIntensity / highlightBrightness;
    
    // Create the semi-inner orange aesthetic. Since this is green it will combine with the aforementioned red to create oranges and yellows.
    baseColor.g += smoothstep(0.37, 0.27, crack) * heatCrackGlowIntensity / highlightBrightness;
    
    // Create the inner white aestheic. Since this is blue it will combined with the aforementioned red and green to create a bright white color.
    baseColor.b += smoothstep(0.26, 0.25, crack) * heatCrackGlowIntensity / highlightBrightness;
    
    // Darken the edges to black for contrast reasons.
    baseColor = lerp(baseColor, float4(0, 0, 0, 1), smoothstep(0.4, 0.41, crack) * smoothstep(0.46, 0.41, crack) * 0.6 - metalInfluence * 1.5);
    
    // Return the result.
    return baseColor;
}

float4 ApplyBottomUnderglow(float4 baseColor, float2 sphereCoords)
{
    // Calculate the glow intensity based on the distance to the bottom of the sphere, as well as the opacity of the color.
    // If the opacity is zero, this naturally cancels out, to prevent additively drawn colors from interfering with invisible pixels.
    float glowInterpolant = smoothstep(underglowStartInterpolant, underglowEndInterpolant, sphereCoords.y) * baseColor.a;
    
    // Combine the glow interpolant with the underglow color.
    float4 glowColor = float4(underglowColor, 1) * glowInterpolant;
    
    // Apply additive blending.
    return baseColor + glowColor;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the pixel's angle from the center.
    float angle = atan2(coords.y - 0.5, coords.x - 0.5);
    
    // Calculate the specular highlight based on distance to the highlight's position.
    float highlight = exp(distance(coords, highlightPosition) * -highlightSharpness);
    float highlightBrightness = lerp(minHighlightBrightness, maxHighlightBrightness, highlight);
    
    // Calculate the highlight color.
    float4 highlightColor = float4(lerp(dullHighlightColor, brightHighlightColor, highlight), 1) * highlightBrightness;
    
    // Ensure that the highlight doesn't make colors transparent. That'd be silly!
    highlightColor.a = 1;
    
    // Calculate information pertaining to the special heat crack visual.
    float2 specialCrackPosition = specialHeatCrackDirection * 0.35 + 0.5;
    float specialCrackIntensity = 0.12 / distance(coords, specialCrackPosition + tex2D(surfaceNoise, coords * 3 + float2(sphereSpinTime * 3, 0)).r * pow(specialHeatCrackIntensity, 2) * 0.4);
    specialCrackIntensity = pow(specialCrackIntensity, 2);
    
    // Calculate the distance to the center of the star. This is magnified a bit for intensity purposes in later equations.
    float2 coordsNormalizedToCenter = (coords - 0.5) * 2;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    
    // Calculate a glow color in accordance with the special crack.
    float4 glowBaseColor = float4(1, 0.01 + specialCrackIntensity * 0.2, 0, 1);
    float4 glow = glowBaseColor * smoothstep(0.1, 0.7, distanceFromCenterSqr) * smoothstep(0.8, 0.71, distanceFromCenterSqr) * pow(specialCrackIntensity, 1.15) * specialHeatCrackIntensity * (specialHeatCrackIntensity > 0.08) * 3;
    distanceFromCenterSqr += cos(angle * 12.5 + globalTime * 9) * length(glow) * heatDistortionMaxOffset;
    
    // Ensure that the star is cut off at the edges to ensure a circular shape.
    float starOpacity = smoothstep(0.707, 0.68, distanceFromCenterSqr);
    
    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.001;
    
    // Exaggerate the pinch slightly.
    spherePinchFactor = pow(spherePinchFactor, 1.5);
    
    float2 sphereCoords = frac((coords - 0.5) * spherePinchFactor + 0.5 + float2(sphereSpinTime, 0));
    
    // Calculate crack values for the metal crack.
    float metalCrack = pow(1 - tex2D(crackNoise, sphereCoords * 3.8), 2.5) * (1 + metalCrackSize);
    
    // Ensure that the crack gets cut off naturally. This is meant to be a bit like a step function except a bit more smooth.
    metalCrack *= smoothstep(0.38, 0.7, metalCrack);
    
    // Calculate the final color, applying metallic and heat cracks.
    float4 baseColor = pow(tex2D(ironTextureSampler, sphereCoords * 2), 1.5) * sampleColor;
    baseColor = ApplyMetalCrackEffects(baseColor, sphereCoords, metalCrack);
    baseColor = ApplyHeatCrackEffects(baseColor, sphereCoords, metalCrack, highlightBrightness, specialCrackIntensity);

    // Combine everything together.
    float brightness = collapseInterpolant * 0.6 + 1;
    float4 result = (baseColor * sampleColor * starOpacity * highlightColor + glow) * brightness;
    
    // Apply a bottom underglow to the sphere from Noxus' lily.
    result = ApplyBottomUnderglow(result, sphereCoords);
    
    return result + result.a * collapseInterpolant * 0.6;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}