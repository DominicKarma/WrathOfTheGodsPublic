texture baseTexture;
sampler2D baseTextureSampler = sampler_state
{
    texture = <baseTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float huePhaseShift;
float globalTime;
float2 baseTextureSize;
float3 eyeColor1;
float3 eyeColor2;
float4 drawAreaRectangle;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTextureSampler, coords);
    
    // Use the brightness, along with time, as a means to a hue offset interpolant. This allows colors to dynamically shift based on palette.
    float brightness = (color.r + color.g + color.b) / 3;
    float hueOffsetInterpolant = sin(globalTime * 4 + brightness * 9 + huePhaseShift) * 0.5 + 0.5;
    
    // Combine colors based on the aforementioned interpolant.
    color = float4(lerp(eyeColor1, eyeColor2, hueOffsetInterpolant), 1) * color.a;
    
    return color;

}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}