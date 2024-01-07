using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class BloodParticle2 : Particle
    {
        public Color InitialColor;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/Blood2";

        public BloodParticle2(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color)
        {
            Position = relativePosition;
            Velocity = velocity;
            Scale = scale;
            Lifetime = lifetime;
            InitialColor = color;
            Color = color;
        }

        public override void Update()
        {
            Velocity *= 0.98f;
            Color = Color.Lerp(InitialColor, Color.Transparent, Pow(LifetimeRatio, 4f));
            Rotation = Velocity.ToRotation();
        }

        public override void Draw()
        {
            float brightness = Pow(Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15f);
            Rectangle frame = Texture.Frame(1, 3, 0, (int)(LifetimeRatio * 3f), 0, 0);
            Vector2 origin = frame.Size() * 0.5f;
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * brightness, Rotation, origin, Scale, SpriteEffects.None, 0f);
        }
    }
}
