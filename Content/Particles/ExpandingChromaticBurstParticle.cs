using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class ExpandingChromaticBurstParticle : Particle
    {
        public float ScaleExpandRate;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => ChromaticBurstPath;

        public ExpandingChromaticBurstParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float scaleExpandRate = 0.8f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
            ScaleExpandRate = scaleExpandRate;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Lifetime - Time);
            Scale += ScaleExpandRate;
        }

        public override void Draw()
        {
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity, Rotation, Texture.Size() * 0.5f, Scale * 0.3f, 0, 0f);
        }
    }
}
