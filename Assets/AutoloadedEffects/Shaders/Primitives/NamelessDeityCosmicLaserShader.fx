sampler laserColorTexture : register(s1);
sampler edgeFadingNoiseTexture : register(s2);
sampler edgeFadingNoiseBackLineTexture : register(s3);
sampler darknessTexture : register(s4);
sampler starsTexture : register(s5);
sampler darkeningNoiseTexture : register(s6);

float globalTime;
float uStretchReverseFactor;
float scrollOffset;
float scrollSpeedFactor;
float lightSmashWidthFactor;
float lightSmashLengthFactor;
float lightSmashLengthOffset;
float lightSmashEdgeNoisePower;
float lightSmashOpacity;
float startingLightBrightness;
float maxLightTexturingDarkness;
float2 playerCoords;
float2 laserDirection;
float2 uCorrectionOffset;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos + float4(uCorrectionOffset.x, uCorrectionOffset.y, 0, 0);
    output.Position.z = 0;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float OpacityFromCutoffInterpolant(float cutoffInterpolant, float edgeOffset, float edgeCenter, float maxWidth)
{
    float edgeCutoff = 1 - pow(1 - cutoffInterpolant, 10);
    float edgeOpacity = InverseLerp(maxWidth, maxWidth * 0.83, distance(edgeOffset, edgeCenter) / edgeCutoff);
    
    return edgeOpacity;
}

float CalculateBackLineInterpolant(float2 coords, float2 zeroCenteredCoords)
{
    // Calculate various noise values. These are used to give variance to the overall light smash line.
    float noise1 = tex2D(edgeFadingNoiseBackLineTexture, coords * 0.6 + float2(globalTime * -1.34, 0)).r;
    float noise2 = tex2D(edgeFadingNoiseBackLineTexture, coords * 1.42 + float2(globalTime * -1.97, 0)).r;
    float noise3 = tex2D(edgeFadingNoiseBackLineTexture, coords * 9.3 + float2(globalTime * -3.67, 0)).r;
    float noise4 = tex2D(edgeFadingNoiseBackLineTexture, coords * 5 - float2(globalTime * 2.27, noise2)).r;
    
    // For ease of use, store player distance values in a 2D vector.
    // X = distance relative to the length of the laser.
    // Y = distance relative to the width of the laser.
    float2 distancesFromPlayer = float2(zeroCenteredCoords.x - playerCoords.x - lightSmashLengthOffset, distance(zeroCenteredCoords.y, playerCoords.y));
    
    // Calculate the edge fade. This will determine how much the light should fade out based on horizontal distance. This is changeable with parameters and varies a bit based on noise.
    float edgeFade = InverseLerp(0.1, 0.08, (distancesFromPlayer.y - (noise3 - 0.5) * lightSmashEdgeNoisePower * 0.076) / lightSmashWidthFactor);
    
    // Calculate the fade for the start of the light. This will ensure that it only draws past the player. This can be shifted backwards and forwards based on the lightSmashLengthOffset parameter.
    float lengthStartInterpolant = InverseLerp(0, lightSmashWidthFactor * 0.3, distancesFromPlayer.x);
    float lengthStartFade = OpacityFromCutoffInterpolant(lengthStartInterpolant, coords.y, playerCoords.y + 0.5, lightSmashWidthFactor) * InverseLerp(0, 0.012, distancesFromPlayer.x);
    
    // Calculate the fade for the end of the light. This ensures customizable light lengths, so that it can look like the light is "emerging" from the player source.
    float lengthEndFade = InverseLerp(0.2, 0.193, distancesFromPlayer.x / lightSmashLengthFactor + noise4 * 0.046);
    
    // Combine all interpolants and take into account the opacity parameter.
    float backLineInterpolant = edgeFade * lengthStartFade * lengthEndFade;
    return backLineInterpolant * lightSmashOpacity;
}

float4 CalculateSpaceColor(float2 coords)
{
    float time = globalTime * 0.2;
    float4 fadeMapColor1 = tex2D(laserColorTexture, float2(coords.x * 2 - time * 1.6, coords.y));
    float4 fadeMapColor2 = tex2D(laserColorTexture, float2(coords.x * 3 - time * 0.8, coords.y * 0.5));
    float opacity = (1 + fadeMapColor1.g);
    
    // Apply bloom to the resulting colors.
    float4 finalColor = float4(0.196, 0.07, 0.392, 1) * opacity;
    finalColor += fadeMapColor2 * finalColor.a;
    
    // Saturate everything a bit.
    finalColor.rgb = pow(finalColor.rgb * 1.09, 2.2) * 0.25;
    
    return finalColor;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 baseCoords = input.TextureCoordinates;
    
    // Account for texture distortion artifacts in accordance with the primitive distortion fixes.
    baseCoords.y = (baseCoords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float2 coords = baseCoords + float2(-0.034, 0);
    
    // Calculate edge cutoff values. This ensures that the start of the laser is not flat and roughly matches the shape of the magic circle.
    float edgeOpacity = OpacityFromCutoffInterpolant(InverseLerp(0, 0.1, coords.x), coords.y, 0.5, 0.56);
    float4 baseColor = color * edgeOpacity * InverseLerp(0, 0.011, coords.x);
    
    // Alter the laser such that pixels at the edges are more squished. Also apply scrolling.
    float squishFactor = 1 - saturate(pow(distance(coords.y, 0.5) * 2, 15)) * 0.3;
    float2 scrolledCoords = coords + float2(coords.y * 0.015 + globalTime * -0.55, 0);
    float2 cylindricalCoords = (frac(scrolledCoords) - 0.5) * float2(1, squishFactor) + 0.5;
    
    // Make the coordinates have a bit of a sideways slant, to help reinforce the visual that the laser is a cylinder.
    cylindricalCoords += float2(-0.4, 0.56) * globalTime;
    
    // Calculate the texture of the laser based on the aforementioned cylindrical coordinates.
    float4 spaceColor = CalculateSpaceColor(cylindricalCoords) * pow(QuadraticBump(coords.y), 0.51);
    
    // Calculate noise values.
    float noise1 = tex2D(edgeFadingNoiseTexture, cylindricalCoords * 0.6).r;
    float noise2 = tex2D(edgeFadingNoiseTexture, cylindricalCoords * 1.42).r;
    float noise3 = tex2D(edgeFadingNoiseTexture, coords * 7 - float2(globalTime * 2.97, noise2)).r;
    
    float4 finalColor = saturate(baseColor * spaceColor);
    float3 backColor = startingLightBrightness - dot(finalColor.rgb, float3(0.3, 0.59, 0.11)) * maxLightTexturingDarkness;
    return lerp(finalColor, float4(backColor, 1), CalculateBackLineInterpolant(coords, baseCoords - 0.5));
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
