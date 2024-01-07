using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using ReLogic.Content;
using ReLogic.Peripherals.RGB;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers.NoxusSky;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class NoxusKeyboardShader : ChromaShader
    {
        public readonly Color BrightBackgroundColor;

        public readonly Color DarkBackgroundColor;

        public readonly Color FogColor;

        public readonly float[,] EyeBrightnessIntensity;

        public static float KeyboardBrightnessIntensity
        {
            get;
            set;
        }

        public static float EyeBrightness
        {
            get;
            set;
        }

        public static readonly ChromaCondition IsActive = new KeyboardShaderLoader.SimpleCondition(player => CommonConditions.Boss.HighestTierBossOrEvent == ModContent.NPCType<EntropicGod>());

        public NoxusKeyboardShader(Color brightBackgroundColor, Color darkBackgroundColor, Color fogColor)
        {
            BrightBackgroundColor = brightBackgroundColor;
            DarkBackgroundColor = darkBackgroundColor;
            FogColor = fogColor;

            // Load brightness values from the eye texture.
            if (Main.netMode != NetmodeID.Server)
            {
                // Get colors from the texture.
                Texture2D eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Shaders/Keyboard/NoxusEye", AssetRequestMode.ImmediateLoad).Value;
                Color[] textureColors = new Color[eyeTexture.Width * eyeTexture.Height];
                eyeTexture.GetData(textureColors);

                // Initialize the brightness array.
                EyeBrightnessIntensity = new float[eyeTexture.Width, eyeTexture.Height];
                for (int i = 0; i < textureColors.Length; i++)
                {
                    int x = i % eyeTexture.Width;
                    int y = i / eyeTexture.Width;
                    Color c = textureColors[i];
                    EyeBrightnessIntensity[x, y] = c.R / 255f;
                }
            }
        }

        public override void Update(float elapsedTime)
        {
            KeyboardBrightnessIntensity = Clamp(KeyboardBrightnessIntensity * 0.96f - 0.005f, 0f, 1f);
        }

        private Color GetBaseBackgroundColor(int index)
        {
            float noise1 = NoiseHelper.GetDynamicNoise(index, Main.GlobalTimeWrappedHourly * 0.4f) * 0.5f;
            float noise2 = NoiseHelper.GetDynamicNoise(index * 2 + 113, Main.GlobalTimeWrappedHourly * 0.97f) * 0.5f;
            Color backgroundColor = Color.Lerp(BrightBackgroundColor, DarkBackgroundColor, (noise1 + noise2) * 0.67f + 0.27f);
            return Color.Lerp(backgroundColor, FogColor, FogIntensity * 0.85f);
        }

        [RgbProcessor(EffectDetailLevel.Low)]
        private void ProcessLowDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time)
        {
            for (int i = 0; i < fragment.Count; i++)
            {
                Color gridColor = GetBaseBackgroundColor(i);
                fragment.SetColor(i, gridColor.ToVector4());
            }
        }

        [RgbProcessor(EffectDetailLevel.High)]
        private void ProcessHighDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time)
        {
            for (int i = 0; i < fragment.Count; i++)
            {
                Point gridPosition = fragment.GetGridPositionOfIndex(i);
                Vector2 uvPositionOfIndex = fragment.GetCanvasPositionOfIndex(i) / fragment.CanvasSize;
                Vector4 gridColor = GetBaseBackgroundColor(i).ToVector4();

                // Apply flashes in accordance to proximity from the flash position, assuming it's currently active.
                float flashDissipation = Vector2.Distance(uvPositionOfIndex, FlashPosition) * 3f + 1f;
                Vector2 flashSamplePosition = uvPositionOfIndex * 0.3f + FlashNoiseOffset;
                float flash = NoiseHelper.GetStaticNoise(flashSamplePosition) * intensity * FlashIntensity / Pow(flashDissipation, 8.1f);
                gridColor = gridColor * (1f + flash) + Vector4.One * flash * 0.02f;

                // Apply flash effects.
                gridColor += Vector4.One * KeyboardBrightnessIntensity;

                // Draw the eye.
                if (EyeBrightness > 0f)
                {
                    float horizontalEyeOffset = Pow(Sin(time), 11f) * 6f;
                    float eyeBrightnessAtPixel = 0f;

                    int x = gridPosition.X + (int)horizontalEyeOffset - 4;
                    int y = gridPosition.Y + 2;
                    if (x >= 0 && y >= 0 && x < EyeBrightnessIntensity.GetLength(0) && y < EyeBrightnessIntensity.GetLength(1))
                        eyeBrightnessAtPixel = EyeBrightnessIntensity[x, y];
                    gridColor += new Vector4(0.54f, 0.32f, 1f, 1f) * EyeBrightness * eyeBrightnessAtPixel * 1.7f;
                }

                // I don't know if the underlying color engine will shit itself if it's given values outside of the natural 0-1 range, and I don't have a keyboard to test it myself, so
                // this clamp is done out of an abundance of caution.
                gridColor = Vector4.Clamp(gridColor, Vector4.Zero, Vector4.One);

                fragment.SetColor(i, gridColor);
            }
        }
    }
}
