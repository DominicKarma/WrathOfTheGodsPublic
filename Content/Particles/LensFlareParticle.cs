using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class LensFlareParticle : Particle
    {
        public float ScaleExpandRate = 0.015f;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.LensFlareParticle.png";

        public LensFlareParticle(Vector2 position, Color color, int lifetime, float scale, float rotation = 0f)
        {
            Position = position;
            Velocity = Vector2.Zero;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
            Rotation = rotation;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Time) * InverseLerp(1f, 0.7f, LifetimeRatio);
            Scale += Vector2.One * ScaleExpandRate;
        }

        public override void Draw(SpriteBatch spriteBatch) => spriteBatch.Draw(Texture, Position - Main.screenPosition, Frame, DrawColor * Opacity, Rotation, null, Scale, Direction.ToSpriteDirection());
    }
}
