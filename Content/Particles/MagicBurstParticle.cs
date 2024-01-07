using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class MagicBurstParticle : Particle
    {
        public float ScaleExpandRate;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/MagicBurst";

        public MagicBurstParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float scaleExpandRate = 0f)
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
            Rectangle frame = Texture.Frame(1, 5, 0, (int)(LifetimeRatio * 4.999f));
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale * 0.8f, 0, 0f);
        }
    }
}
