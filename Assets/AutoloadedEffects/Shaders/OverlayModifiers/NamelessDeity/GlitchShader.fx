sampler baseTexture : register(s0);
sampler additiveStaticNoise : register(s1);

float globalTime;
float glitchInterpolant;
float2 coordinateZoomFactor;

float Random(float2 coords)
{
    return abs(frac(sin(dot(coords, float2(17.8342, 74.8819))) * 53648));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate a noise value that very, very rapidly descends downward.
    // So fast that it gives the appearance of TV static. This as an overall effect on the entire texture.
    float noise = Random(coords * coordinateZoomFactor * 0.3 + float2(0, frac(globalTime * -97.3)));
        
    // Calculate a base noise value that makes a bright line scroll down the screen. This is achieved by using a strongly squished sinusoid that scrolls downward and
    // taking its resulting value as a 0-1 interpolant.
    float brightLineInterpolant = pow(sin(globalTime * 7.4 - coords.y * 5.4) * 0.5 + 0.5, 4.96);
    
    // After the initial bright line value is calculated, incorporate the X axis to weaker extent. This allows the line to have bends and look less unnaturally simple.
    brightLineInterpolant += pow(sin(globalTime * 1.9 - coords.x * 2.4) * 0.5 + 0.5, 2.51) * 0.55;
    
    // Lastly, apply the noise from above, and clamp the entire interpolant between 0-1 before passing it in as a color multiplier.
    brightLineInterpolant += noise * 0.17;
    
    float4 baseColor = tex2D(baseTexture, coords) * sampleColor;
    float4 glitchedColor = float4(baseColor.rgb, 1) * lerp(1, 3.5, pow(saturate(brightLineInterpolant), 2)) + tex2D(additiveStaticNoise, coords).r * 0.4;
    return lerp(baseColor, glitchedColor, glitchInterpolant);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
