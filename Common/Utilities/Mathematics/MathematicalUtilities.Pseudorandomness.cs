using Microsoft.Xna.Framework;
using Terraria.GameContent.RGB;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        // When two periodic functions are summed, the resulting function is periodic if the ratio of the b/a is rational, given periodic functions f and g:
        // f(a * x) + g(b * x). However, if the ratio is irrational, then the result has no period.
        // This is desirable for somewhat random wavy fluctuations.
        // In this case, pi and e used, which are indeed irrational numbers.
        /// <summary>
        /// Calculates an aperiodic sine. This function only achieves this if <paramref name="a"/> and <paramref name="b"/> are irrational numbers.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <param name="a">The first irrational coefficient.</param>
        /// <param name="b">The second irrational coefficient.</param>
        public static float AperiodicSin(float x, float dx = 0f, float a = Pi, float b = MathHelper.E)
        {
            return (Sin(x * a + dx) + Sin(x * b + dx)) * 0.5f;
        }

        /// <summary>
        /// Applies 2D FBM, an iterative process commonly use with things like Perlin noise to give a natural, "crisp" aesthetic to noise, rather than a blobby one.
        /// <br></br>
        /// The greater the amount of octaves, the more pronounced this effect is, but the more performance intensive it is.
        /// </summary>
        /// <param name="x">The X position to sample from.</param>
        /// <param name="y">The Y position to sample from.</param>
        /// <param name="seed">The RNG seed for the underlying noise calculations.</param>
        /// <param name="octaves">The amount of octaves. The greater than is, the more crisp the results are.</param>
        /// <param name="gain">The exponential factor between each iteration. Iterations have an intensity of g^n, where g is the gain and n is the iteration number.</param>
        /// <param name="lacunarity">The degree of self-similarity of the noise.</param>
        public static float FractalBrownianMotion(float x, float y, int seed, int octaves, float gain = 0.5f, float lacunarity = 2f)
        {
            float result = 0f;
            float frequency = 1f;
            float amplitude = 0.5f;

            // Offset the noise a bit based on the seed.
            x += seed * 0.00489937f % 10f;

            for (int i = 0; i < octaves; i++)
            {
                // Calculate -1 to 1 ranged noise from the input value.
                float noise = NoiseHelper.GetStaticNoise(new Vector2(x, y) * frequency) * 2f - 1f;

                result += noise * amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }
            return result;
        }
    }
}
