sampler baseTexture : register(s0);

float globalTime;
float freezeInterpolant;

float3 HueShift(float3 color)
{
    const float3 kRGBToYPrime = float3(0.299, 0.587, 0.114);
    const float3 kRGBToI = float3(0.596, -0.275, -0.321);
    const float3 kRGBToQ = float3(0.212, -0.523, 0.311);

    const float3 kYIQToR = float3(1.0, 0.956, 0.621);
    const float3 kYIQToG = float3(1.0, -0.272, -0.647);
    const float3 kYIQToB = float3(1.0, -1.107, 1.704);

    float YPrime = dot(color, kRGBToYPrime);
    float I = dot(color, kRGBToI);
    float Q = dot(color, kRGBToQ);
    float hue = atan2(Q, I);
    float chroma = sqrt(I * I + Q * Q);

    hue = lerp(hue, 2.77, 0.9);

    Q = chroma * sin(hue);
    I = chroma * cos(hue);

    float3 yIQ = float3(YPrime, I, Q);

    return float3(dot(yIQ, kYIQToR), dot(yIQ, kYIQToG), dot(yIQ, kYIQToB));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = tex2D(baseTexture, coords);
    float3 blue = HueShift(result.rgb);
    result = float4(lerp(result.rgb, blue, freezeInterpolant * sqrt(1 - result.g)), 1) * result.a;
    result = lerp(result, result.a, freezeInterpolant * 0.33);
    
    return result * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}