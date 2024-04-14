using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class BloodParticle2 : Particle
    {
        public Color InitialColor;

        public override BlendState BlendState => BlendState.NonPremultiplied;

        public override string AtlasTextureName => "NoxusBoss.BloodParticle2.png";

        public BloodParticle2(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color)
        {
            Position = relativePosition;
            Velocity = velocity;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
            InitialColor = color;
            DrawColor = color;
        }

        public override void Update()
        {
            Velocity *= 0.98f;
            DrawColor = Color.Lerp(InitialColor, Color.Transparent, Pow(LifetimeRatio, 4f));
            Rotation = Velocity.ToRotation();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float brightness = Pow(Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15f);
            Rectangle frame = Texture.Frame.Subdivide(1, 3, 0, (int)(LifetimeRatio * 3f));
            spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, DrawColor * brightness, Rotation, null, Scale, SpriteEffects.None);
        }
    }
}
