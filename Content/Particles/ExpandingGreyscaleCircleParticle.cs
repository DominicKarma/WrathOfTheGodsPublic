using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class ExpandingGreyscaleCircleParticle : Particle
    {
        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.ExpandingGreyscaleCircleParticle.png";

        public ExpandingGreyscaleCircleParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Lifetime - Time);
            Scale += Vector2.One * 0.9f;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, null, Scale * 0.4f, 0);
        }
    }
}
