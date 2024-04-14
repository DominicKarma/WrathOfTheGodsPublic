using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class SmallSmokeParticle : Particle
    {
        public float Spin;

        public float Alpha;

        public Color ColorFire;

        public Color ColorFade;

        public override string AtlasTextureName => "NoxusBoss.SmallSmokeParticle.png";

        public SmallSmokeParticle(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float opacity, float rotationSpeed = 0f)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            Scale = Vector2.One * scale;
            Alpha = opacity;
            Rotation = Main.rand.NextFloat(TwoPi);
            Opacity = 1f;
            Spin = rotationSpeed;
            Lifetime = 9999;
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0f) ? 1f : -1f);
            Velocity *= 0.85f;
            if (Alpha > 90f)
            {
                Lighting.AddLight(Position, DrawColor.ToVector3() * 0.1f);
                Scale += Vector2.One * 0.01f;
                Alpha -= 3f;
            }
            else
            {
                Scale *= 0.975f;
                Alpha -= 0.6f;
            }

            if (Alpha < 0f)
                Kill();

            DrawColor = Color.Lerp(ColorFire, ColorFade, Saturate((255f - Alpha - 100f) / 80f)) * (Alpha / 255f);
        }
    }
}
