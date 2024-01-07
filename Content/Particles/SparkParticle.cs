using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class SparkParticle : Particle
    {
        public Color InitialColor;

        public bool AffectedByGravity;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "Terraria/Images/Extra_89";

        public SparkParticle(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, float scale, Color color)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Lifetime = lifetime;
            InitialColor = color;
            Color = color;
        }

        public override void Update()
        {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, Pow(LifetimeRatio, 3f));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + PiOver2;
        }

        public override void Draw()
        {
            Vector2 scale = new Vector2(0.5f, 1.6f) * Scale;
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation, Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation, Texture.Size() * 0.5f, scale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
        }
    }
}
