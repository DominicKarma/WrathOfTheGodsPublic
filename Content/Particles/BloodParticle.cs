using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class BloodParticle : Particle
    {
        public Color InitialColor;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/Blood";

        public BloodParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color)
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
            Scale *= 0.98f;
            Velocity.X *= 0.97f;
            Velocity.Y = Clamp(Velocity.Y + 0.9f, -22f, 22f);
            Color = Color.Lerp(InitialColor, Color.Transparent, Pow(LifetimeRatio, 3f));
            Rotation = Velocity.ToRotation() + PiOver2;
        }

        public override void Draw()
        {
            float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(Velocity.Y), true) * 0.84f;
            float brightness = (float)Math.Pow((double)Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15);
            Vector2 scale = new Vector2(1f, verticalStretch + 1f) * Scale * 0.1f;
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * brightness, Rotation, Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
