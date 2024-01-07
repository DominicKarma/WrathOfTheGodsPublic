using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class MediumMistParticle : Particle
    {
        public Color ColorFire;

        public Color ColorFade;

        public float Alpha;

        public float Spin;

        public override int FrameCount => 3;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/MediumMist";

        public MediumMistParticle(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float opacity, float rotationSpeed = 0f)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            Scale = scale;
            Alpha = opacity;
            Rotation = Main.rand.NextFloat(TwoPi);
            Spin = rotationSpeed;
            Frame = Main.rand.Next(FrameCount);
            Lifetime = 120;
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0f) ? 1f : -1f);
            Velocity *= 0.85f;
            if (Alpha > 90f)
            {
                Lighting.AddLight(Position, Color.ToVector3() * 0.1f);
                Scale += 0.01f;
                Alpha -= 3f;
            }
            else
            {
                Scale *= 0.975f;
                Alpha -= 2f;
            }

            if (Alpha < 0f)
                Kill();

            Color = Color.Lerp(ColorFire, ColorFade, Clamp((255f - Alpha - 100f) / 80f, 0f, 1f)) * (Alpha / 255f);
        }
    }
}
