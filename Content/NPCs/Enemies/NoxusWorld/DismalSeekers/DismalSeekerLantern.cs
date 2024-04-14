using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Particles;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Enemies.NoxusWorld.DismalSeekers
{
    public class DismalSeekerLantern : ModProjectile
    {
        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        // Used instead of Projectile.rotation so that this value can be easily transferred from the rotation the Dismal Seeker was using when the lantern was thrown.
        public ref float Rotation => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public static float TerminalVelocity => 15f;

        public static float Gravity => 0.45f;

        public static float AirResistanceFactor => 0.982f;

        public override void SetStaticDefaults()
        {
            if (Main.netMode != NetmodeID.Server)
                MyTexture = ModContent.Request<Texture2D>(Texture);
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            // Apply air resistance.
            Projectile.velocity.X *= AirResistanceFactor;

            // Adhere to gravity.
            if (Projectile.velocity.Y < TerminalVelocity)
                Projectile.velocity.Y += Gravity;

            // Weakly seek out the nearest player.
            Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.velocity += Projectile.SafeDirectionTo(closest.Center) * 0.33f;

            // Idly emit light.
            Lighting.AddLight(Projectile.Center, Color.Violet.ToVector3() * 0.67f);

            // SPEEEEEEEEEEEEEEEN!!!
            Rotation += Projectile.velocity.X * 0.05f;
            Projectile.spriteDirection = Sign(Projectile.velocity.X);

            // Emit smoke and fire particles.
            EmitIdleParticles(Projectile.Center, Projectile.velocity, Rotation, Projectile.Opacity);

            Time++;
        }

        public static void EmitIdleParticles(Vector2 center, Vector2 velocity, float lanternRotation, float opacity)
        {
            // Emit lantern smoke and fire.
            if (Main.rand.NextBool(3))
            {
                Dust smoke = Dust.NewDustPerfect(center + Vector2.UnitY.RotatedBy(lanternRotation) * 8f, DustID.Smoke);
                smoke.velocity = -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(7.5f) - velocity * 0.3f;
                smoke.alpha = (int)Lerp(255f, 160f, opacity);
                smoke.noGravity = true;
            }
            if (opacity >= 0.6f && Main.rand.NextBool())
            {
                Dust fire = Dust.NewDustPerfect(center + Vector2.UnitY.RotatedBy(lanternRotation) * 8f, 264);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(2f) + velocity * 1.4f;
                fire.color = Color.Lerp(Color.DarkBlue, Color.Fuchsia, Main.rand.NextFloat(0.8f)) * opacity;
                fire.fadeIn = 0.2f;
                fire.noLight = true;
                fire.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Create a custom explosion sound.
            SoundEngine.PlaySound(DismalSeeker.LanternExplodeSound, Projectile.Center);

            // Create a bunch of fire and smoke dust.
            for (int i = 0; i < 18; i++)
            {
                if (Main.rand.NextBool())
                {
                    Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.Smoke);
                    smoke.velocity = -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(7.5f);
                    smoke.alpha = 226;
                    smoke.noGravity = true;
                }
                if (Projectile.Opacity >= 0.6f)
                {
                    Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), 264);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(11f);
                    fire.color = Color.Lerp(Color.DarkBlue, Color.Fuchsia, Main.rand.NextFloat(0.8f)) * Projectile.Opacity;
                    fire.fadeIn = 0.96f;
                    fire.noLight = true;
                    fire.noGravity = fire.velocity.Length() >= 4f;
                }
            }

            // Create a bunch of fire particles.
            for (int i = 0; i < 50; i++)
            {
                float particleScale = Main.rand.NextFloat(0.67f, 1.1f);
                Color fireColor = Color.Lerp(Color.MediumPurple, Color.Blue, Main.rand.NextFloat(0.8f));
                fireColor = Color.Lerp(fireColor, Color.Wheat, Main.rand.NextFloat(0.7f));
                fireColor.A = 0;

                Vector2 fireVelocity = -Vector2.UnitY.RotatedByRandom(1.24f) * Main.rand.NextFloat(2f, 8.5f) * particleScale;
                var smoke = new SmallSmokeParticle(Projectile.Center, fireVelocity, fireColor * 0.9f, fireColor * 0.4f, particleScale * 2.5f, 55f, Main.rand.NextFloatDirection() * 0.02f)
                {
                    Rotation = Main.rand.NextFloat(TwoPi),
                };
                smoke.Spawn();
            }

            // Create a burning shockwave.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NewProjectileBetter(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LanternExplosion>(), 60, 0f);

            // Shake the screen a little bit.
            StartShakeAtPoint(Projectile.Center, 5f, shakeStrengthDissipationIncrement: 0.3f);

            // Create lantern piece gores.
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 1; i <= 3; i++)
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2CircularEdge(3f, 4f) - Vector2.UnitY * 5.1f, ModContent.Find<ModGore>(Mod.Name, $"DismalSeekerLantern{i}").Type, Projectile.scale);
            }

            // Die.
            Projectile.Kill();
            return true;
        }

        public static void DrawLantern(Vector2 lanternDrawPosition, float lanternRotation, float opacity, float backglowOpacity, float scale, Vector2 opacityAdjustedScale, SpriteEffects direction, float lanternShineInterpolant = 0f)
        {
            // Collect textures.
            Texture2D lanternTexture = MyTexture.Value;

            // Draw the lantern backglow.
            Vector2 backglowDrawPosition = lanternDrawPosition + Vector2.UnitY.RotatedBy(lanternRotation) * lanternTexture.Height * opacityAdjustedScale * 0.5f;
            Main.spriteBatch.Draw(BloomCircleSmall, backglowDrawPosition, null, Color.Wheat with { A = 0 } * backglowOpacity * 0.7f, 0f, BloomCircleSmall.Size() * 0.5f, scale * 1.1f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, backglowDrawPosition, null, Color.Violet with { A = 0 } * backglowOpacity * 0.4f, 0f, BloomCircleSmall.Size() * 0.5f, scale * 2.26f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, backglowDrawPosition, null, Color.DarkBlue with { A = 0 } * backglowOpacity * 0.26f, 0f, BloomCircleSmall.Size() * 0.5f, scale * 3.03f, 0, 0f);

            // Draw the lantern.
            Main.spriteBatch.Draw(lanternTexture, lanternDrawPosition, null, Color.White * opacity, lanternRotation, lanternTexture.Size() * new Vector2(0.5f, 0f), opacityAdjustedScale * 0.8f, direction, 0f);

            // Draw the lantern shine if it's in use.
            if (lanternShineInterpolant > 0f && lanternShineInterpolant < 1f)
            {
                float shineIntensity = Pow(Convert01To010(lanternShineInterpolant), 2f);
                Vector2 shineScale = new Vector2(1.15f, 0.9f) * shineIntensity * opacityAdjustedScale * 0.1f;
                Color shineColor = (Color.LightPink with { A = 0 }) * shineIntensity * 0.9f;
                Main.spriteBatch.Draw(BrightSpiresTexture, backglowDrawPosition, null, shineColor, 0f, BrightSpiresTexture.Size() * 0.5f, shineScale, 0, 0f);
                Main.spriteBatch.Draw(BrightSpiresTexture, backglowDrawPosition, null, shineColor, PiOver2, BrightSpiresTexture.Size() * 0.5f, shineScale, 0, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the lantern.
            Vector2 lanternDrawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects direction = Projectile.spriteDirection.ToSpriteDirection();
            DrawLantern(lanternDrawPosition, Rotation, Projectile.Opacity, Projectile.Opacity, Projectile.scale, Vector2.One * Projectile.scale, direction);
            return false;
        }
    }
}
