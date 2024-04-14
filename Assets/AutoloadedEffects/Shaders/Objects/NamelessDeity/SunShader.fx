sampler fireNoiseTexture : register(s0);
sampler accentNoiseTexture : register(s1);
sampler uvOffsetNoiseTexture : register(s2);

float globalTime;
float coronaIntensityFactor;
float sphereSpinTime;
float3 mainColor;
float3 darkerColor;
float3 subtractiveAccentFactor;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance to the center of the sun. This is magnified a bit for intensity purposes in later equations.
    float2 coordsNormalizedToCenter = coords * 2 - 1;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    float starOpacity = InverseLerp(0.5, 0.42, distanceFromCenterSqr);
    
    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.045;
    float2 sphereCoords = coords * spherePinchFactor + float2(sphereSpinTime, 0);
    
    // Calculate the star brightness texture from the sphere coordinates.
    float starCoordsOffset = tex2D(fireNoiseTexture, sphereCoords).r * 0.41 + globalTime * 0.3;
    float2 starCoords = sphereCoords + float2(starCoordsOffset, 0);
    float3 starBrightnessTexture = tex2D(fireNoiseTexture, starCoords);
    
    // Calculate the glow interpolant. The closer a pixel is to the center, the stronger this value is.
    float starGlow = saturate(1 - distanceFromCenterSqr * 0.91);
    
    // Combine various aforementioned values into the base result:
    // 1. The result is brighter the higher the pinch factor is. This makes colors at the cross direction edges a little bit weaker.
    // 2. The result is brighter the higher the star glow is.
    // 3. The result is brightened relative to the brightness texture. This gives variance in the brightness of the result, and keeps it crisp.
    float3 result = spherePinchFactor * mainColor * 0.777 + starGlow * darkerColor + starBrightnessTexture;
    
    // Apply subtractive texturing to the result, skewing things towards a darker orange red based on accent noise and the inverse of the brightness.
    // This allows the result to have darker patches on it.
    result = lerp(result, darkerColor, saturate(1 - starBrightnessTexture.r) * 0.8);
    result -= (1 - subtractiveAccentFactor) * tex2D(accentNoiseTexture, sphereCoords * 2).r * 1.1;
    
    // Apply sharp brightness texturing to the result, as though lava is flowing through it. The textures this creates are thin but very bright, like lava rivers.
    float2 uvOffset = tex2D(uvOffsetNoiseTexture, coords + float2(0, globalTime * 0.4));
    result += pow(tex2D(accentNoiseTexture, sphereCoords * 1.2 + uvOffset * 0.04).r, 2) * 2.1;
    
    // Calculate the corona brightness. This effect weakens on the star itself (obviously, it's a corona) and slowly dissipates the further out the pixel is from the center.
    // This gives a very, very strong bright edge to the result that dissipates in a manner similar to a radial bloom (except with a little bit of noise-based randomness).
    float coronaFadeOut = InverseLerp(0.2, 0.5, distanceFromCenterSqr) * InverseLerp(1.91, 0.98, distanceFromCenterSqr) * coronaIntensityFactor;
    float coronaBrightness = coronaFadeOut / abs(distanceFromCenterSqr - 0.5 + uvOffset.y * 0.04 + 0.04);
    
    // Combine everything together.
    return (starOpacity * float4(result, 1) + float4(mainColor, 1) * coronaBrightness) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}