sampler baseTexture : register(s0);
sampler neuronTexture : register(s1);
sampler neuronMaskTexture : register(s2);
sampler bendNoiseTexture : register(s3);
sampler neuronShapeTexture : register(s4);

float globalTime;
float signalDetail;
float synapseShapePrecision;
float synapseFireSpeed;
float forwardScrollSpeed;
float spinSpeed;
float vignetteCurvatureFactor;
float vignettePower;
float aspectRatio;
float forwardBendFactor;

float2 neuronUVs[10];
float neuronIntensities[10];

float Voronoi(float2 x)
{    
    float v = tex2D(neuronTexture, x * 0.035);
    float bend = tex2D(bendNoiseTexture, x * 0.64);    
    return lerp(v, 0.6 - bend, v);
}

float Modulo(float dividend, float divisor)
{
    return dividend - floor(dividend / divisor) * divisor;
}

float2x2 RotationMatrix(float angle)
{
    float sine = sin(angle);
    float cosine = cos(angle);
    return float2x2(cosine, -sine, sine, cosine);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float intensity = 0;
    float2 directionedScrollSpeed = float2(-0.015, 0);
    
    // Calculate the vignette intensity based on distance from the center of the screen.
    float distanceFromCenter = distance((coords - 0.5) * float2(1, aspectRatio) + 0.5, 0.5);
    float vignetteFade = exp(distanceFromCenter * -vignettePower);
    
    // Loop through all neuron UVs, and make it so that the vignette fades in accordance to all of them.
    float neuronIntensity = 0;
    for (int i = 0; i < 10; i++)
        neuronIntensity += pow(saturate(0.016 / distance(coords, neuronUVs[i])), 2.45) * neuronIntensities[i];
    vignetteFade = lerp(vignetteFade, 1, smoothstep(0, 0.6, neuronIntensity));
    
    // Calculate proximity to the center of the scene.
    float centerFade = lerp(0.867, 1, smoothstep(0.04, 0.09, distanceFromCenter));
    
    // Warp coordinates.
    coords = lerp(coords, 0.5, distanceFromCenter * forwardBendFactor);
    
    i = 0;
    for (i = 0; i < 4; i++)
    {
        // Calculate the local index of the given layer. This normally would simply be stationary, but to give the illusion of moving forward or backward
        // it's relative to the current time, allowing layers to gradually move, giving the illusion that the player is travelling in 3D space in Noxus' mind.
        float index = Modulo(i - globalTime * forwardScrollSpeed, 4);
        
        // Decrease the amplitude and increase the frequency. Standard things for a FBM-like model.
        float frontLayerFade = smoothstep(0.7, 1, index);
        float amplitude = pow(0.62, index) * frontLayerFade * max(0, 0.7 + neuronIntensity * 1.4 - distance(centerFade, 1) * 1.6);
        float localFrequency = pow(2.18, index) * 7.9 + 6;
        
        // Calculate the zoomed coords for the given FBM layer.
        float2 zoomedCoords = mul(RotationMatrix(globalTime / (6 - index) * spinSpeed + distanceFromCenter * vignetteCurvatureFactor), coords - 0.5) * localFrequency + 0.5;
        zoomedCoords += index * 1.1;
        
        // Scroll based on the zoomed coords.
        float2 localScrollOffset = globalTime * directionedScrollSpeed / (index + 4);
        float2 scrolledCoords = zoomedCoords + localScrollOffset;
        
        // Use voronoi sampling and a separately scrolling mask texture to calculate the neuron texture.
        float synapseMask = tex2D(neuronMaskTexture, (scrolledCoords - 0.5) * 0.3 + 0.5 + localScrollOffset * 0.15);
        float synapseInterpolant = Voronoi(scrolledCoords) * synapseMask * 0.8;
        
        // Calculate the synapse fire colors.
        if (i >= 1)
        {
            // Calculate the signal interpolant. Signals weaken the farther out they are, based on the vignette interpolant.
            float signalInterpolant = Voronoi((zoomedCoords - 0.5) * 0.3 + 20.5 + globalTime * -synapseFireSpeed) + (1 - vignetteFade) * 0.09;
            
            // Ensure that signals only influence parts of the result that aren't bright already.
            float reinforcementInterpolant = smoothstep(1, 0.9, intensity);
            
            float shapeInterpolant = 1 - smoothstep(0, 1 / synapseShapePrecision, synapseInterpolant);;
            float detailInterpolant = 1 - smoothstep(0, signalDetail, signalInterpolant);
            intensity += reinforcementInterpolant * amplitude * pow(shapeInterpolant * (detailInterpolant + 0.5), 2);
        }
        
        // Harshly cut off the synapse interpolant such that only values between 0 and 0.24 are considered, with darker colors gaining a higher brightness.
        // This makes it so that only the dark edges between the voronoi texture are considered.
        synapseInterpolant = 1 - smoothstep(0, 0.24, synapseInterpolant);
        
        // Increase the intensity for this layer, feeding the synapse interpolant into a smooth noise function.
        intensity += synapseInterpolant * amplitude * 0.7;
    }
    
    float3 colorExponent = float3(3 + sin(intensity * 4 + globalTime * 0.46) * 1.5, 4, 1.1);
    
    // Apply vignette fading to the overall result.
    intensity *= vignetteFade;
    
    // Fade at the center of the scene, so that the player can see attacks there more clearly.
    intensity *= centerFade;
    
    // Bias results in favor of desired colors based on exponents and noise values.
    float4 result = float4(0, 0, 0, 1) + pow(float4(pow(intensity, colorExponent), 0), 3) * 2;
    
    // Add a slight cyan bias.
    float cyanBias = tex2D(bendNoiseTexture, coords * 0.6);
    result.g += saturate(result.b * cyanBias * 0.43 - result.r);
    
    return result * sampleColor * 1.56;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}