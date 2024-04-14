using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles.Metaballs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class NoxusGas : ModProjectile, IProjOwnedByBoss<EntropicGod>
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 86;
            Projectile.height = 86;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            float gasSize = InverseLerp(-3f, 25f, Time) * Projectile.width * 0.68f;
            float angularOffset = Sin(Time / 5f) * 0.77f;
            ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(Projectile.Center + Projectile.velocity * 3f, Main.rand.NextVector2Circular(2f, 2f) + Projectile.velocity.RotatedBy(angularOffset).RotatedByRandom(0.6f) * 0.26f, gasSize);

            // Dissipate if on top of the nearest player.
            Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.WithinRange(closest.Center, 35f))
            {
                for (int i = 0; i < 25; i++)
                {
                    Vector2 gasParticleVelocity = Main.rand.NextVector2Circular(3.2f, 3.2f) + Projectile.velocity * 0.2f;
                    ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(Projectile.Center, gasParticleVelocity, gasSize * Main.rand.NextFloat(0.2f, 0.46f));
                }

                Projectile.Kill();
            }

            Time++;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
