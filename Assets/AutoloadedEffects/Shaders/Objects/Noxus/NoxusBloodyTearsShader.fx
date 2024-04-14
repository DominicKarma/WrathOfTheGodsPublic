sampler baseTexture : register(s0);
sampler shapeTexture : register(s1);
sampler colorNoiseTexture : register(s2);

float globalTime;
float animationStartInterpolant;
float animationEndInterpolant;
float topCutoffThresholdLeft;
float topCutoffThresholdRight;
float playerWidth;
float2 playerTopLeft;
float2 playerTopRight;

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Apply cutoffs at the top of the blood stream.
    float topCutoffThreshold = 0.01 + tex2D(shapeTexture, coords * 0.2 + float2(0, globalTime * -0.1)) * 0.01;
    topCutoffThreshold += (1 - QuadraticBump(coords.x)) * 0.03;
    topCutoffThreshold += animationEndInterpolant;
    topCutoffThreshold += lerp(topCutoffThresholdLeft, topCutoffThresholdRight, coords.x);
    if (coords.y < topCutoffThreshold)
        return 0;
    
    // Apply cutoffs at the bottom of the blood stream.
    float bottomCutoffThreshold = 1 - tex2D(shapeTexture, coords * 0.18 + float2(0, globalTime * -0.078)) * 0.3;
    bottomCutoffThreshold -= 1 - animationStartInterpolant;
    if (coords.y >= bottomCutoffThreshold)
        return 0;
    
    // Apply cutoffs at the edges of the blood stream.
    float distanceFromEdge = distance(coords.x, 0.5);
    float edgeCutoffNoise = tex2D(shapeTexture, coords * 1.02 + float2(0, globalTime * -0.9));
    float edgeCutoffThreshold = lerp(0.39, 0.5, edgeCutoffNoise);
    if (distanceFromEdge >= edgeCutoffThreshold)
        return 0;
    
    // Calculate local opacity values. These will make colors that are close to being cut off disappear more gradually.
    float topOpacity = smoothstep(topCutoffThreshold, topCutoffThreshold + 0.016, coords.y);
    float bottomOpacity = smoothstep(bottomCutoffThreshold, bottomCutoffThreshold - 0.15, coords.y);
    float edgeOpacity = smoothstep(edgeCutoffThreshold, edgeCutoffThreshold - 0.12, distanceFromEdge);
    
    // Make blood not appear below the player.
    float horizontalDistanceFromPlayer = min(distance(coords.x, playerTopLeft.x), distance(coords.x, playerTopRight.x));
    if (horizontalDistanceFromPlayer <= playerWidth + edgeCutoffNoise * 0.01 && coords.y > playerTopLeft.y + 0.01 && playerTopLeft.y >= -0.01)
    {
        float distanceBelowPlayer = coords.y - playerTopLeft.y;
        bottomOpacity = smoothstep(0.1, 0, distanceBelowPlayer);
    }
    
    // Combine opacity values together.
    float opacity = edgeOpacity * bottomOpacity * topOpacity;
    opacity *= 1 - animationEndInterpolant;
    
    // Combine colors.
    sampleColor.a = saturate(sampleColor.a + coords.y * 0.9);
    float darkness = (1 - sampleColor.a) * 1.2;
    sampleColor.a = lerp(sampleColor.a, 1, 0.6);
    
    float4 color = tex2D(baseTexture, coords) * sampleColor * opacity - float4(darkness, darkness, darkness, 0);
    return color;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}