using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class GlowyShardParticle : Particle
    {
        public int FrameY;

        public float BackglowScale;

        public Color BackglowColor;

        public override int FrameCount => 3;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.GlowyShardParticle.png";

        public GlowyShardParticle(Vector2 position, Vector2 velocity, Color color, Color backglowColor, float scale, float backglowScale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            BackglowColor = backglowColor;
            Scale = Vector2.One * scale;
            BackglowScale = backglowScale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
            FrameY = Main.rand.Next(FrameCount);
        }

        public override void Update()
        {
            Opacity = InverseLerpBump(0f, 0.14f, 0.67f, 1f, LifetimeRatio);
            Velocity *= 0.98f;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw the backglow.
            float backglowScale = BackglowScale * Opacity;
            Main.spriteBatch.Draw(BloomCircleSmall, Position - Main.screenPosition, null, BackglowColor * Opacity, 0f, BloomCircleSmall.Size() * 0.5f, backglowScale, 0, 0f);

            // Draw the particle as usual above the backglow.
            base.Draw(spriteBatch);
        }
    }
}
