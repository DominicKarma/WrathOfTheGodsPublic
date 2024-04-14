using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets
{
    public class StarPet : ModProjectile, IDrawsWithShader
    {
        public class StarPetTargetContent : ARenderTargetContentByRequest
        {
            public StarPet Host
            {
                get;
                internal set;
            }

            public static readonly Vector2 Size = new(256, 256);

            protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
            {
                // Initialize the underlying render target if necessary.
                PrepareARenderTarget_AndListenToEvents(ref _target, device, (int)Size.X, (int)Size.Y, RenderTargetUsage.PreserveContents);

                device.SetRenderTarget(_target);
                device.Clear(Color.Transparent);

                // Draw the host's contents to the render target.
                Host.DrawSelf(Size * 0.5f);

                device.SetRenderTarget(null);

                // Mark preparations as completed.
                _wasPrepared = true;
            }
        }

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public static StarPetTargetContent StarDrawContents
        {
            get;
            private set;
        }

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.LightPet[Projectile.type] = true;

            // Register the star target.
            if (Main.netMode != NetmodeID.Server)
            {
                StarDrawContents = new();
                Main.ContentThatNeedsRenderTargets.Add(StarDrawContents);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            CheckActive();

            // Hover near the owner.
            Vector2 hoverDestination = Owner.Center + new Vector2(Owner.direction * -50f, Cos01(Time * 0.056f) * -30f - 36f);
            Projectile.SmoothFlyNear(hoverDestination, 0.199f, 0.81f);

            // Emit a lot of light.
            Lighting.AddLight(Projectile.Center, Vector3.One * 3.2f);

            Time++;
        }

        public void CheckActive()
        {
            // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
            if (!Owner.dead && Owner.HasBuff(ModContent.BuffType<StarPetBuff>()))
                Projectile.timeLeft = 2;
        }

        public void DrawSelf(Vector2 drawPosition)
        {
            // Disable the sprite batch's matrix transformation.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Draw a bloom flare behind everything.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 0.98f + Projectile.identity;
            Color bloomFlareColor1 = Color.LightGoldenrodYellow with { A = 0 } * 0.5f;
            Color bloomFlareColor2 = Color.Red with { A = 0 } * 0.5f;
            float flareScale = Projectile.scale * 0.2f;
            float flareOpacity = Projectile.Opacity;
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor1 * flareOpacity * 0.7f, bloomFlareRotation, BloomFlare.Size() * 0.5f, flareScale, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor2 * flareOpacity * 0.45f, -bloomFlareRotation, BloomFlare.Size() * 0.5f, flareScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, (Color.Orange with { A = 0 }) * flareOpacity * 0.45f, 0f, BloomCircleSmall.Size() * 0.5f, flareScale * 8f, 0, 0f);

            // Prepare the sprite batch for shaders.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Draw the sun itself.
            var fireballShader = ShaderManager.GetShader("NoxusBoss.SunShader");
            fireballShader.TrySetParameter("coronaIntensityFactor", 0.05f);
            fireballShader.TrySetParameter("mainColor", new Color(204, 163, 79));
            fireballShader.TrySetParameter("darkerColor", new Color(204, 92, 25));
            fireballShader.TrySetParameter("subtractiveAccentFactor", new Color(181, 0, 0));
            fireballShader.TrySetParameter("sphereSpinTime", Main.GlobalTimeWrappedHourly * 0.9f);
            fireballShader.SetTexture(WavyBlotchNoise, 1, SamplerState.PointWrap);
            fireballShader.SetTexture(PsychedelicWingTextureOffsetMap, 2, SamplerState.PointWrap);
            fireballShader.Apply();

            Vector2 scale = Vector2.One * Projectile.width * Projectile.scale * 1.5f / DendriticNoiseZoomedOut.Size();
            Main.spriteBatch.Draw(DendriticNoiseZoomedOut, drawPosition, null, Color.White, Projectile.rotation, DendriticNoiseZoomedOut.Size() * 0.5f, scale, 0, 0f);

            Main.spriteBatch.End();
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            // Initialize the star drawer, with this projectile as its current host.
            StarDrawContents.Host = this;
            StarDrawContents.Request();

            // If the star drawer is ready, draw it to the screen.
            // If a dye is in use, apply it.
            if (!StarDrawContents.IsReady)
                return;

            Texture2D target = StarDrawContents.GetTarget();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            DrawData targetData = new(target, drawPosition, target.Frame(), Color.White, 0f, target.Size() * 0.5f, 1f, 0, 0f);
            GameShaders.Armor.Apply(Owner.cLight, Projectile, targetData);
            targetData.Draw(Main.spriteBatch);
        }
    }
}
