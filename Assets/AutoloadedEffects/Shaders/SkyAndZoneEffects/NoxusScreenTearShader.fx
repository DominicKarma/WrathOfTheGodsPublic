sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler negativeTextureMap : register(s2);
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
float2x2 worldTransform;

// !!! Screen shader, do not delete the above parameters !!!
float2 noxusUV;

float4 InfiniteMirrorEffect(float maxIterations, float2 coords)
{
    float breakOutIteration = maxIterations;
    for (int i = 0; i < maxIterations; i++)
    {
        if (coords.x < 0.25 || coords.x > 0.75 || coords.y < 0.1 || coords.y > 0.75)
        {
            breakOutIteration = i;
            break;
        }

        coords = (coords - 0.5) * 1.2 + 0.5;
        coords.x += sin(i * 0.4 + uTime) * 0.01;
        coords.y += (sin(i / maxIterations * 6.283 - uTime * 2) * 0.5 + 0.5) * 0.09;
    }
    
    return tex2D(baseTexture, coords) * (1 - breakOutIteration / maxIterations);
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 noxusRelativeCoords = coords - noxusUV;
    float2 originalCoords = coords;
    float shoveDirection = noxusRelativeCoords.x < 0.5 ? 1 : -1;
    float innerNoise = tex2D(noiseTexture, originalCoords + float2(0, uTime * 0.1)).r * 16.2019;
    float shoveDistance = (pow(noxusRelativeCoords.y * 1.33, 2) * 0.15 + 0.07) * uIntensity;
    float shoveOffset = shoveDirection * shoveDistance;
    
    float distanceFromHorizontalCenter = min(distance(originalCoords.x, noxusUV.x), distance(coords.x, noxusUV.x));
    float innerColorEdgeInterpolant = (distanceFromHorizontalCenter + sin(innerNoise * 1.25) * 0.005) / abs(shoveOffset);
    float innerColorInterpolant = smoothstep(1, 0.85, innerColorEdgeInterpolant);
    float negativeMapAlpha = tex2D(negativeTextureMap, mul(coords - 0.5, worldTransform) + 0.5).a;
    innerColorInterpolant = lerp(innerColorInterpolant, negativeMapAlpha, negativeMapAlpha > 0);
    
    float4 innerColor = InfiniteMirrorEffect(15, originalCoords);
    innerColor.rgb = 1 - innerColor.rgb;
    
    float dualSplitColorIncrement = uIntensity * 0.3;
    float4 color = tex2D(baseTexture, coords);
    if (noxusRelativeCoords.x < 0)
        color.r += dualSplitColorIncrement;
    else
        color.gb += float2(0.75, 1) * dualSplitColorIncrement;
    
    return lerp(color, innerColor, innerColorInterpolant) - QuadraticBump(innerColorInterpolant) * 3;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}