sampler baseTexture : register(s0);
sampler textureNoiseTexture : register(s1);
sampler uvOffsetNoiseTexture : register(s2);

float globalTime;
float brightnessIntensity;
float aspectRatio;
float distortionRadius;
float2 distortionCenter;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float2 AspectRatioCorrect(float2 coords)
{
    return (coords - 0.5) * float2(1, aspectRatio) + 0.5;
}

float2 RotatedBy(float2 v, float theta)
{
    float s = sin(theta);
    float c = cos(theta);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float3 CalculateBackColor(float2 coords)
{
    float time = globalTime * 0.2;
    
    // Calculate the distance to the center. This is magnified a bit for intensity purposes in later equations.
    float2 coordsNormalizedToCenter = (coords - distortionCenter) * 1.414;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    
    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.005;
    float2 sphereCoords = coords * spherePinchFactor + float2(0, time * 0.9);
    
    // Calculate the fog brightness texture from the sphere coords.
    float fogCoordsOffset = tex2D(textureNoiseTexture, sphereCoords).r * 0.21 - time * 0.2;
    float2 fogCoords = sphereCoords * 0.4 + float2(fogCoordsOffset, 0);
    float fogBrightnessTexture1 = tex2D(textureNoiseTexture, fogCoords).r;
    float fogBrightnessTexture2 = tex2D(textureNoiseTexture, fogCoords * 0.8).r * 0.1;
    float fogBrightnessTexture3 = tex2D(textureNoiseTexture, fogCoords * 0.51).r;
    
    // Calculate the distortion color, using noise textures to have variance between distortions, dark greens, and blues.
    float3 distortionColor = lerp(0, float3(0.12, 0.011, 0.129), fogBrightnessTexture1);
    distortionColor += float3(-0.3, 1.4, 1.3) * fogBrightnessTexture2;
    distortionColor -= fogBrightnessTexture3 * 0.19;
    
    return distortionColor;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance from the center of the field.
    float distanceFromdistortion = distance(AspectRatioCorrect(coords), AspectRatioCorrect(distortionCenter));
    
    // Calculate edge offset values, so that it's not just a plain, pure circle edge.
    float edgeOffset1 = tex2D(textureNoiseTexture, coords + float2(globalTime * -0.12, 0)).r * 0.07;
    float edgeOffset2 = tex2D(textureNoiseTexture, coords * 1.1 + float2(globalTime * 0.15, 0.5478 + edgeOffset1 * 8)).r * 0.06;
    
    // Distort the background.
    float distortionInterpolant = InverseLerp(distortionRadius + edgeOffset1 + edgeOffset2 + 0.07, distortionRadius, distanceFromdistortion);
    float gravitationalLensingAngle = exp(-distanceFromdistortion / distortionRadius * 3.2) * 2.64;
    coords = RotatedBy(coords - 0.5, gravitationalLensingAngle) + 0.5;
    
    // Combine colors in a subtle way.
    float4 color = lerp(tex2D(baseTexture, coords), float4(CalculateBackColor(coords), 1), distortionInterpolant * 0.24) * (1 + distortionInterpolant * brightnessIntensity);
    
    // Err slightly towards a negative color.
    float3 negative = 1 - tex2D(baseTexture, coords).rgb;
    color = lerp(color, float4(negative, 1), pow(saturate((1 - distanceFromdistortion / distortionRadius) * 1.6), 3));
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}