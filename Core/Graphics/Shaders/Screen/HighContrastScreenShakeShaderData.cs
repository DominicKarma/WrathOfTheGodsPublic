using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class HighContrastScreenShakeShaderData : ScreenShaderData
    {
        public const string ShaderKey = "NoxusBoss:HighContrast";

        public static float ContrastIntensity
        {
            get;
            set;
        }

        public HighContrastScreenShakeShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Make the contrast intensity taper off naturally, in case it's still active for some reason when it shouldn't be.
            ContrastIntensity = Clamp(ContrastIntensity - 0.08f, 0f, 10f);

            bool shouldBeActive = ContrastIntensity >= 0.01f && NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.01f;
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public static Matrix CalculateContrastMatrix(float contrast)
        {
            // The way matrices work is as a means of creating linear transformations, such as squishes, rotations, scaling effects, etc.
            // Strictly speaking, however, they act as a sort of encoding for functions. The exact specifics of how this works is a bit too dense
            // to stuff into a massive code comment, but 3blue1brown's linear algebra series does an excellent job of explaining how they work:
            // https://www.youtube.com/watch?v=fNk_zzaMoSs&list=PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab

            // For this matrix, the "axes" are the RGBA channels, in that order.
            // Given that the matrix is somewhat sparse, it can be easy to represent the output equations for each color one-by-one.
            // For the purpose of avoiding verbose expressions, I will represent "oneOffsetContrast" as "c", and "inverseForce" as "f":

            // R = c * R + f * A
            // G = c * G + f * A
            // B = c * B + f * A
            // For the purposes of the screen shaders, A is always 1, so it's possible to rewrite things explicitly like so:
            // R = c * R + (1 - c) * 0.5
            // G = c * G + (1 - c) * 0.5
            // B = c * B + (1 - c) * 0.5

            // These are all linear equations with slopes that become increasingly sharp the greater c is. At a certain point (which can be trivially computed from c) the output
            // will be zero, and everything above or below that will race towards a large absolute value. The result of this is that color channels that are already strong are emphasized to their maximum
            // extent while color channels that are weak vanish into nothing, effectively increasing the contrast by a significant margin.
            // The reason the contrast needs to be offset by 1 is because inputs from 0-1 have the inverse effect, making the resulting colors more homogenous by bringing them closer to a neutral grey.
            // This effect could be useful to note for other contexts, but for the intended purposes of this shader it's easier to correct for this.
            float oneOffsetContrast = contrast + 1f;
            float inverseForce = (1f - oneOffsetContrast) * 0.5f;
            return new(
                oneOffsetContrast, 0f, 0f, 0f,
                0f, oneOffsetContrast, 0f, 0f,
                0f, 0f, oneOffsetContrast, 0f,
                inverseForce, inverseForce, inverseForce, 1f);
        }

        public override void Apply()
        {
            float configIntensityInterpolant = ContrastIntensity * InverseLerp(0f, 0.45f, NoxusBossConfig.Instance.VisualOverlayIntensity);
            Shader.Parameters["contrastMatrix"].SetValue(CalculateContrastMatrix(configIntensityInterpolant));
            base.Apply();
        }
    }
}
