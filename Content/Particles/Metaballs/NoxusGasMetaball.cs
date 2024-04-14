using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles.Metaballs
{
    public class NoxusGasMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "NoxusBoss.BasicMetaballCircle.png";

        public override Color EdgeColor => Color.MediumPurple;

        public override bool ShouldRender => ActiveParticleCount >= 1 || AnyProjectiles(ModContent.ProjectileType<DarkComet>());

        public override Func<Texture2D>[] LayerTextures => [() => ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/Metaballs/NoxusGasLayer1").Value];

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Velocity = Collision.TileCollision(particle.Center, particle.Velocity, 1, 1);
            if (particle.Velocity.Y == 0f)
            {
                particle.Velocity.X *= 0.5f;
                particle.Size *= 0.93f;
            }

            particle.Size *= 0.93f;
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 2f;

        public override void ExtraDrawing()
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == ModContent.ProjectileType<DarkComet>())
                {
                    Color c = Color.White;
                    c.A = 0;
                    p.ModProjectile.PreDraw(ref c);
                }
            }
        }
    }
}
