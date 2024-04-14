sampler staticTexture : register(s0);

float globalTime;
float2 playerUV;
float2 textureSize;

float2 AspectRatioAdjust(float2 coords)
{
    return (coords - 0.5) * float2(1, textureSize.y / textureSize.x) + 0.5;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculaate noise static.
    float noise = frac(tex2D(staticTexture, frac(coords * 16)).r + globalTime * 0.16);
    
    // Use the static for a nother static sampling, resulting in a near completely random visual.
    noise = tex2D(staticTexture, frac(noise * 2));
    
    // Calculate how brightness the static is, based on how far the pixel is from the player.
    float distanceFromPlayer = distance(AspectRatioAdjust(playerUV), AspectRatioAdjust(coords));
    float staticBrightness = 0.075 + smoothstep(0.035, 0.013, distanceFromPlayer) * 0.5;
    
    // Add the static to a black background.
    float4 color = float4(0, 0, 0, 1) + pow(noise, 1.5) * staticBrightness;
    return color;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}