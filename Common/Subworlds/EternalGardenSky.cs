using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.Waters;
using NoxusBoss.Core.Configuration;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Subworlds
{
    public class EternalGardenSky : CustomSky
    {
        private bool skyActive;

        internal static float opacity;

        public static Color BackgroundAmbientColor
        {
            get
            {
                // The RGB values of the background are specially chosen such that it gains a majority blue overtone by default (because the auroras and lake are majority blue), while
                // suppressing purples, since they are in the minority (No, the cosmic background effect doesn't count, that's not really emitting light).
                Color standardAmbientLightColor = new(12, 29, 48);

                // Naturally, as the stars recede everything should go towards a neutral dark. There's a small amount of ambient lighting present for gameplay visibility reasons, but that's pretty much it.
                Color darkAmbientLightColor = new(4, 4, 4);

                // Lastly, once Nameless' eye appears everything should receive a reddish orange tint over everything, since that's the color of the eye.
                Color eyeAmbientLightColor = new(26, 11, 9);

                // Interpolate the ambient colors based on the current situation.
                Color ambientLightColor = Color.Lerp(standardAmbientLightColor, darkAmbientLightColor, NamelessDeitySky.StarRecedeInterpolant);
                ambientLightColor = Color.Lerp(ambientLightColor, eyeAmbientLightColor, Pow(NamelessDeitySky.SkyEyeScale * NamelessDeitySky.SkyEyeOpacity, 1.5f));

                return ambientLightColor;
            }
        }

        public static Color LakeAmbientColor
        {
            get
            {
                // Same idea as BackgroundAmbientColor for the most part.
                Color standardAmbientLightColor = Color.SkyBlue;
                Color darkAmbientLightColor = new(34, 34, 34);
                Color eyeAmbientLightColor = new(233, 194, 201);

                // Interpolate the ambient colors based on the current situation.
                Color ambientLightColor = Color.Lerp(standardAmbientLightColor, darkAmbientLightColor, NamelessDeitySky.StarRecedeInterpolant);
                ambientLightColor = Color.Lerp(ambientLightColor, eyeAmbientLightColor, Pow(NamelessDeitySky.SkyEyeScale * NamelessDeitySky.SkyEyeOpacity, 1.5f));

                return ambientLightColor;
            }
        }

        public static Asset<Texture2D>[] BackgroundFrameTextures
        {
            get;
            private set;
        }

        public static Asset<Texture2D>[] LakeFrameTextures
        {
            get;
            private set;
        }

        public const int BackgroundAnimationFrames = 4;

        // The speed of the parallax. The closer the layer to the player the faster it will be.
        public const float ScreenParallaxSpeed = 0.2f;

        internal static void LoadTextures()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Load background textures.
            BackgroundFrameTextures = new Asset<Texture2D>[BackgroundAnimationFrames];
            LakeFrameTextures = new Asset<Texture2D>[BackgroundAnimationFrames];
            for (int i = 0; i < BackgroundAnimationFrames; i++)
            {
                BackgroundFrameTextures[i] = ModContent.Request<Texture2D>($"Terraria/Images/Background_{i + 251}");
                LakeFrameTextures[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/Backgrounds/GardenLake{i + 1}");
            }
        }

        public override void Deactivate(params object[] args)
        {
            skyActive = false;
        }

        public override void Reset()
        {
            skyActive = false;
        }

        public override bool IsActive()
        {
            return skyActive || opacity > 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            skyActive = true;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Main.gameMenu)
                return;

            // Draw the starry sky.
            if (minDepth < 0f && maxDepth > 0f)
            {
                Main.spriteBatch.Draw(EternalGardenSkyTargetManager.SkyTarget, Vector2.Zero, Color.White);
            }

            // Disable ambient sky objects like wyverns and eyes appearing in the background.
            if (skyActive)
            {
                SkyManager.Instance["Ambience"].Deactivate();
                SkyManager.Instance["Slime"].Opacity = -1f;
                SkyManager.Instance["Slime"].Deactivate();
            }

            int frameOffset = (int)(Main.GameUpdateCount / 10U) % BackgroundAnimationFrames;
            Texture2D backgroundTexture = BackgroundFrameTextures[frameOffset].Value;
            Texture2D waterTexture = LakeFrameTextures[frameOffset].Value;

            // Apply parallax effects.
            int x = 0;
            int y = (int)(Main.screenPosition.Y * ScreenParallaxSpeed * 0.5f);

            // Loop the background horizontally.
            for (int i = -2; i <= 2; i++)
            {
                // Draw the base background.
                Vector2 layerPosition = new(Main.screenWidth * 0.5f - x + waterTexture.Width * i, Main.screenHeight - y + ScreenParallaxSpeed * 100f);
                spriteBatch.Draw(backgroundTexture, layerPosition - backgroundTexture.Size() * 0.5f, null, BackgroundAmbientColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            // Draw the brightened water.
            if (minDepth < 0f && maxDepth > 0f)
            {
                // Prepare for shader drawing if enabled.
                bool drawWithShader = EternalGardenWater.FancyWaterEnabled;
                Matrix backgroundMatrix = GetCustomSkyBackgroundMatrix();
                if (drawWithShader)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, backgroundMatrix);
                }

                // Prepare the lake reflection shader.
                var reflectionShader = ShaderManager.GetShader("NoxusBoss.LakeReflectionShader");
                if (drawWithShader)
                {
                    float horizontalDrawOffset = (Main.screenWidth - EternalGardenSkyTargetManager.SkyTarget.Width) / (float)EternalGardenSkyTargetManager.SkyTarget.Width * 0.585f;

                    float horizontalStretchOffset = 0.5f * EternalGardenLakeTargetManager.LakeTarget.Width / EternalGardenSkyTargetManager.SkyTarget.Width;
                    bool gaudyBullshitMode = NoxusBossConfig.Instance.VisualOverlayIntensity >= 1f;
                    reflectionShader.TrySetParameter("gaudyBullshitMode", gaudyBullshitMode);
                    reflectionShader.TrySetParameter("reflectionOpacityFactor", NamelessDeitySky.StarRecedeInterpolant >= 1f ? 0.5f : Pow(1f - NamelessDeitySky.StarRecedeInterpolant, 12.5f));
                    reflectionShader.TrySetParameter("reflectionZoom", new Vector2(0.85f, 1f));
                    reflectionShader.TrySetParameter("reflectionParallaxOffset", Vector2.UnitX * horizontalDrawOffset);
                    reflectionShader.TrySetParameter("reflectionXCoordInterpolationStart", 0.5f - horizontalStretchOffset);
                    reflectionShader.TrySetParameter("reflectionXCoordInterpolationEnd", 0.5f + horizontalStretchOffset);
                    reflectionShader.SetTexture(EternalGardenSkyTargetManager.SkyTarget, 1);
                    reflectionShader.SetTexture(DendriticNoise, 2, SamplerState.AnisotropicWrap);
                    reflectionShader.Apply();
                }

                Texture2D lakeTarget = EternalGardenLakeTargetManager.LakeTarget;
                Vector2 lakeDrawPosition = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight) + Vector2.UnitY * (ScreenParallaxSpeed * 100f - y);
                spriteBatch.Draw(lakeTarget, lakeDrawPosition, null, LakeAmbientColor * opacity * 0.24f, 0f, lakeTarget.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

                // Return the sprite batch to its normal state.
                if (drawWithShader)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, backgroundMatrix);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame || Main.gameMenu)
                skyActive = false;

            if (skyActive && opacity < 1f)
                opacity += 0.02f;
            else if (!skyActive && opacity > 0f)
                opacity -= 0.02f;
        }

        public override float GetCloudAlpha() => 0f;
    }
}
