using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using ReLogic.Peripherals.RGB;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class NamelessDeityKeyboardShader : ChromaShader
    {
        public readonly float[,] EyeBrightnessIntensity;

        public static float BrightnessIntensity
        {
            get;
            set;
        }

        public static float DarknessIntensity
        {
            get;
            set;
        }

        public static float EyeBrightness
        {
            get;
            set;
        }

        public static int RandomColorsTimer
        {
            get;
            set;
        }

        public static readonly ChromaCondition IsActive = new KeyboardShaderLoader.SimpleCondition(player => CommonConditions.Boss.HighestTierBossOrEvent == ModContent.NPCType<NamelessDeityBoss>());

        public NamelessDeityKeyboardShader()
        {
            // Load brightness values from the eye texture.
            if (Main.netMode != NetmodeID.Server)
            {
                // Get colors from the texture.
                Texture2D eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Shaders/Keyboard/NamelessDeityEye", AssetRequestMode.ImmediateLoad).Value;
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
            BrightnessIntensity = Clamp(BrightnessIntensity * 0.96f - 0.005f, 0f, 1f);

            if (!Main.gamePaused)
            {
                EyeBrightness = Clamp(EyeBrightness * 0.96f - 0.005f, 0f, 1f);
                DarknessIntensity *= 0.94f;
                if (RandomColorsTimer > 0)
                    RandomColorsTimer--;
            }
        }

        public static Vector3 Palette(float t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 colorFactor = TwoPi * (c * t + d);
            colorFactor.X = Cos(colorFactor.X);
            colorFactor.Y = Cos(colorFactor.Y);
            colorFactor.Z = Cos(colorFactor.Z);

            return a + b * colorFactor;
        }

        [RgbProcessor(EffectDetailLevel.Low, EffectDetailLevel.High)]
        private void ProcessLowDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time)
        {
            for (int i = 0; i < fragment.Count; i++)
            {
                // Calculate coordinates.
                Point gridPosition = fragment.GetGridPositionOfIndex(i);
                Vector2 canvasPositionOfIndex = fragment.GetCanvasPositionOfIndex(i);
                Vector2 uvPositionOfIndex = canvasPositionOfIndex / fragment.CanvasSize;

                // Use random colors if necessary.
                if (RandomColorsTimer >= 1)
                {
                    float hue = NoiseHelper.GetDynamicNoise(canvasPositionOfIndex * 2f, time * 0.72f);
                    fragment.SetColor(i, Main.hslToRgb(hue, 0.7f, 0.5f).ToVector4());
                    continue;
                }

                // Calculate the base vibrancy color, with much of the background being dark with a handful of glimmering stars.
                float verticalOffset = NoiseHelper.GetDynamicNoise(canvasPositionOfIndex, time * 0.2f);
                Vector4 gridColor = Color.Lerp(Color.Black, Color.Indigo, 0.3f).ToVector4();
                float vibrancyInterpolant = Sin((verticalOffset + canvasPositionOfIndex.X + canvasPositionOfIndex.Y) * TwoPi);
                if (vibrancyInterpolant > 0f)
                {
                    float colorInterpolant = (canvasPositionOfIndex.Y * 12f + time * 0.9f) % 1f;
                    Vector3 psychedelicColor = Palette(colorInterpolant, DivineWingDrawer.WingColorShift, new(0.5f, 0.5f, 0.2f), new(1f, 1f, 1f), new(0f, 0.333f, 0.667f)) * 0.8f;
                    gridColor = Vector4.Lerp(gridColor, new(psychedelicColor, 1f), Pow(vibrancyInterpolant, 3f - BrightnessIntensity * 2.1f + DarknessIntensity * 25f) * (1f - DarknessIntensity));
                }

                // Apply a color seam if that effect is currently in use.
                if (SeamScale >= 0.001f)
                {
                    float signedDistanceToLine = SignedDistanceToLine(uvPositionOfIndex, new Vector2(0.5f, 0.5f), Vector2.UnitX);
                    float distanceToLine = Abs(signedDistanceToLine);
                    float brightnessTaper = Remap(SeamScale, 4f, 50f, 3f, 0.9f);
                    gridColor = Vector4.Lerp(gridColor, Vector4.One, Clamp((1f - distanceToLine * brightnessTaper) * SeamScale * 0.33f, 0f, 1f) * (1f - HeavenlyBackgroundIntensity));
                }

                // Draw the eye.
                if (EyeBrightness > 0f && NamelessDeityBoss.Myself is not null && DarknessIntensity < 0.9f)
                {
                    float distanceFromNamelessDeity = Main.screenPosition.X + Main.screenWidth * 0.5f - (NamelessDeityBoss.Myself?.Center ?? Vector2.Zero).X;
                    float horizontalEyeOffset = Pow(Sin(time * 0.44f), 7f) * 4f + Clamp(distanceFromNamelessDeity * 0.004f, -7f, 7f);
                    float eyeBrightnessAtPixel = 0f;

                    int x = gridPosition.X + (int)horizontalEyeOffset - 6;
                    int y = gridPosition.Y + 2;
                    if (x >= 0 && y >= 0 && x < EyeBrightnessIntensity.GetLength(0) && y < EyeBrightnessIntensity.GetLength(1))
                        eyeBrightnessAtPixel = EyeBrightnessIntensity[x, y];
                    gridColor += Vector4.One * EyeBrightness * eyeBrightnessAtPixel * (1f - DarknessIntensity);
                }

                // Apply brightness effects.
                gridColor += Vector4.One * BrightnessIntensity;

                // I don't know if the underlying color engine will shit itself if it's given values outside of the natural 0-1 range, and I don't have a keyboard to test it myself, so
                // this clamp is done out of an abundance of caution.
                gridColor = Vector4.Clamp(gridColor, Vector4.Zero, Vector4.One);

                fragment.SetColor(i, gridColor);
            }
        }
    }
}
