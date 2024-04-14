using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class MagicBurstParticle : Particle
    {
        public float ScaleExpandRate;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.MagicBurstParticle.png";

        public MagicBurstParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float scaleExpandRate = 0f)
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
            Rectangle frame = Texture.Frame.Subdivide(1, 5, 0, (int)(LifetimeRatio * 4.999f));
            spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, DrawColor * Opacity, Rotation, null, Scale * 0.8f, 0);
        }
    }
}
