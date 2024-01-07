using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class LightSlashDrawer : ModSystem
    {
        public static ManagedRenderTarget SlashTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget SlashTargetPrevious
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareAfterimageTarget;
            Main.QueueMainThreadAction(() =>
            {
                SlashTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
                SlashTargetPrevious ??= new(true, RenderTargetManager.CreateScreenSizedTarget);
            });
        }

        private void PrepareAfterimageTarget()
        {
            // Don't waste resources if Nameless is not present or there are no slashes.
            if (!Filters.Scene[LightSlashesOverlayShaderData.ShaderKey]?.IsActive() ?? true)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Prepare the render target for drawing.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
            gd.SetRenderTarget(SlashTargetPrevious);
            gd.Clear(Color.Transparent);

            // Draw the contents of the previous frame to the target.
            // The color represents exponential decay factors for each RGBA component, since this performs repeated draws across multiple frames.
            Main.spriteBatch.Draw(SlashTarget, Vector2.Zero, new(0.52f, 0.95f, 0.95f, 0.94f));

            // Draw the blur shader to the result.
            ApplyBlurEffects();

            // Draw all slash projectiles to the render target.
            DrawAllSlashes();

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        private static void DrawAllSlashes()
        {
            int slashID = ModContent.ProjectileType<LightSlash>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.type != slashID || !p.active)
                    continue;

                p.As<LightSlash>().DrawToTarget();
            }
        }

        public static void ApplyBlurEffects()
        {
            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(SlashTarget);
            gd.Clear(Color.Transparent);

            // Prepare the afterimage psychedelic shader.
            var afterimageShader = ShaderManager.GetShader("GaussianBlurShader");
            afterimageShader.TrySetParameter("blurOffset", 0.0032f);
            afterimageShader.TrySetParameter("colorMask", Vector4.One);
            afterimageShader.Apply();

            Main.spriteBatch.Draw(SlashTargetPrevious, Vector2.Zero, Color.White);
        }
    }
}
