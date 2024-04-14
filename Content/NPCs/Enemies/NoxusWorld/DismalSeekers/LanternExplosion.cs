using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Enemies.NoxusWorld.DismalSeekers
{
    public class LanternExplosion : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public static int Lifetime => SecondsToFrames(0.3f);

        public float LifetimeRatio => 1f - Projectile.timeLeft / (float)Lifetime;

        public ref float Radius => ref Projectile.ai[0];

        public static float IdealRadius => 320f;

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            // Make screen shove effects happen on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                RadialScreenShoveSystem.Start(Projectile.Center, 16);
                StartShakeAtPoint(Projectile.Center, 6f);
                Projectile.localAI[0] = 1f;
            }

            // Cause the explosion to expand outward.
            Radius = Lerp(Radius, IdealRadius, 0.057f);
            Projectile.Opacity = InverseLerp(2f, 10f, Projectile.timeLeft);
            if (Projectile.Opacity <= 0.8f)
                Projectile.damage = 0;

            Vector2 originalSize = Projectile.Size;
            Projectile.Size = Vector2.One * Radius * 1.1f;
            Projectile.position -= (Projectile.Size - originalSize) * 0.5f;

            // Randomly create small fire particles.
            float fireVelocityArc = Pi * InverseLerp(Lifetime, 0f, Projectile.timeLeft);
            for (int i = 0; i < 4; i++)
            {
                Vector2 particleSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Radius * Projectile.scale * Main.rand.NextFloat(0.66f, 0.98f);
                Vector2 particleVelocity = (particleSpawnPosition - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(fireVelocityArc) * Main.rand.NextFloat(1f, 6f);
                SquishyLightParticle particle = new(particleSpawnPosition, particleVelocity, Main.rand.NextFloat(0.1f, 0.23f), Color.Lerp(Color.Wheat, Color.Violet, Main.rand.NextFloat(0.8f)), Main.rand.Next(25, 44));
                particle.Spawn();
            }
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            DrawData explosionDrawData = new(ViscousNoise, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

            var shockwaveShader = ShaderManager.GetShader("NoxusBoss.ShockwaveShader");
            shockwaveShader.TrySetParameter("shockwaveColor", Color.Lerp(Color.DarkSlateBlue, Color.Fuchsia, Pow(1f - LifetimeRatio, 1.45f) * 0.9f));
            shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
            shockwaveShader.TrySetParameter("projectilePosition", Projectile.Center - Main.screenPosition);
            shockwaveShader.TrySetParameter("shockwaveOpacityFactor", Projectile.Opacity);
            shockwaveShader.Apply();
            explosionDrawData.Draw(Main.spriteBatch);
        }
    }
}
