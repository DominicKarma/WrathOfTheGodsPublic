using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Metaballs
{
    public class NoxusGasMetaball : Metaball
    {
        public class GasParticle
        {
            public float Size;

            public Vector2 Velocity;

            public Vector2 Center;
        }

        public static readonly List<GasParticle> GasParticles = new();

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.AfterProjectiles;

        public override Color EdgeColor => new(202, 110, 234);

        public override bool AnythingToDraw => GasParticles.Any() || AnyProjectiles(ModContent.ProjectileType<DarkComet>());

        public override List<Texture2D> Layers => new()
        {
            ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/NoxusGasLayer1").Value
        };

        public static void CreateParticle(Vector2 spawnPosition, Vector2 velocity, float size)
        {
            GasParticles.Add(new()
            {
                Center = spawnPosition,
                Velocity = velocity,
                Size = size
            });
        }

        public override void Update()
        {
            foreach (GasParticle particle in GasParticles)
            {
                particle.Velocity *= 0.99f;
                particle.Size *= 0.93f;
                particle.Center += particle.Velocity;
            }
            GasParticles.RemoveAll(p => p.Size <= 2f);
        }

        public override void DrawInstances()
        {
            Texture2D circle = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/BasicCircle").Value;
            foreach (GasParticle particle in GasParticles)
                Main.spriteBatch.Draw(circle, particle.Center - Main.screenPosition, null, Color.White, 0f, circle.Size() * 0.5f, new Vector2(particle.Size) / circle.Size(), 0, 0f);

            foreach (Projectile p in Main.projectile.Where(p => p.active))
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
