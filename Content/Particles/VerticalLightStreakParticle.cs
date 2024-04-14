using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class VerticalLightStreakParticle : Particle
    {
        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.VerticalLightStreakParticle.png";

        public VerticalLightStreakParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, Vector2 scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Time);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, null, Scale, 0);
        }
    }
}
