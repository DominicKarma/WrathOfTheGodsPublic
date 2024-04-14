using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class LargeMistParticle : Particle
    {
        public float Spin;

        public float Gravity;

        public bool UseAdditiveBlending;

        public override int FrameCount => 4;

        public override BlendState BlendState => UseAdditiveBlending ? BlendState.Additive : SubtractiveBlending;

        public override string AtlasTextureName => "NoxusBoss.LargeMistParticle.png";

        public LargeMistParticle(Vector2 position, Vector2 velocity, Color color, float scale, float gravity, int lifetime, float rotationSpeed = 0f, bool additive = false)
        {
            Position = position;
            Velocity = velocity;
            Scale = Vector2.One * scale;
            DrawColor = color;
            Rotation = Main.rand.NextFloat(TwoPi);
            Spin = rotationSpeed;
            Lifetime = lifetime;
            Gravity = gravity;
            UseAdditiveBlending = additive;
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0f) ? 1f : -1f);
            Velocity.X *= 0.89f;
            Scale *= 1.01f;

            if (Collision.SolidCollision(Position, 1, 1))
            {
                Velocity.Y *= 0.6f;
                Velocity.X = 0f;
                Scale *= 1.015f;
                Time += 3;
            }
            else
                Velocity.Y += Gravity;

            Opacity = Pow(1f - LifetimeRatio, 0.7f) * InverseLerp(0f, 12f, Time);
        }
    }
}
