using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class SlowSolarSpark : ModProjectile, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 120;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 1.25f;
        }

        public override void AI()
        {
            // Release short-lived sparks.
            if (Main.rand.NextBool(24))
            {
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Wheat, Main.rand.NextFloat(0.2f, 0.84f));
                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.TreasureSparkle);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = true;
                spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Gradually accelerate. This is mostly to give a smooth aesthetic moreso than as a gameplay mechanic, since this attack is supposed to be one that requires weaving.
            if (Projectile.velocity.Length() <= 24f)
                Projectile.velocity += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.15f;

            // Decide rotation.
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() - PiOver2;

            // Fade in and out.
            Projectile.Opacity = InverseLerpBump(0f, 4f, Lifetime - 18f, Lifetime, Time);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw afterimages.
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White with { A = 0 }, 1, null, 0.001f, 0.5f);
            return false;
        }
    }
}
