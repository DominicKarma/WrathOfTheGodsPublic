using System;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class BloodParticle : Particle
    {
        public Color InitialColor;

        public override BlendState BlendState => BlendState.NonPremultiplied;

        public override string AtlasTextureName => "NoxusBoss.BloodParticle.png";

        public BloodParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color)
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
            Scale *= 0.98f;
            Velocity.X *= 0.97f;
            Velocity.Y = Clamp(Velocity.Y + 0.9f, -22f, 22f);
            DrawColor = Color.Lerp(InitialColor, Color.Transparent, LifetimeRatio.Cubed());
            Rotation = Velocity.ToRotation() + PiOver2;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(Velocity.Y), true) * 0.84f;
            float brightness = Pow(Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15f);
            Vector2 scale = new Vector2(1f, verticalStretch + 1f) * Scale * 0.1f;
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * brightness, Rotation, null, scale, SpriteEffects.None);
        }
    }
}
