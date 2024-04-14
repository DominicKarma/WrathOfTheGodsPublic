sampler baseTarget : register(s0);
sampler colorField : register(s1);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float density = tex2D(baseTarget, coords).r;
    float4 color = tex2D(colorField, coords) * coords.y;
    return saturate(density * color * smoothstep(1, 0.8, coords.y)) * smoothstep(0.5, 0.4, distance(coords.x, 0.5)) * smoothstep(0.1, 0.22, coords.y) * sampleColor * 1.1;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}