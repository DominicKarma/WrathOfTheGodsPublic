sampler baseTexture : register(s0);
sampler genralNoiseMap : register(s1);
sampler normalMap : register(s2);
sampler warpSamplingOffsetMap : register(s3);

float globalTime;
float3 colorShift;
float3 lightDirection;
float2 normalMapZoom;
float normalMapCrispness;

// Refer to the following links for an explanation as to how this function works.
// http://dev.thi.ng/gradients/
// https://iquilezles.org/articles/palettes/
float3 Palette(float t, float3 a, float3 b, float3 c, float3 d)
{
    return a + b * sin(6.28318 * (c * t + d) + 1.5707);
}

float TriangleWave(float x)
{
    if (x % 2 < 1)
        return x % 2;
    return -(x % 2) + 2;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the base color relative to the tinting sample color and the base texture's color at the given pixel.
    float4 color = tex2D(baseTexture, coords) * sampleColor;
    
    float distanceFromEdge = length(coords - float2(1, 0.5));
    
    // Calculate values from the warp noise that will be applied to the palette sampling position.
    // They are designed in such a way that the offsets give the texture a viscous feel, which is very important for the overall
    // psychedelic effect of these wings.
    float2 warpNoiseOffset = tex2D(warpSamplingOffsetMap, coords * 1.3 + float2(globalTime * 0.2, 0)).rg;
    float psychedelicInterpolant = tex2D(genralNoiseMap, coords * 0.9 + warpNoiseOffset * 0.023).r * 1.45;
    float brightnessInterpolant = tex2D(genralNoiseMap, coords * 2.5 - warpNoiseOffset * 0.055).r;
    
    // Calculate the base psychedelic color from the warp noise.
    float3 psychedelicColor = Palette(psychedelicInterpolant, colorShift, float3(0.5, 0.5, 0.2), float3(1, 1, 1), float3(0, 0.333, 0.667)) * 0.8;
    psychedelicColor += pow(brightnessInterpolant, 3.5) * 2.4;
    
    float4 psychedelicColor4 = float4(psychedelicColor, 1) * color.a;
    
    // Calculate ring-based brightness values.
    float ringBrightness = clamp(0.2 / TriangleWave(globalTime * 0.6 - distanceFromEdge * 2), 0, 2.7) + 1;
    
    // Begin calculating the final result.
    float4 result = lerp(color, psychedelicColor4, color.r * 0.8) * ringBrightness;
    
    // Apply the normal map to the result to apply texturing.
    float3 normal = normalize(tex2D(normalMap, coords * normalMapZoom).xyz * 2 - 1);
    float brightness = pow(saturate(dot(lightDirection, normal)), normalMapCrispness);
    result.rgb *= brightness;
    
    return result;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}