using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class ConvergingSupernovaEnergy : ModProjectile, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 70;
            Projectile.scale = 1.5f;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Release short-lived cyan-green sparks.
            if (Main.rand.NextBool(24))
            {
                Color sparkColor = Color.Lerp(Color.ForestGreen, Color.Cyan, Main.rand.NextFloat(0.32f, 0.75f));
                sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = spark.velocity.Length() >= 7.5f;
                spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Gradually accelerate.
            if (Projectile.velocity.Length() <= 16f)
                Projectile.velocity *= 1.03f;

            // Decide rotation.
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() - PiOver2;

            // Fade in and out.
            Projectile.Opacity = InverseLerp(0f, 12f, Time) * InverseLerp(0f, 12f, Projectile.timeLeft);

            Time++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // Necessary to ensure these projectiles draw over the supernova to be at max intensity.
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw afterimages.
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White with { A = 0 }, 1, null, 0.001f, 0.6f);
            return false;
        }
    }
}
