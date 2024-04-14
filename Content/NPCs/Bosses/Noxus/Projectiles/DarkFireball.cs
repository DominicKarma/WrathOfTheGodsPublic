using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class DarkFireball : ModProjectile, IProjOwnedByBoss<EntropicGod>
    {
        public bool SetActiveFalseInsteadOfKill => true;

        public ref float Time => ref Projectile.ai[0];

        public ref float ExplodeDelay => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 108;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Create a lot of smoke on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 16; i++)
                {
                    Color voidColor = Color.Lerp(Color.Purple, Color.Black, Main.rand.NextFloat(0.54f, 0.9f));
                    voidColor = Color.Lerp(voidColor, Color.DarkBlue, Main.rand.NextFloat(0.4f));
                    Vector2 darkGasVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.87f) * Main.rand.NextFloat(1f, 6.8f);
                    HeavySmokeParticle darkGas = new(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), darkGasVelocity, voidColor, 27, Projectile.scale * 1.24f, Projectile.Opacity * 0.6f, Main.rand.NextFloat(0.02f), true);
                    darkGas.Spawn();
                }
                Projectile.localAI[0] = 1f;
            }

            // Spawn gas particles.
            for (int i = 0; i < 2; i++)
            {
                Color voidColor = Color.Lerp(Color.Purple, Color.Black, Main.rand.NextFloat(0.54f, 0.9f));
                voidColor = Color.Lerp(voidColor, Color.DarkBlue, Main.rand.NextFloat(0.4f));
                HeavySmokeParticle darkGas = new(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextVector2Circular(1f, 1f), voidColor, 11, Projectile.scale * 1.24f, Projectile.Opacity * 0.6f, Main.rand.NextFloat(0.02f), true);
                darkGas.Spawn();
            }

            // Spawn smoke.
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.Zero);
            Vector2 smokeVelocity = -currentDirection.RotatedByRandom(0.93f) * Main.rand.NextFloat(1f, 7f);
            Dust smokeDust = Dust.NewDustPerfect(Projectile.Center, 31);
            smokeDust.velocity = smokeVelocity * Main.rand.NextFloat(0.3f, 1.1f) + Projectile.velocity;
            smokeDust.scale *= 0.92f;
            smokeDust.fadeIn = -0.2f;
            smokeDust.noGravity = true;

            // Decelerate a little bit over time.
            Projectile.velocity *= 0.9945f;

            // Decide the current rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Perform animations.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            Time++;
            if (Time >= 108f - ExplodeDelay)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // Explode on death.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NewProjectileBetter(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NoxusExplosion>(), EntropicGod.ExplosionDamage, 0f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float explosionInterpolant = Pow(InverseLerp(60f, 0f, Projectile.timeLeft), 0.48f);
            float explosionPulse = Cos01(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity);
            float alphaFactor = Lerp(0.25f, 1f, explosionPulse * explosionInterpolant);

            Color drawColor = Color.White;
            drawColor.A = (byte)(drawColor.A * alphaFactor);
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], drawColor);
            return false;
        }
    }
}
