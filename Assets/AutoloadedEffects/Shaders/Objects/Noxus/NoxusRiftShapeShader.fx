sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler detachingNoiseTexture : register(s2);

float time;
float textureSwirlSpeed;
float textureSwirlStrength;
float collapseAnimationSpeed;
float radialDistortionExaggeration;
float vanishInterpolant;
float outerEdgePixelEraseSensitivity;
float2 worldPositionOffset;
float2 outerEdgeShapeNoiseZoom;

// This shader is only responsible for creating the shape of the portal. Colorations happen in a different shader.
// In this shader, the red component represents the presence of edges.
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Slightly bias portal texture coordinates towards the center proportional to how far the current pixel is from the center.
    // The further away it is, the more strongly it's interpolated back.
    float2 squishedCoords = lerp(coords, 0.5, distance(coords, 0.5) * -0.35);
    
    // Convert the squished coordinates from Cartesian to polar for the purpose of angle and distance manipulations.
    float2 polar = float2(distance(squishedCoords, 0.5), atan2(squishedCoords.y - 0.5, coords.x - 0.5));
    
    // Calculate the distance distortion based on a scrolling noise texture.
    float distanceDistortion = tex2D(noiseTexture, (coords + float2(0, time * 0.9)) * 0.0687) * 0.113 - 0.0267;
    
    // Calculate the distance to the absolute center at 0.5 from the base coordinates, taking into account the aforementioned distanceDistortion value.
    // This is used to create the dark, firey edge to the rift.
    float distanceToCenter = distance(coords, 0.5);
    float distortedDistanceToCenter = distanceToCenter + distanceDistortion * 0.4;
    
    // Calculate noise values for the edge of the portal.
    float eraseStepValue = 1 - outerEdgePixelEraseSensitivity;
    float2 noiseCoords = polar + worldPositionOffset;
    noiseCoords.x = sin(noiseCoords.x * 6.283 - time * 9) * 0.5 + 0.6;
    noiseCoords.y += distanceToCenter * -7;
    noiseCoords = float2(cos(noiseCoords.y), sin(noiseCoords.y)) * noiseCoords.x + 0.5;
    noiseCoords *= outerEdgeShapeNoiseZoom;
    float noise = tex2D(detachingNoiseTexture, noiseCoords * 1.1 + float2(0, time * 0.64)) * (1 - tex2D(detachingNoiseTexture, noiseCoords * 0.9 + float2(time * 0.74, 0)));
    noise += smoothstep(0.21, 0.44, distanceToCenter) * eraseStepValue * 2;
    
    // Calculate values pertaining to the shape of the portal.
    float innerRadius = (1 - vanishInterpolant) * 0.32;
    bool innerPartOfRift = distortedDistanceToCenter < innerRadius;
    bool outerEdgeIsSpared = step(noise, eraseStepValue);
    float isSolidShape = outerEdgeIsSpared || innerPartOfRift;
    
    // Calculate the inner edge interpolant. This appears right at the edge of the inner shape.
    // The outerEdgeIsSpared check ensures that parts that would otherwise be covered by the outer shape's distortions don't get awkwardly covered up.
    float innerEdgeInterpolant = smoothstep(innerRadius * 0.875, innerRadius, distortedDistanceToCenter) * !outerEdgeIsSpared;
    
    // Calculate the outer edge interpolant.
    float outerEdgeInterpolant = smoothstep(0.67, 1, noise / eraseStepValue);
    
    // Choose when edge interpolant should be considered.
    float edgeInterpolant = lerp(innerEdgeInterpolant, outerEdgeInterpolant, !innerPartOfRift);
    
    return lerp(float4(0, 0, 0, 1) * isSolidShape, float4(1, 0, 0, 1), edgeInterpolant * isSolidShape);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}