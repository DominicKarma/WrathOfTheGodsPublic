float3 edgeColor;
float3 centerColor;

float edgeBlendLength;
float edgeBlendStrength;

float mainOpacity;
float centerOpacity;
float halfSpreadAngle;
float spreadOutPower;

bool inverted;

texture sampleTexture;
sampler2D Texture1Sampler = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

// I can't believe it has gotten down to this.
float realCos(float value)
{
    return sin(value + 1.57079);
}

// Gets the distance of a plot from a line with a specified origin and angle.
float distanceFromLine(float2 origin, float angle, float2 plot)
{
    return abs(realCos(angle) * (origin.y - plot.y) - sin(angle) * (origin.x - plot.x));
}

// Gets the distance of a plot from a line with a specified origin and angle, but crops the line so it only expands towards the angle.
float distanceFromLineCropped(float2 origin, float angle, float2 plot, float plotAngle)
{
    // If the angle between the line's angle and the plot's angle is less than 90� (aka , not BEHIND the half-line), return the distance from the line.
    if (abs(angle - plotAngle) < 1.5707)
        return distanceFromLine(origin, angle, plot);
    
    // If we are behind the line, just give the distance between the start point and the plot.
    else
        return length(origin - plot);
}


float4 main(float2 uv : TEXCOORD) : COLOR
{
    // Set the uv's Y position to be only contained within the upper half of the texture, to mirror it around a horizontal axis.
    if (uv.y > 0.5)
        uv.y = 1 - uv.y;
    
    float2 mappedUv = float2(uv.x - 0.5, abs(uv.y - 0.5));
    
    // Get the length of the doubled distance, so that it's zero at the center of the sprite and one at the very edge of the circle.
    float distanceFromCenter = length(mappedUv) * 2;
    
    // Crop the sprite into a circle.
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    // Grabs the angle (only as a positive angle, since it's a mirror image underneath.
    float angle = atan2(mappedUv.y, mappedUv.x);
    
    // Grabs the distance of the point from the edge line.
    float distanceFromLine = distanceFromLineCropped(0, halfSpreadAngle, mappedUv, angle);
    
    // Grab the colors to blend the edge lines with.
    float3 color = float3(0, 0, 0);
    float opacity = 0;
    
    // If we are inside the spread's radius.
    if (angle <= halfSpreadAngle)
    {
        color = lerp(centerColor, edgeColor, pow(angle / halfSpreadAngle, 9));
        float fadeOut = pow(inverted ? distanceFromCenter : (1 - distanceFromCenter), spreadOutPower);
        if (inverted)
            fadeOut = 1 - fadeOut;
        
        opacity = centerOpacity * fadeOut;
    }
    
    // If we are further from the line than the edge's blending length, just don't even do the blending.
    if (distanceFromLine > edgeBlendLength)
        return float4(color * opacity * mainOpacity, opacity * mainOpacity);
    
    // The higher this value is, the more we blend with the edge's opacity and color.
    float edgeBlendFactor = pow(1 - distanceFromLine / edgeBlendLength, edgeBlendStrength);
    
    color = lerp(color, edgeColor, edgeBlendFactor);
    opacity = lerp(opacity, pow(1 - distanceFromCenter, 3), edgeBlendFactor) * mainOpacity;
    
    // Apply alpha multiplication.
    color = color * opacity;
    return float4(color, opacity);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 main();
    }
}