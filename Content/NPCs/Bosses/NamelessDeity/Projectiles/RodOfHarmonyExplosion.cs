﻿using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class RodOfHarmonyExplosion : ModProjectile, IDrawPixelated
    {
        public static int Lifetime => SecondsToFrames(0.2f);

        public float LifetimeRatio => 1f - Projectile.timeLeft / (float)Lifetime;

        public ref float Radius => ref Projectile.ai[0];

        public static float IdealRadius => 300f;

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
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
            Radius = Lerp(Radius, IdealRadius, 0.06f);
            Projectile.Opacity = InverseLerp(2f, 10f, Projectile.timeLeft);

            // Randomly create small magic particles.
            float fireVelocityArc = Pi * InverseLerp(Lifetime, 0f, Projectile.timeLeft);
            for (int i = 0; i < 4; i++)
            {
                Vector2 particleSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Radius * Projectile.scale * Main.rand.NextFloat(0.66f, 0.98f);
                Vector2 particleVelocity = (particleSpawnPosition - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(fireVelocityArc) * Main.rand.NextFloat(1f, 6f);
                SquishyLightParticle particle = new(particleSpawnPosition, particleVelocity, Main.rand.NextFloat(0.1f, 0.23f), Color.Lerp(Color.Violet, Color.HotPink, Main.rand.NextFloat(0.8f)), Main.rand.Next(21, 47));
                particle.Spawn();
            }
        }

        public void DrawWithPixelation()
        {
            Main.spriteBatch.PrepareForShaders();
            DrawData explosionDrawData = new(ViscousNoise, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

            var shockwaveShader = ShaderManager.GetShader("ShockwaveShader");
            shockwaveShader.TrySetParameter("shockwaveColor", Color.Lerp(Color.BlueViolet, Color.LightGoldenrodYellow, Pow(1f - LifetimeRatio, 1.45f) * 0.95f));
            shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
            shockwaveShader.TrySetParameter("projectilePosition", Projectile.Center - Main.screenPosition);
            shockwaveShader.TrySetParameter("shockwaveOpacityFactor", Projectile.Opacity);
            shockwaveShader.Apply();
            explosionDrawData.Draw(Main.spriteBatch);
            Main.spriteBatch.ResetToDefault();
        }
    }
}
