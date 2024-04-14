using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class ExpandingChromaticBurstParticle : Particle
    {
        public float ScaleExpandRate;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.ExpandingChromaticBurstParticle.png";

        public ExpandingChromaticBurstParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float scaleExpandRate = 0.8f)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
            ScaleExpandRate = scaleExpandRate;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Lifetime - Time);
            Scale += Vector2.One * ScaleExpandRate;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, null, Scale * 0.3f, 0);
        }
    }
}
