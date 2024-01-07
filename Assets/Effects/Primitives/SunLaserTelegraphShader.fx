sampler streakHighlightTexture : register(s1);
sampler noiseTexture : register(s2);

float globalTime;
float verticalCutoffStartThreshold;
float verticalCutoffEndThreshold;
float2 highlightScrollOffset;
float2 brightnessNoiseScrollOffset;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    // Calculate the horizontal distance of the pixel from the center line of the telegraph.
    // This is exaggerated a little bit so that the edge flames don't go as far out.
    float distanceFromCenterX = abs(coords.y - 0.5) * 1.5;
    
    // Calculate the streak highlight brightness. This uses squished coordinates to keep the resulting line streaks of a natural size.
    // This samples from the same texture twice for a bit of variety.
    float2 highlightCoords = (coords - 0.5) * float2(0.5, 0.6) + 0.5 + highlightScrollOffset;
    float highlightBrightness = tex2D(streakHighlightTexture, highlightCoords + float2(globalTime * -1.2, 0)) * 0.75 + tex2D(streakHighlightTexture, highlightCoords + float2(globalTime * -1.4, 0)) * 0.8;
    
    // Calculate the inner brightness interpolant offset. This uses noise to make the result a bit less mathematically perfect and more rough.
    float innerBrightnessInterpolantOffset = tex2D(noiseTexture, coords * 0.33 + float2(globalTime * -1.5, 0) + brightnessNoiseScrollOffset) * 0.2;
    
    // Use the brightness interpolant offset to calculate the inner brightness of the fire telegraph. This makes pixels near the horizontal center brighter, while pixels near the edge have to
    // rely more on the streak highlight for coloring.
    float innerBrightness = InverseLerp(0.26, 0.125, distanceFromCenterX + innerBrightnessInterpolantOffset);
    
    // Calculate the base color of the streak. It starts off with a hot orange before becoming a pale red/brown the further along the trail the pixel is.
    float3 baseColor = lerp(float3(1, 0.81, 0.25), float3(1, 0.3, 0), saturate(coords.x * 2));
    
    // Combine everything from above together, and make the colors dissipates at the far edges of the trail.
    float4 streak = (float4(baseColor, 1) * highlightBrightness + innerBrightness) * InverseLerp(0.5, 0.4, distanceFromCenterX);
    
    // Apply vertical cutoff visuals based on input parameters. These determine where the telegraph line starts and ends, and can be used to give an aesthetic of it being "blasted off" if desired.
    float verticalOpacityStartCutoff = InverseLerp(-0.2, 0, coords.x - distanceFromCenterX * 0.53 - verticalCutoffStartThreshold);
    float verticalOpacityEndCutoff = InverseLerp(0.2, 0, coords.x + distanceFromCenterX * 0.97 - verticalCutoffEndThreshold);
    streak *= verticalOpacityStartCutoff * verticalOpacityEndCutoff;
    
    // Lastly, calculate and use a sharp brightness value if a pixel is close to the start of the telegraph, so that it can burn with the same intensity as the star's corona.
    float coronaProximityBrightness = lerp(2.84, 1, InverseLerp(0, 0.25, coords.x));
    return streak * input.Color * coronaProximityBrightness;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
