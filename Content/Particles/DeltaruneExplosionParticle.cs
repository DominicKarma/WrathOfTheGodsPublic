using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Content.Particles
{
    public class DeltaruneExplosionParticle : Particle
    {
        public override int FrameCount => 16;

        public override string AtlasTextureName => "NoxusBoss.DeltaruneExplosionParticle.png";

        public DeltaruneExplosionParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Frame = Texture.Frame.Subdivide(1, FrameCount, 0, (int)(LifetimeRatio * FrameCount));
        }
    }
}
