sampler baseTexture : register(s0);
sampler behindSplitTexture : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

// !!! Screen shader, do not delete the above parameters !!!

float2 splitCenters[10];
float splitSlopes[10];
float2 splitDirections[10];
float splitWidths[10];
bool activeSplits[10];

bool offsetsAreAllowed;

float splitBrightnessFactor;
float splitTextureZoomFactor;

float InverseLerp(float to, float from, float x)
{
    return saturate((x - to) / (from - to));
}

float SignedDistanceToLine(float2 p, float2 linePoint, float2 lineDirection)
{
    return dot(lineDirection, p - linePoint);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance from the screen coordinate to the lines, along with the necessary contributing brightness from that distance.
    float2 offset = 0;
    float brightnessBoost = 0;
    
    for (float i = 0; i < 10; i++)
    {
        if (activeSplits[i])
        {
            float signedLineDistance = SignedDistanceToLine(coords, splitCenters[i], splitDirections[i] * float2(1, uScreenResolution.y / uScreenResolution.x));
            float lineDistance = abs(signedLineDistance);
            brightnessBoost += splitWidths[i] / (lineDistance + 0.001) * 0.3;
    
            // Calculate how much both sides of the line should be shoved away from the line.
            offset += splitDirections[i] * sign(signedLineDistance) * splitWidths[i] * -0.5;
        }
    }
    
    // Calculate colors.
    float4 baseColor = tex2D(baseTexture, coords + offset * offsetsAreAllowed) + brightnessBoost;
    float4 backgroundDimensionColor1 = tex2D(behindSplitTexture, coords * splitTextureZoomFactor + float2(uTime, 0) * -0.23) * splitBrightnessFactor;
    float4 backgroundDimensionColor2 = tex2D(behindSplitTexture, coords + backgroundDimensionColor1.rb * splitTextureZoomFactor * 0.12) * splitBrightnessFactor * 0.5;
    float4 backgroundDimensionColor = backgroundDimensionColor1 + backgroundDimensionColor2;
    
    // Combine colors based on how close the pixel is to the line.
    float brightness = pow(InverseLerp(0.06, 0.4, brightnessBoost), 2);
    return lerp(baseColor, backgroundDimensionColor, brightness);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}