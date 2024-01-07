using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class VerticalLightStreakParticle : Particle
    {
        public Vector2 ScaleVector;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/VerticalLightStreak";

        public VerticalLightStreakParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, Vector2 scale)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            ScaleVector = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Time);
        }

        public override void Draw()
        {
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity, Rotation, Texture.Size() * 0.5f, ScaleVector, 0, 0f);
        }
    }
}
