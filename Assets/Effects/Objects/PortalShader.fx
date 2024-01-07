sampler baseTexture : register(s0);
sampler fadeoutDistanceLookupTexture : register(s1);
sampler uvOffsetNoise : register(s2);
sampler innerTexture : register(s3);
sampler lightSpikesEdgeTexture : register(s4);

float globalTime;
float circleStretchInterpolant;
float edgeFadeInSharpness;
float aheadCircleZoomFactor;
float aheadCircleMoveBackFactor;
float spaceBrightness;
float2 spaceTextureOffset;
float2 spaceTextureZoom;
float2 aimDirection;
float3 generalColor;
float2x2 transformation;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Apply the matrix transformation to the coordinates.
    coords = mul(coords - 0.5, transformation) + 0.5;
    
    // Calculate a UV offset from noise. This is used to give offsets to the edges of the portal.
    float uvOffsetAngle = tex2D(uvOffsetNoise, coords) * 32 + globalTime * 20;
    float2 uvOffset = float2(cos(uvOffsetAngle), sin(uvOffsetAngle)) * pow(circleStretchInterpolant, 2) * 0.013;
    
    // Calculate distance values for the portal edge.
    // This uses a premade texture that has all the distances from a five-sided star saved, since the underlying trig used to calculate that is quite computationally
    // intensive for a small shader like this to be doing on the fly.
    float distanceFromCenter = distance(coords + uvOffset, 0.5);
    float portalDistance = lerp(tex2D(fadeoutDistanceLookupTexture, float2(coords.x, 1 - coords.y)) * circleStretchInterpolant * 0.11, 0.3, pow(circleStretchInterpolant, 7.3));
    float signedDistanceFromPortalEdge = portalDistance - distanceFromCenter;
    float distanceFromPortalEdge = abs(signedDistanceFromPortalEdge);
    
    // Calculate portal colors.
    float portalBrightness = pow(0.132 / (distanceFromPortalEdge * 2), 2);
    float edgeFade = InverseLerp(-0.2, -0.09, signedDistanceFromPortalEdge);
    float4 finalColor = tex2D(baseTexture, coords) * sampleColor * float4(generalColor, 1) * portalBrightness;
    
    // Calculate the space color for the inside of the portal.
    float4 spaceColor = tex2D(innerTexture, frac(coords * spaceTextureZoom + globalTime * float2(0.1, 0) + spaceTextureOffset));
    spaceColor.rgb *= spaceBrightness + 0.5;
    finalColor = lerp(finalColor, spaceColor, InverseLerp(-0.05, 0.04, signedDistanceFromPortalEdge)) * edgeFade;
    
    // Apply forward light ray intensity effects to the portal.
    // This works by shoving the portal circle forward a little bit and making it so that areas that don't intersect both circles get lit up.
    float height1 = tex2D(lightSpikesEdgeTexture, frac(coords.y * 3));
    float height2 = tex2D(lightSpikesEdgeTexture, frac((coords.y + 0.33747) * 5));
    float spike = lerp(height1, height2, sin(globalTime * 23.2) * 0.5 + 0.5) * pow(circleStretchInterpolant, edgeFadeInSharpness);
    float distanceFromRingEdgeSoft = InverseLerp(0.25, 0.36, abs(coords.y - 0.5));
    float distanceFromAheadShape = distance(coords + uvOffset + float2((-0.09 + distanceFromRingEdgeSoft * 0.06) * aheadCircleMoveBackFactor, 0), 0.5);
    float distanceFromRingEdgeHard = InverseLerp(0.36, 0.33, abs(coords.y - 0.5));
    float forwardLightIntensity = InverseLerp(0.44, 0.3, distanceFromAheadShape * aheadCircleZoomFactor) * InverseLerp(0.2, 0.3, distanceFromAheadShape) * distanceFromRingEdgeHard;
    if (coords.x < 0.5)
        forwardLightIntensity *= InverseLerp(-0.07, -0.01, signedDistanceFromPortalEdge);
    
    return finalColor + forwardLightIntensity * spike;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}