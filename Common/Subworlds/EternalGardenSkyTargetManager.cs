using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Subworlds
{
    public class EternalGardenSkyTargetManager : ModSystem
    {
        public static bool DarkStardustBackgroundExists => NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.4f;

        public static bool AuroraExists => NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.4f;

        public static ManagedRenderTarget SkyTarget
        {
            get;
            private set;
        }

        public static Asset<Texture2D> SpecialStarTexture
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            // Load the star texture.
            if (Main.netMode != NetmodeID.Server)
                SpecialStarTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/VerticalLightStreak");

            // Prepare target creation and draw processes.
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareSkyTarget;
            Main.QueueMainThreadAction(() =>
            {
                SkyTarget = new(true, (_, _2) => new(Main.instance.GraphicsDevice, 2560, 1440));
            });
        }

        private void PrepareSkyTarget()
        {
            // Do nothing if not in the garden or if Nameless is present.
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame || NamelessDeitySky.KaleidoscopeInterpolant > 0f || NamelessDeitySky.HeavenlyBackgroundIntensity > 0f)
                return;

            // Move to the sky render target.
            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(SkyTarget);
            gd.Clear(Color.Transparent);

            // Draw the background to the target.
            DrawBackground();

            // Return to the backbuffer.
            gd.SetRenderTarget(null);
        }

        public static void DrawBackground()
        {
            // Don't draw anything if the heavenly background is at full intensity.
            if (NamelessDeitySky.HeavenlyBackgroundIntensity >= 1f)
                return;

            if (Main.netMode == NetmodeID.Server)
                return;

            // Don't draw stars if the camera is underground.
            if (Main.screenPosition.Y >= Main.worldSurface * 16.0 + 16.0)
                return;

            // Don't drwa stars if the background is too bright.
            Color colorOfTheSkies = Main.ColorOfTheSkies;
            if (255f * (1f - Main.cloudAlpha * Main.atmo) - colorOfTheSkies.R - 25f <= 0f)
                return;

            Texture2D beautifulStarTexture = SpecialStarTexture.Value;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            int totalLayers = (int)Lerp(2f, 6f, NoxusBossConfig.Instance.VisualOverlayIntensity);
            for (int i = 0; i < Main.numStars * totalLayers; i++)
            {
                int layerIndex = i / Main.numStars;
                Star star = Main.star[i % Main.numStars];

                // Draw layers behind everything.
                if (i % Main.numStars == 0 && DarkStardustBackgroundExists)
                    DrawStardustLayer(layerIndex);

                // Don't attempt to draw null or hidden stars.
                if (star is null || star.hidden)
                    continue;

                // Calculate draw values for the given star.
                Texture2D starTexture = TextureAssets.Star[star.type].Value;
                Vector2 starDrawPosition = new Vector2(star.position.X / 1920f, star.position.Y / 1200f) * Main.ScreenSize.ToVector2();
                starDrawPosition.Y = Lerp(starDrawPosition.Y, -250f, 0.45f);

                // Clump stars a bit.
                float centerBiasInterpolant = 0.56f - layerIndex * 0.1f - i * 0.14587f % 0.3f;
                starDrawPosition = Vector2.SmoothStep(starDrawPosition, new Vector2(1920f, 600f) * 0.5f, centerBiasInterpolant);
                starDrawPosition += (i * 0.367463f).ToRotationVector2() * 45f;

                // Draw stars.
                DrawStar(star, starDrawPosition, layerIndex, starTexture, beautifulStarTexture);
            }

            // Draw special constellations.
            DrawConstellations();

            // Draw the aurora.
            if (AuroraExists)
                DrawAurora();

            // Draw Nameless' eye if it's present.
            if (NamelessDeitySky.SkyEyeScale >= 0.03f && NamelessDeitySky.SkyEyeOpacity > 0f)
            {
                Vector2 eyeDrawPosition = new(960f * Clamp(Main.screenWidth, 0f, 1920f) / 1920f, Main.moonModY - 60f);
                NamelessDeitySky.ReplaceMoonWithNamelessDeityEye(eyeDrawPosition + Vector2.UnitY * Main.screenHeight / 1377f * 190f, Matrix.Identity);
            }

            Main.spriteBatch.End();
        }

        public static void DrawStar(Star star, Vector2 starDrawPosition, int layerIndex, Texture2D starTexture, Texture2D beautifulStarTexture)
        {
            Vector2 directionAwayFromCenter = (starDrawPosition - new Vector2(1920f, Main.screenHeight) * 0.5f).SafeNormalize(Vector2.UnitY);

            // Make stars recede away if necessary.
            starDrawPosition += directionAwayFromCenter * Pow(NamelessDeitySky.StarRecedeInterpolant, 3.7f) * 2000f;

            // Calculate the star's twinkle interpolant.
            float twinkleSeedOffset = (WorldGen.currentWorldSeed ?? "Fix").GetHashCode() % 40f;
            float twinkle = Cos01(Main.GlobalTimeWrappedHourly * -1.5f + star.position.X * 0.012168f - star.position.Y * 0.003f + twinkleSeedOffset) + 0.1f;

            // Calculate star colors.
            float colorInterpolant = (Pow(star.position.X * 0.0947346f % 1f, 4f) + star.position.Y * 0.0059474f % 0.6f) % 1f;
            Color starColor = MulticolorLerp(colorInterpolant, Color.DarkOrange, Color.Orange, Color.OrangeRed, Color.LightSkyBlue, Color.White) * twinkle;
            float greyscale = (starColor.R + starColor.G + starColor.B) / 765f;
            Color greyscaleColor = new(greyscale, greyscale, greyscale);
            starColor = Color.Lerp(starColor, greyscaleColor, 0.4f);
            starColor.A = 0;

            // Draw trails for falling stars.
            if (star.falling || NamelessDeitySky.StarRecedeInterpolant > 0f)
            {
                star.fadeIn = 0f;

                Vector2 offsetDirection = star.fallSpeed;
                double afterimageCount = NamelessDeitySky.StarRecedeInterpolant > 0f ? 25 : star.fallTime;
                if (afterimageCount > 30)
                    afterimageCount = 30;
                if (NamelessDeitySky.StarRecedeInterpolant > 0f)
                    offsetDirection = directionAwayFromCenter * NamelessDeitySky.StarRecedeInterpolant * 10f;

                Vector2 origin = starTexture.Size() * 0.5f;
                for (int j = 1; j < afterimageCount; j++)
                {
                    Vector2 afterimageOffset = -offsetDirection * j * 0.4f;
                    float afterimageScale = star.scale * (1f - j / 30f) * star.twinkle * Main.ForcedMinimumZoom;
                    Main.spriteBatch.Draw(starTexture, starDrawPosition + afterimageOffset, null, starColor * (1f - j / 30f), star.rotation, origin, afterimageScale, SpriteEffects.None, 0f);
                }
            }

            star.twinkleSpeed = (layerIndex * 0.001f) + 0.0002f + Cos(star.position.X * 0.01f) * 0.00015f;

            // Draw the star.
            float spokesOffset = Lerp(0.26f, 1.09f, Pow(star.position.X * 0.158585f % 1f, 2f) * twinkle);
            float scaleFactor = Clamp(0.04f - layerIndex * 0.006f, 0.018f, 1f);
            scaleFactor += Lerp(-0.015f, 0.046f, Pow((star.position.X * 0.01156f + star.position.Y * 0.007f) % 1f, 13f));

            // Apply very slight parallax.
            starDrawPosition -= Main.LocalPlayer.position * scaleFactor * new Vector2(1f, 2f) * 0.2f;

            // Calculate the star's rotation.
            float starRotation = Sin(Main.GlobalTimeWrappedHourly * 0.2f + star.position.Length() * 0.01f) * 0.1f;

            Color bloomColor = starColor;
            bloomColor.R = (byte)Utils.Clamp(bloomColor.R + (int)(twinkle * 54), 0, 255);
            TwinkleParticle.DrawTwinkle(beautifulStarTexture, starDrawPosition, 4, starRotation, bloomColor * 0.2f, starColor, Vector2.One * star.scale * star.twinkle * Main.ForcedMinimumZoom * scaleFactor, spokesOffset);
        }

        public static void DrawConstellations()
        {
            if (NamelessDeitySky.StarRecedeInterpolant > 0f)
                return;

            // An imperfect mod. Nay, a deeply faulted one. A bad one. A reflection of a bygone past.
            // And yet, a personal one. I must pay my respects.
            if (Main.LocalPlayer.name == "Dominic")
                DrawConstellation("Lukamoso", new Vector2(391f, -284f), 2.3f, 0.25f);

            // Rarely draw ogscule.
            if (WorldGen._genRandSeed % 1000 == 374)
                DrawConstellation("Ogscule", new Vector2(-337f, -488f), 0.7f, -0.31f, 340f, 0.06f);

            // Draw the player's head if they have defeated Nameless.
            if (WorldSaveSystem.HasDefeatedNamelessDeity)
                DrawConstellation("Player", -Vector2.UnitY * 300f, 1f, -0.31f, 240f, 0.054f);
        }

        public static void DrawConstellation(string constellationName, Vector2 drawOffset, float horizontalScale = 1f, float rotation = 0f, float upscaleFactor = 200f, float starVarianceFactor = 0.09f)
        {
            // Don't do anything if no valid constellation could not be found.
            if (!ShapeCurveManager.TryFind(constellationName, out ShapeCurve curve))
                return;

            Texture2D beautifulStarTexture = SpecialStarTexture.Value;
            curve = curve.Upscale(upscaleFactor);
            for (int i = 0; i < curve.ShapePoints.Count; i++)
            {
                Vector2 constellationOffset = curve.ShapePoints[i] * new Vector2(horizontalScale, 1f);
                Vector2 baseDrawPosition = new Vector2(1980f, 1440f) * 0.5f + drawOffset;
                Vector2 starDrawPosition = (baseDrawPosition + constellationOffset).RotatedBy(rotation, baseDrawPosition);
                Star star = new()
                {
                    twinkle = Lerp(0.3f, 0.87f, Cos01(i / (float)curve.ShapePoints.Count * Pi + Main.GlobalTimeWrappedHourly * 1.75f - baseDrawPosition.X)),
                    scale = (1f - NamelessDeitySky.StarRecedeInterpolant) * 2f,
                    position = starDrawPosition * starVarianceFactor
                };

                // Apply a tiny amount of randomness to the positions.
                starDrawPosition += (starDrawPosition.X * 50f).ToRotationVector2() * (starDrawPosition.X % 6f);

                DrawStar(star, starDrawPosition, i * 175 % 3, beautifulStarTexture, beautifulStarTexture);
            }
        }

        public static void DrawStardustLayer(int layerIndex)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Prepare the stardust shader.
            Vector2 screenArea = Main.ScreenSize.ToVector2();
            var stardustShader = ShaderManager.GetShader("GardenStardustBackground");
            stardustShader.TrySetParameter("zoom", 0.6f + layerIndex * 0.54f);
            stardustShader.TrySetParameter("brightColor", new Color(71 + layerIndex * 12, 55, 150 + layerIndex * 15));
            stardustShader.TrySetParameter("spaceColor", new Color(10, 7, 15));
            stardustShader.TrySetParameter("parallaxOffset", Main.LocalPlayer.Center / screenArea * -0.02f);
            stardustShader.Apply();

            // Draw the layer. Each layer is exponentially weaker than the last.
            float layerOpacity = Pow(0.9f, layerIndex) * 0.24f;
            Color layerColor = Color.White * layerOpacity * (1f - NamelessDeitySky.StarRecedeInterpolant);
            Main.spriteBatch.Draw(SmudgeNoise, screenArea * 0.5f, null, layerColor, 0f, SmudgeNoise.Size() * 0.5f, screenArea / SmudgeNoise.Size(), 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        }

        public static void DrawAurora()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Prepare the aurora shader.
            float verticalSquish = 0.25f;
            Vector2 screenArea = Main.ScreenSize.ToVector2();
            var auroraShader = ShaderManager.GetShader("AuroraShader");
            auroraShader.TrySetParameter("verticalSquish", verticalSquish);
            auroraShader.TrySetParameter("scrollSpeedFactor", 0.05f);
            auroraShader.TrySetParameter("accentApplicationStrength", 2.81f);
            auroraShader.TrySetParameter("parallaxOffset", Main.LocalPlayer.Center / screenArea * 0.6f);
            auroraShader.TrySetParameter("bottomAuroraColor", new Vector3(0.32f, 2.4f, 1.08f));
            auroraShader.TrySetParameter("topAuroraColor", new Vector3(0.8f, 0.96f, 1.15f));
            auroraShader.TrySetParameter("auroraColorAccent", new Vector3(0.16f, -0.4f, 0.21f));
            auroraShader.SetTexture(TurbulentNoise, 1, SamplerState.AnisotropicWrap);
            auroraShader.Apply();

            // Draw the texture. The shader will use it to draw the actual aurora.
            Vector2 auroraDrawPosition = screenArea * new Vector2(0.5f, 0f);
            Main.spriteBatch.Draw(SwirlNoise, auroraDrawPosition, null, Color.White * (1f - NamelessDeitySky.StarRecedeInterpolant) * 0.84f, 0f, SmudgeNoise.Size() * new Vector2(0.5f, 0f), screenArea / SwirlNoise.Size() * new Vector2(1f, verticalSquish), 0, 0f);
        }
    }
}
