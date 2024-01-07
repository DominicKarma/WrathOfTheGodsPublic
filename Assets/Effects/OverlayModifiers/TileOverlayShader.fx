sampler baseTexture : register(s0);
sampler tileContentsRenderTarget : register(s1);
sampler blackContentsRenderTarget : register(s2);

float2 zoom;
float2 tileOverlayOffset;
float2 inversionZoom;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, (coords - 0.5) * inversionZoom + 0.5);
    
    // Check if the pixel is covered by the dreaded DrawBlack or tile target. If it isn't, don't draw it.
    if (tex2D(tileContentsRenderTarget, (coords + tileOverlayOffset) / zoom).a <= 0 && tex2D(blackContentsRenderTarget, (coords + tileOverlayOffset) / zoom).a <= 0)
        return 0;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}