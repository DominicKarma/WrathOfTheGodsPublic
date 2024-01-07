using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class PulseRing : Particle
    {
        public float OriginalScale;

        public float FinalScale;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/PulseRing";

        public PulseRing(Vector2 position, Vector2 velocity, Color color, float originalScale, float finalScale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
        }

        public override void Update()
        {
            Scale = Lerp(OriginalScale, FinalScale, Pow(LifetimeRatio, 0.25f));
            Opacity = Cos(LifetimeRatio * PiOver2);

            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }
    }
}
