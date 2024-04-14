using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class TileOverlaysSystem : ModSystem
    {
        private static bool overlayDrewLastFrame;

        public static ManagedRenderTarget OverlayableTarget
        {
            get;
            private set;
        }

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() => OverlayableTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget));
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareOverlayTarget;
            On_Main.DrawProjectiles += DrawOverlayTarget;
        }

        private void PrepareOverlayTarget()
        {
            if (OverlayableTarget is null)
                return;

            // Verify that there's anything to draw.
            if (!Main.projectile.Any(p => p.active && p.ModProjectile is IDrawsOverTiles))
            {
                overlayDrewLastFrame = false;
                return;
            }

            var gd = Main.instance.GraphicsDevice;

            gd.SetRenderTarget(OverlayableTarget);
            gd.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw all projectiles that have the relevant interface.
            overlayDrewLastFrame = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is IDrawsOverTiles drawer)
                {
                    drawer.DrawOverTiles(Main.spriteBatch);
                    overlayDrewLastFrame = true;
                }
            }

            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        private void DrawOverlayTarget(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            if (!overlayDrewLastFrame)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Prepare the overlay shader and supply it with tile information.
            var shader = ShaderManager.GetShader("NoxusBoss.TileOverlayShader");
            shader.TrySetParameter("zoom", new Vector2(1.15f, 1.27f));
            shader.TrySetParameter("tileOverlayOffset", (Main.sceneTilePos - Main.screenPosition) / Main.ScreenSize.ToVector2() * -1f);
            shader.TrySetParameter("inversionZoom", Main.GameViewMatrix.Zoom);
            shader.SetTexture(Main.instance.tileTarget, 1);
            shader.SetTexture(Main.instance.blackTarget, 2);
            shader.Apply();

            Main.spriteBatch.Draw(OverlayableTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
    }
}
