sampler baseTexture : register(s0);
sampler windNoiseTexture : register(s1);
sampler backgroundNoiseTexture : register(s2);
sampler flashNoiseTexture : register(s3);

float time;
float scrollSpeed;
float noiseZoom;
float intensity;
float flashNoiseZoom;
float flashIntensity;
float2 flashCoordsOffset;
float2 flashPosition;
float2 screenPosition;
float3 backgroundColor1;
float3 backgroundColor2;

float2 noxusPosition;
float darknessIntensity;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    
    // Calculate the distance from the flash.
    float distanceFromFlash = distance(coords, flashPosition);
    
    // Perform multiple iterations of noise for the final results.
    float noise = 0;
    float previousNoise = 0;
    float whiteInterpolant = 0;
    float2 worldOffset = screenPosition * 0.00002;
    for (int i = 0; i < 4; i++)
    {
        // Calculate local intensity for the noise layer. This exponentially tapers off in successive layers.
        float intensity = pow(1.23, -i);
        
        // Calculate the time value for the given layer. Layers in the front are faster than layers in the back.
        float animationTime = -time / intensity * scrollSpeed;
        
        // Calculate noise coordinates. These are squished a bit to give an illusion of wind.
        float verticalNoiseOffset = tex2D(backgroundNoiseTexture, coords * 10 + time * 0.36 - screenPosition * 0.00015) * 0.008;
        float2 noiseCoords = coords * (1.95 + i * 0.6) + float2(animationTime + previousNoise * 0.12, sin(animationTime * 20 + previousNoise + coords.x * 19.5) * 0.075 + verticalNoiseOffset);
        noiseCoords += worldOffset * 2;
        noiseCoords *= float2(0.25, 0.5);
        
        // Interpolate between wind and idle background colors based on how far back the layer is.
        float wind = tex2D(windNoiseTexture, noiseCoords);
        float background = tex2D(backgroundNoiseTexture, noiseCoords * (13 - i * 5.3));
        float localNoise = lerp(wind, background, pow(1 - intensity, 9.5)) * intensity;
        
        // Incorporate flashes into the back layers.
        if (i >= 2)
        {
            float localFlashIntensity = intensity * InverseLerp(0.13, 0.04, distanceFromFlash) * flashIntensity * (1 - background) * 0.08;
            localNoise += localFlashIntensity * 0.97;
            whiteInterpolant += pow(localFlashIntensity, 2) * 0.4;
        }
        
        // Add noise.
        noise += localNoise;
        previousNoise = localNoise + 1.25;
    }

    // Combine colors based on separate noise calculations.
    float backgroundColorNoise = tex2D(windNoiseTexture, coords * 0.4 + noise * 0.02);
    float3 backgroundColor = lerp(backgroundColor1, backgroundColor2, backgroundColorNoise) * noise * (0.817 + flashIntensity * 0.003);
    backgroundColor = pow(backgroundColor, float3(3.3, 5, 3.9) + noise - 1.1);
    
    // Interpolate towards red at points, to give variety against the purples.
    float redPower = noise * lerp(0.11, 0.36, cos(time * -0.4 + coords.x * 20) * 0.5 + 0.5);
    backgroundColor *= float3(1 + redPower, 1, 1 - redPower * 1.33);
    
    // Make the background around Noxus pitch black.
    float distanceToNoxus = distance(noxusPosition, coords);
    float distortedDistanceToNoxus = distanceToNoxus - frac(abs(previousNoise)) * 0.1;
    float blackInterpolant = InverseLerp(0.15, 0.05, distortedDistanceToNoxus) * darknessIntensity;
    backgroundColor = lerp(backgroundColor, 0, blackInterpolant);
    
    // Apply brightness effects.
    backgroundColor = lerp(backgroundColor, 1, saturate(whiteInterpolant));
    
    // Return the final result.
    float opacity = lerp(0.98, 1, blackInterpolant);
    return (color * float4(backgroundColor, 1)) * intensity * opacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}