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
    public class RedirectingDarkFlame : ModProjectile, IDrawAdditive
    {
        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> UndirectionedTexture
        {
            get;
            private set;
        }

        // The more "undirectioned" the projectile is. As a fireball speeds up it naturally should have a "direction" in the form of its lag-behind tail
        // However, this makes little sense when a fire is just idling in place, in which case it should just emit embers in all directions.
        public float UndirectionedInterpolant => InverseLerp(7f, 1.4f, Projectile.velocity.Length());

        public float FrontglowInterpolant => InverseLerp(FastAccelerationDelay + 10f, FastAccelerationDelay - 30f, Time) * (FadeoutCountdown >= 1f ? InverseLerp(0f, FadeoutCountdownMax, FadeoutCountdown) : 1f);

        public ref float Time => ref Projectile.ai[0];

        public ref float FadeoutCountdown => ref Projectile.ai[1];

        public static int FastAccelerationDelay => 60;

        public static int FadeoutCountdownMax => 18;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;

            if (Main.netMode != NetmodeID.Server)
            {
                MyTexture = ModContent.Request<Texture2D>(Texture);
                UndirectionedTexture = ModContent.Request<Texture2D>($"{Texture}Undirectioned");
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            // Dissipate if fading out.
            if (FadeoutCountdown > 0f)
            {
                Projectile.Opacity = Clamp(Projectile.Opacity - 1.02f / FadeoutCountdownMax, 0f, 1f);

                FadeoutCountdown--;
                if (FadeoutCountdown <= 0f)
                    Projectile.Kill();
                return;
            }

            // Rotate.
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Release smoke and fire.
            if (Main.rand.NextBool(3))
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Smoke);
                smoke.velocity = Main.rand.NextVector2Circular(4f, 4f) - Projectile.velocity * 0.8f;
                smoke.alpha = (int)Lerp(255f, 100f, Projectile.Opacity);
                smoke.noGravity = true;
            }
            if (Projectile.Opacity >= 0.6f && Main.rand.NextFloat() < 0.9f / Pow(Projectile.velocity.Length(), 0.45f))
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f), DustID.PlatinumCoin);
                fire.velocity = Main.rand.NextVector2Circular(2f, 2f) - Projectile.velocity * 0.4f;
                fire.color = Color.Lerp(Color.DarkBlue, Color.Fuchsia, Main.rand.NextFloat(0.8f));
                fire.color = Color.Lerp(fire.color, Color.Wheat, Main.rand.NextFloat(0.7f)) * Projectile.Opacity;
                fire.fadeIn = 0.3f;
                fire.scale = Main.rand.NextFloat(0.8f, 1.3f);
                fire.noLight = true;
                fire.noGravity = true;
            }

            // Die in water.
            if (Collision.WetCollision(Projectile.TopLeft, Projectile.width, Projectile.height))
            {
                SoundEngine.PlaySound(DismalSeeker.EmberExtinguishSound, Projectile.Center);

                // Release smoke particles.
                for (int i = 0; i < 11; i++)
                {
                    Color smokeColor = Color.Lerp(Color.Fuchsia, Color.DarkSlateGray, Main.rand.NextFloat(0.67f, 0.9f));
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(0.71f) * Main.rand.NextFloat(0.6f, 6f);
                    SmallSmokeParticle smoke = new(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), smokeVelocity, smokeColor with { A = 0 } * 0.51f, Color.DarkGray * 0.3f, 1.65f, 152f);
                    smoke.Spawn();
                }

                Projectile.Kill();
            }

            // Accelerate over time. This effect is significantly slower at first.
            float acceleration = Time < FastAccelerationDelay ? 0.003f : 0.023f;
            Projectile.velocity *= 1f + acceleration;

            // Arc towards the nearest player a bit while accelerating.
            float arcInterpolant = InverseLerp(0f, 90f, Time - FastAccelerationDelay);
            float arcForce = arcInterpolant * 0.18f;
            Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.velocity = (Projectile.velocity + Projectile.SafeDirectionTo(closest.Center) * arcForce).ClampLength(0f, 19f);

            Time++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Initialize the fadeout countdown.
            if (FadeoutCountdown <= 0f)
            {
                FadeoutCountdown = FadeoutCountdownMax;
                Projectile.position += oldVelocity * 1.8f;
                Projectile.netUpdate = true;
            }

            Projectile.velocity = Vector2.Zero;
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.8f && Time >= FastAccelerationDelay - 10f;

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Collect textures.
            Texture2D texture = MyTexture.Value;
            Texture2D undirectionedTexture = UndirectionedTexture.Value;

            // Draw the fireball. This is diminished somewhat if the frontglow is strong enough.
            float flameFrontglowOpacity = Lerp(1f, 0.2f, FrontglowInterpolant);
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(Color.White) * flameFrontglowOpacity;
            Main.spriteBatch.Draw(undirectionedTexture, drawPosition, null, color * UndirectionedInterpolant, Projectile.rotation, undirectionedTexture.Size() * 0.5f, Projectile.scale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color * (1f - UndirectionedInterpolant), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);

            // Draw the frontglow.
            float frontglowScale = Projectile.scale * Lerp(0.8f, 0.3f, FrontglowInterpolant);
            float frontglowOpacity = Lerp(0.4f, 1f, FrontglowInterpolant);
            Color frontglowColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Projectile.identity / 11f % 0.75f) * frontglowOpacity;
            Color frontglowColorOuter = Color.Lerp(Color.HotPink, Color.IndianRed, Projectile.identity / 8f % 0.67f) * frontglowOpacity * 0.5f;
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, frontglowColor, 0f, BloomCircleSmall.Size() * 0.5f, frontglowScale * 1.3f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, frontglowColorOuter, 0f, BloomCircleSmall.Size() * 0.5f, frontglowScale * 2.25f, 0, 0f);
        }
    }
}
