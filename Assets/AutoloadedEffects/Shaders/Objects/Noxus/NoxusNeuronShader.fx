sampler crackTexture : register(s1);
sampler distortionTexture : register(s2);
sampler nucleusAccentTexture : register(s3);

float globalTime;
float identity;
float scale;
float neuronCenterTranslucency;
float neuronShapeDistortion;

float neuronHighlightIntensity;
float2 neuronHighlightPosition;

float nucleusShapeDistortion;
float nucleusHighlightIntensity;
float2 nucleusHighlightPosition;

float4 nucleusColor;
float3 neuronAccentColor;

float2x2 RotationMatrix(float angle)
{
    float sine = sin(angle);
    float cosine = cos(angle);
    return float2x2(cosine, -sine, sine, cosine);
}

float CrackAtPoint(float2 coords, float rotation)
{
    // Rotate coordinates.
    coords = mul((coords - 0.5), RotationMatrix(rotation)) + 0.5;
    return 1 - tex2D(crackTexture, coords);
}

float4 CalculateNeuronOpacity(float2 coords)
{
    // Calculate the distance distortion based on noise. This will influence the shape of the neuron somewhat.
    float distanceDistortion = tex2D(distortionTexture, coords + float2(identity, globalTime * 0.045)) * neuronShapeDistortion;
    
    // Calculate the distance from the center of the neuron based on the distortion.
    float distanceFromCenter = distance(coords, 0.5) - distanceDistortion;
    
    // Calculate the base opacity. This will create the base shape of the neuron.
    float baseOpacity = smoothstep(0.21, 0.2, distanceFromCenter);
    
    // Calculate the opacity fadeout, which will make the neuron a bit more translucent in the center, allowing the nucleus to show.
    float centerOpacityFadeout = smoothstep(0.19, 0.12, distanceFromCenter) * neuronCenterTranslucency;
    
    // Combine the opacities.
    return baseOpacity - centerOpacityFadeout;
}

float CalculateNeuronBranchCracks(float2 coords)
{
    // Calculate the neuron crack coordinates.
    float2 crackCoords = coords * 0.5 + float2(0, 0.06) + 0.1;
    
    // Calculate the crack intensity value based on two texture samples.
    float baseRotation = identity + globalTime * 0.02;
    float crack = saturate(CrackAtPoint(crackCoords, baseRotation) + CrackAtPoint(crackCoords * 1.6, identity * -0.618 - 0.754 - globalTime * 0.014));
    crack = 1 - crack;
    
    // Calculate the distance opacity fade.
    float distanceFromCenter = distance(coords, 0.5);
    float distanceFade = smoothstep(0.3, 0.28, distanceFromCenter + crack * 0.18);
    
    // Sharpen the opacity values.
    crack = smoothstep(0.2, 0.85, crack);
    
    // Combine everything together.
    return (1 - crack) * distanceFade;
}

float4 CalculateNeuronColor(float2 coords, float opacity, float4 sampleColor)
{
    // Calculate the accent color of the neuron. This will be applied additively.
    float accentInterpolant = tex2D(nucleusAccentTexture, coords * 1.96 + identity * 0.19).r * sampleColor.a;
    float4 accent = float4(neuronAccentColor, 0) * accentInterpolant;
    
    // Combine the sample color and accent color, applying the opacity.
    return (sampleColor + accent) * opacity;
}

float CalculateHighlightBrightness(float2 coords, float highlightIntensity, float2 highlightPosition)
{
    // Use the useful exp(-a * x) trick for opacity taper-offs.
    float highlightBrightnessInterpolant = exp(distance(coords, highlightPosition) * -highlightIntensity);
    return highlightBrightnessInterpolant;
}

float4 CalculateCellNucleusColor(float2 coords)
{
    // Calculate the distance distortion based on noise. This will influence the shape of the nucleus somewhat.
    float distanceDistortion = tex2D(distortionTexture, coords * 0.9 + float2(identity + 0.548, globalTime * 0.081)) * nucleusShapeDistortion;
    
    // Calculate the distance from the center of the neuron based on the distortion.
    float distanceFromCenter = distance(coords, 0.5) - distanceDistortion;
    
    // Calculate the opacity based on the distance from the center of the neuron.
    float opacity = smoothstep(0.08, 0.071, distanceFromCenter) * 0.56;
    float4 accent = float4(-1, -1, -0.5, 0) * tex2D(nucleusAccentTexture, coords * 1.7).r;
    
    // Calculate the nucleus color based on the accent and opacity.
    float4 accentedNucleusColor = (nucleusColor + accent) * opacity;
    accentedNucleusColor.a = opacity * 2;
    
    // Calculate a sharp highlight brightness.
    float highlightBrightness = lerp(0.3, 2, CalculateHighlightBrightness(coords, nucleusHighlightIntensity, nucleusHighlightPosition));
    
    // Apply the highlight brightness to the accented color.
    accentedNucleusColor *= float4(highlightBrightness, highlightBrightness, highlightBrightness, 1);
    
    // Return the result.
    return accentedNucleusColor;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Scale coordinates for the neuron itself, and calculate the distance from said coordinates.
    float2 scaledCoords = (coords - 0.5) / scale + 0.5;
    float distanceFromCenter = distance(scaledCoords, 0.5);
    
    // Calculate the neuron color.
    float opacity = CalculateNeuronOpacity(scaledCoords);
    float4 neuronColor = CalculateNeuronColor(coords, CalculateNeuronBranchCracks(coords) * (1 - opacity) * (distanceFromCenter > 0.2) + opacity, sampleColor);
    
    // Combine the neuron color with the nucleus color.
    float4 result = neuronColor + CalculateCellNucleusColor(scaledCoords) * sampleColor.a;
    
    // Lastly, apply a highlight to the overall neuron.
    float highlightBrightness = lerp(0.24, 1.5, CalculateHighlightBrightness(coords, neuronHighlightIntensity, neuronHighlightPosition));
    return result * float4(highlightBrightness, highlightBrightness, highlightBrightness, 1);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}