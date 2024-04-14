sampler baseTexture : register(s0);
sampler teethTexture : register(s1);
sampler shapeNoiseTexture : register(s2);
sampler windNoiseTexture : register(s3);
sampler windBrightnessNoiseTexture : register(s4);
sampler staticNoiseTexture : register(s5);

float globalTime;
float scale;
float mouthBackgroundWindSpeed;
float distortionIntensity;
float distortionScrollSpeed;
float distanceCutoffThreshold;
float4 windColor;
float4 mouthBackgroundColor;
float4 edgeColor;

float ShapeDistanceFunction(float2 coords)
{
    // This is what this ends up looking like as a shape:
    // https://cdn.discordapp.com/attachments/1129281417663238178/1194788281251016714/screenshot.png
    float interpolant = saturate(coords.x);
    float2 shapePoint = float2(interpolant, sin(interpolant * 3.141) * 0.23);
    if (coords.y < 0.5)
        shapePoint.y = 1 - shapePoint.y;
    
    // Take the distance from the base coords to the shape line.
    return distance(coords, shapePoint);
}

float4 CalculateWindColor(float2 coords)
{
    // Calculate a rapidly changing static noise value. This will be used to define variance in the base noise background.
    float staticNoise = tex2D(staticNoiseTexture, coords * 20 + globalTime * float2(20, 89)) * 0.05;
    
    // Calculate the noise background.
    float4 background = mouthBackgroundColor + staticNoise;
    
    // Sample noise values for the wind.
    float windIntensity = 0.09 / pow(tex2D(windNoiseTexture, coords * float2(0.4, 2) + float2(globalTime * mouthBackgroundWindSpeed, 0)), 0.8);
    
    // Make the wind cut off at edge values, ensuring sharp boundaries.
    windIntensity *= smoothstep(0.5, 0.7, windIntensity);
    
    // Apply a fast moving darkening effect to the wind, making it look like there's a dark gale on top of it.
    windIntensity *= tex2D(windBrightnessNoiseTexture, coords * float2(0.1, 4) - globalTime * mouthBackgroundWindSpeed * 2.3);
    
    // Combine the background and wind.
    return background + windColor * windIntensity;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the noise influence power for the current pixel. Pixels more distant are affected to a weaker degree.
    float distortionInfluence = saturate(1.414 - distance(coords, 0.5) * 3);
    
    // Calculate the base shape noise coordinates. These are squished so that the shape distortions are generally biased towards retaining the pseudo-ellipse shape of the
    // mouth, rather than introducing strangely uniform blotches that pop in and out of existence.
    float2 baseShapeCoords = coords * float2(1, 4);
    
    // Calculate two shape noise values based on the aforementioned coords and combine them to calculate the distance distortion.
    float shapeNoise1 = tex2D(shapeNoiseTexture, baseShapeCoords + float2(globalTime * distortionScrollSpeed * 3.3, 0)).r;
    float shapeNoise2 = tex2D(shapeNoiseTexture, baseShapeCoords * 3 + float2(globalTime * distortionScrollSpeed, 0)).r * 0.15;
    float distanceDistortion = (saturate(shapeNoise1 - shapeNoise2) - 0.3) * distortionInfluence * -distortionIntensity;

    // Calculate the distortion from the center, taking into account distortion and scale.
    // As the scale decreases, the mouth rift's cutoff becomes more strict, resulting in the appearance that the effect is collapsing in itself naturally, rather than being universally downscaled.
    float distanceFromCenter = ShapeDistanceFunction(coords);
    float distortedDistanceFromCenter = (distanceFromCenter + distanceDistortion) / scale;
    
    // Once the distorted distance exceeds a designated cutoff threshold, the pixel is erased.
    float eraseCutoff = step(distortedDistanceFromCenter, distanceCutoffThreshold);
    
    // Calculate the color of the teeth inside of the mouth.
    float2 teethCoords = (coords - 0.5) * float2(1.6, 2.7) + 0.5;
    teethCoords.y = lerp(teethCoords.y, 0.5, 0.3);
    float4 teethColor1 = tex2D(teethTexture, (teethCoords - 0.5) * float2(0.67, 1.1) + 0.5 + float2(0.15, -0.05)) * float4(0.23, 0.23, 0.23, 1);
    float4 teethColor2 = tex2D(teethTexture, 0.984 - teethCoords) * float4(0.6, 0.6, 0.6, 1);
    
    float4 result = tex2D(baseTexture, coords) * float4(0, 0, 0, 1) + max(teethColor1, teethColor2);
    
    // Apply wind effects to pixels that are black.
    result += CalculateWindColor(coords) * (result.r <= 0.001);
    
    // Calculate and apply an edge color effect, where pixels that are very close to being cut off instead gain a border.
    result += edgeColor * smoothstep(distanceCutoffThreshold - 0.02, distanceCutoffThreshold, distortedDistanceFromCenter);
    
    return result * eraseCutoff;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}