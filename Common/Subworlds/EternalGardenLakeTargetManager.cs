using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Subworlds
{
    // I cannot begin to tell you how much I absolutely hate the vanilla background drawing system.
    // This is the only way I could mitigate STUPID horizontal offsets in the reflections across resolutions, given that beforehand I had to perform the
    // demonic act of trying to meticulously project a single texture onto five looping textures.
    public class EternalGardenLakeTargetManager : ModSystem
    {
        public static ManagedRenderTarget LakeTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            // Prepare target creation and draw processes.
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareLakeTarget;
            Main.QueueMainThreadAction(() =>
            {
                LakeTarget = new(true, (width, height) =>
                {
                    return new(Main.instance.GraphicsDevice, (int)(width * 1f), (int)(height * 1f), true, SurfaceFormat.Color, DepthFormat.Depth24, 1, RenderTargetUsage.PreserveContents);
                });
            });
        }

        private void PrepareLakeTarget()
        {
            // Do nothing if not in the garden or if Nameless is present.
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame || NamelessDeitySky.KaleidoscopeInterpolant > 0f || NamelessDeitySky.HeavenlyBackgroundIntensity > 0f)
                return;

            // Move to the lake render target.
            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(LakeTarget);
            gd.Clear(Color.Transparent);

            // Draw the background to the target.
            DrawLake();

            // Return to the backbuffer.
            gd.SetRenderTarget(null);
        }

        public static void DrawLake()
        {
            // Don't draw anything if the heavenly background is at full intensity.
            if (NamelessDeitySky.HeavenlyBackgroundIntensity >= 1f)
                return;

            if (Main.netMode == NetmodeID.Server)
                return;

            int frameOffset = (int)(Main.GameUpdateCount / 10U) % EternalGardenSky.BackgroundAnimationFrames;
            Texture2D waterTexture = EternalGardenSky.LakeFrameTextures[frameOffset].Value;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            for (int i = -2; i <= 2; i++)
            {
                Vector2 layerPosition = new(Main.screenWidth * 0.5f + waterTexture.Width * i, Main.screenHeight * 0.5f);
                Main.spriteBatch.Draw(waterTexture, layerPosition - waterTexture.Size() * 0.5f, null, Color.SkyBlue * EternalGardenSky.opacity, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }
    }
}
