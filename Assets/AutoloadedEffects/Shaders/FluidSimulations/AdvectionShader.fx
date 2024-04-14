sampler baseTarget : register(s0);
sampler velocityField : register(s1);
sampler turbulenceTexture : register(s2);
sampler densityField : register(s3);

float globalTime;
float deltaTime;
float gravity;
float2 simulationSize;
float2 stepSize;
float2 force;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the turbulence angle based on a noise texture. This offsets the velocity a bit over time and makes the fluid a bit less boring
    float turbulenceAngle = tex2D(turbulenceTexture, coords * 0.75 + float2(0, globalTime * -0.01)) * 10;
    
    // Calculate the velocity.
    float2 velocity = tex2D(velocityField, coords).xy + float2(cos(turbulenceAngle), sin(turbulenceAngle)) * 0.1 + force * float2(1, coords.y);
    
    // Apply gravity.
    velocity.y += gravity;
    
    // Extrapolate into the future based on velocity.
    // This subtracts instead of adds because the update step is technically in reverse, determining where the current position should be based on the past, rather
    // than where the future position should be based on the current position.
    float2 backStep = coords - velocity * deltaTime;
    
    return tex2D(baseTarget, backStep);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}