using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class PulseRing : Particle
    {
        public float OriginalScale;

        public float FinalScale;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.PulseRing.png";

        public PulseRing(Vector2 position, Vector2 velocity, Color color, float originalScale, float finalScale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = Vector2.One * originalScale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
        }

        public override void Update()
        {
            Scale = Vector2.One * Lerp(OriginalScale, FinalScale, Pow(LifetimeRatio, 0.25f));
            Opacity = Cos(LifetimeRatio * PiOver2);
            Velocity *= 0.95f;

            Lighting.AddLight(Position, DrawColor.R / 255f, DrawColor.G / 255f, DrawColor.B / 255f);
        }
    }
}
