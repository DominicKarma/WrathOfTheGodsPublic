using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics.Particles;

namespace NoxusBoss.Content.Particles
{
    public class DeltaruneExplosionParticle : Particle
    {
        public override int FrameCount => 16;

        public override string TexturePath => "NoxusBoss/Content/Particles/DeltaruneExplosionParticle";

        public DeltaruneExplosionParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Frame = (int)(LifetimeRatio * FrameCount);
        }
    }
}
