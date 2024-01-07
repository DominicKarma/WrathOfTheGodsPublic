using Microsoft.Xna.Framework;
using NoxusBoss.Common.Biomes;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using ReLogic.Peripherals.RGB;
using Terraria.GameContent.RGB;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class EternalGardenKeyboardShader : ChromaShader
    {
        public static readonly ChromaCondition IsActive = new KeyboardShaderLoader.SimpleCondition(player => player.InModBiome<EternalGardenBiome>() && NamelessDeityBoss.Myself is null);

        public override void Update(float elapsedTime)
        {

        }

        [RgbProcessor(EffectDetailLevel.Low)]
        private void ProcessLowDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time) =>
            ProcessHighDetail(device, fragment, quality, time);

        [RgbProcessor(EffectDetailLevel.High)]
        private void ProcessHighDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time)
        {
            for (int i = 0; i < fragment.Count; i++)
            {
                Point gridPosition = fragment.GetGridPositionOfIndex(i);
                Vector2 uvPositionOfIndex = fragment.GetCanvasPositionOfIndex(i) / fragment.CanvasSize;
                Vector4 gridColor = Vector4.Zero;

                // Calculate the space background color.
                float spaceInterpolant = Sin01(uvPositionOfIndex.X * 0.7f + uvPositionOfIndex.Y * 2.2f + time * 0.2f);
                Vector4 spaceColor = Color.Lerp(new(17, 12, 30), new(17, 16, 38), spaceInterpolant).ToVector4();
                gridColor += spaceColor * 0.6f;

                // Calculate star colors.
                float starTwinkleInterpolant = 1f + Sin(time * 2.3f + i) * 0.25f;
                float starColorInterpolant = Pow(NoiseHelper.GetStaticNoise(gridPosition.X / 2 + gridPosition.Y / 6), 2.72f);
                float starAnimationSpeed = Lerp(0.002f, 0.06f, NoiseHelper.GetStaticNoise(i + 156));
                starColorInterpolant = (starColorInterpolant + time * starAnimationSpeed) % 1f;
                starColorInterpolant = InverseLerp(0f, 0.3f, starColorInterpolant);

                Vector4 starColor = MulticolorLerp(starColorInterpolant, Color.Black, Color.White, Color.Yellow, Color.Orange, Color.DarkOrange, Color.Red, Color.Cyan, new(20, 0, 120), Color.Black).ToVector4() * starTwinkleInterpolant;
                if (starColorInterpolant >= 1f)
                    starColor = Color.Black.ToVector4();

                // Apply stars to the background.
                gridColor += starColor;

                fragment.SetColor(i, gridColor);
            }
        }
    }
}
