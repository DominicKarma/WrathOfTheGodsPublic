sampler baseMaskStreakTexture : register(s1);
sampler lightningStreakTexture : register(s2);
sampler overlayStreakTexture : register(s3);
sampler baseNoiseTexture : register(s4);
sampler subtractiveNoiseTexture : register(s5);

float globalTime;
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

float4 ScreenBlend(float4 a, float4 b)
{
    return 1 - (1 - a) * (1 - b);
}

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float startingFade = InverseLerp(0.059, 0.017, coords.x);
    
    float widthCutoffInterpolant = 1 - pow(coords.x, 0.45);

    // Calculate the edge fade of the pixel. The closer it is to the left or right side of the laser, the more translucent it is.
    // The intensity of this effect depends on two major factors:
    // 1. It's significantly stronger at the start of the laser, so that the start feels more gradual and connected to the energy orb the laser is casted from.
    // 2. The position of the pixel relative to the length of the laser, along with time. This gives a bit of a sinusoidal "waving" motion that makes the laser feel less rectangular.
    float edgeFade = pow(QuadraticBump(coords.y), startingFade * 11 + sin(globalTime * 10 + coords.x * 16) * 1.1 + 2.3 + widthCutoffInterpolant * 20);
    
    // Calculate the laser's noise texture. This starts with a grainy scrolling texture that's screen blended with the laser's base color.
    // From there, its red and green components are sifted away based on another, more "blotchy" noise texture, to give color variance, in this case towards
    // blues and cyans. This effect is weak enough to leave plenty of purple, however.
    float4 noiseColor = ScreenBlend(tex2D(baseNoiseTexture, coords * float2(0.7, 0.2) + float2(globalTime * -2, 0)), color) * color.a;
    noiseColor.rg -= tex2D(subtractiveNoiseTexture, coords * float2(2, 1) + float2(globalTime * -1.4, 0)).r * 0.17;
    
    // Calculate the overlay color. This effect serves as a subtractive effect, leaving black streaks wherever it's activated.
    // The coordinates are calculated such that the overlay moves in a "swirl", as though it's spinning around a 3D effect.
    float2 overlayCoords = float2(coords.x * 3 - globalTime * 8, coords.y + coords.x * 3 - globalTime * 2.1);
    float overlay = tex2D(overlayStreakTexture, overlayCoords).r * (1 - startingFade) * 0;
    
    // Calculate lightning overlays. Combined, these make purple/magneta lightning overlays on the texture.
    float lightning1 = tex2D(lightningStreakTexture, coords * float2(2.67, 0.81) + float2(globalTime * -5.11, 0) + float2(-0.66, 0) * coords.x) * (1 - startingFade);
    float lightning2 = tex2D(lightningStreakTexture, float2(coords.x, 1 - coords.y) * float2(3, 0.93) + globalTime * float2(-3.99, 0) + float2(0.89, 0) * coords.x) * (1 - startingFade);
    float lightning = (lightning1 + lightning2) * 0;
    float4 result = edgeFade * noiseColor * 1.6;
    result.r *= lerp(1, 1.3 - lightning2 * 0.95, lightning);
    result.g *= lerp(1, 0.2, lightning);
    
    // Add a little bit more pink-ness the color based on the lightning intensity.
    result += color.a * lightning.r * 0.3;
    
    // Incorporate the subtractive overlay.
    result.rgb -= overlay * pow(color.a, 0.1) * edgeFade;
    
    return result * (1 - startingFade);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
