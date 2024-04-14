sampler crackedNoiseTexture : register(s1); // IT'S TIME TO BECOME CRACKED!

float radialOffsetTime;
float globalTime;
float animationSpeed;
float vignettePower;
float vignetteBrightness;
float crackBrightness;
float2 aspectRatioCorrectionFactor;
float4 primaryColor;
float4 secondaryColor;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float2 AspectRatioCorrect(float2 coords)
{
    return (coords - 0.5) * aspectRatioCorrectionFactor + 0.5;
}

float2 CalculateRadiallyOffsetCoords(float2 coords)
{
    // Offset the radius based on time. This results in a cool "jet out" effect.
    float originalRadius = distance(coords, 0.5);
    float radius = fmod(originalRadius + radialOffsetTime, 3);
    
    // Calculate the angular offset based on time, radius, and horizontal position.
    // This serves as a useful way of making the crack tendrils sway around in an eerie way.
    float offsetAngle = cos(globalTime + originalRadius * (coords.x > 0.5 ? 1 : -1) * 30 + (coords.x + 2) * 40) * 0.047;
    
    // Calculate the angle with the offset angle accounted for, and convert the entire thing back to a position with the
    // angle -> direction equation.
    float angle = atan2(coords.y - 0.5, coords.x - 0.5) + offsetAngle;
    return float2(cos(angle), sin(angle)) * radius;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate an intermediate value that serves as a coordinate offset. This helps to offset the static, scrolling nature of the calculated colors below.
    float crackOffset = tex2D(crackedNoiseTexture, coords * 4.5 + float2(0, globalTime * -animationSpeed * 0.6)).r;

    // Calculate two colors based on the crack noise: a "background" and "tendril" crack. Both take into account the offset above for inputs along a diagonal.
    // The background cracks move around slowly in a single direction, with small offset details.
    // The tendril crack actively sways around while pulsing in towards the player, giving the impression of a disgusting eye.
    float backgroundCrackColor = tex2D(crackedNoiseTexture, coords * 4.7 + -crackOffset * 0.19 + float2(globalTime * -animationSpeed * 0.67, 0.16)).r;
    float tendrilCrackColor = tex2D(crackedNoiseTexture, CalculateRadiallyOffsetCoords(coords) * 2 + crackOffset * 0.05).r;
    
    // Combine the two colors together.
    float crackColor = 1.2 - (backgroundCrackColor * primaryColor * 0.6 + tendrilCrackColor * secondaryColor);
    
    // Calculate vignette variables.
    float distanceToCenter = distance(AspectRatioCorrect(coords), 0.5);
    float vignetteInterpolant = saturate(pow(distanceToCenter, vignettePower) * vignetteBrightness + crackColor * 0.2) * 0.8;
    float blacknessInterpolant = vignetteInterpolant + pow(crackBrightness, 2) * 0.9;
    
    // Calculate the base color. For the most part it is a pure red. This will be used to apply color to the crack color from above.
    float redInterpolant = saturate(pow(crackColor, 2)) + vignetteInterpolant * 0.12;
    float4 baseColor = float4(redInterpolant * 0.3, redInterpolant * 0.014, tendrilCrackColor * 0.02, 1);
    
    // Calculate a white accent. This gets stronger the further out the given pixel is, based on vignette calculations.
    // This helps to ensure that the base red colors are given some variety in their shading as they get further out from the center of the screen.
    float whiteAccent = crackColor * vignetteInterpolant * pow(crackBrightness, 5) * 0.13;
    
    // Combine everything together.
    return (float4(redInterpolant * 0.3, redInterpolant * 0.014, tendrilCrackColor * 0.02, 1) * blacknessInterpolant + whiteAccent) * crackBrightness;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}