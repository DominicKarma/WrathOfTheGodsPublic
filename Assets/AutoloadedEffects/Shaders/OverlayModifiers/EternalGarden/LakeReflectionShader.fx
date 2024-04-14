sampler baseTexture : register(s0);
sampler reflectionTexture : register(s1);
sampler colorNoiseTexture : register(s2);

bool gaudyBullshitMode;
float reflectionOpacityFactor;
float globalTime;
float reflectionXCoordInterpolationStart;
float reflectionXCoordInterpolationEnd;
float2 reflectionParallaxOffset;
float2 reflectionZoom;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Firstly calculate and apply accent noise to the base pixel color, biasing between greens and mild violets.
    // This helps give some additional texturing to the lake itself.
    float accentNoise = (tex2D(colorNoiseTexture, coords * float2(0.5, 2) + float2(globalTime * 0.02, 0)).r - 0.5) * 0.07;
    float4 color = tex2D(baseTexture, coords) * 1.1;
    color += float4(-accentNoise * 0.6, accentNoise, 0.18, 0) * color.a;
    
    // Store the height for the given pixel for ease of calculations.
    float height = 1 - coords.y;
    
    // Without this a mysterious line appears for some reason at the top of the lake texture.
    if (coords.y <= 0.2 || coords.y >= 0.97)
        return 0;
    
    // Calculate the base for the reflection coordinates.
    // This awkwardly interpolation is necessary because background drawcode is funny and draws the same lake texture 5 times to create the
    // illusion of infinite scrolling.
    float2 reflectionCoords = coords;
    reflectionCoords.x = lerp(reflectionXCoordInterpolationStart, reflectionXCoordInterpolationEnd, reflectionCoords.x);
    
    // Slightly bias the Y reflection position towards 0.5.
    reflectionCoords.y = (reflectionCoords.y - 0.5) * 0.8 + 0.5;
    
    // Incorporate parallax.
    reflectionCoords += reflectionParallaxOffset;
    
    // Add liquid distortion effects via a simple positional sine curve.
    // This could probably be better by applying Snell's Law or something similar but that seems like it'd just degrade performance for a tiny detail.
    reflectionCoords.x += sin(globalTime * 4 + coords.y * 560) * 0.00036;
    
    // Shift the reflection coords upward a bit and then stretch them horizontally, to help make the projection look more realistic and to ensure that the
    // aurora is noticeable on the result.
    reflectionCoords.y -= 0.25;
    
    // Vertically stretch coordinates based on how far up they are.
    reflectionCoords.y *= 1 + height * 2;
    
    // Flip the reflection coords so that the strongest lighting is nearest to the camera.
    reflectionCoords.y = 0.69 - reflectionCoords.y;
    
    // Apply the universal zoom.
    reflectionCoords = (reflectionCoords - 0.5) * reflectionZoom + 0.5;
    
    // Calculate the base reflection color. Naturally, if the base color is invisible (aka there's not water), no reflection can be displayed.
    float4 reflectionColor = tex2D(reflectionTexture, reflectionCoords) * any(color);
    
    // Calculate the greyscale color and interpolate towards it to ensure that stars lose their distinctive colorations against the background.
    // This effect is deliberately weakened near the bottom of the effect so that the auroras aren't just a strange pale color.
    float greyscale = dot(reflectionColor.rgb, 0.333);
    reflectionColor.rgb = lerp(reflectionColor.rgb, greyscale, height * 0.5);
    reflectionColor = pow(reflectionColor, 3 - height * 2.3);
    
    // Combine colors.
    float opacity = (1 - pow(height, 0.4)) * reflectionOpacityFactor;
    return color * sampleColor * (1 + gaudyBullshitMode * 0.4) + reflectionColor * opacity * (gaudyBullshitMode * 1.67 + 1.48);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}