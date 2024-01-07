using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class HeavySmokeParticle : Particle
    {
        public bool Glowing;

        public float HueShift;

        public float Spin;

        public override int FrameCount => 7;

        public override BlendState DrawBlendState => Glowing ? BlendState.Additive : BlendState.NonPremultiplied;

        public override string TexturePath => "NoxusBoss/Content/Particles/HeavySmokeParticle";

        public HeavySmokeParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float opacity, float rotationSpeed = 0f, bool glowing = false, float hueshift = 0f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Frame = Main.rand.Next(FrameCount);
            Lifetime = lifetime;
            Opacity = opacity;
            Spin = rotationSpeed;
            Glowing = glowing;
            HueShift = hueshift;
        }

        public override void Update()
        {
            if (LifetimeRatio < 0.1f)
                Scale += 0.01f;
            if (LifetimeRatio > 0.9f)
                Scale *= 0.975f;

            // Ensure the color's hue stays within natural boundaries while its hue shifts.
            Vector3 hueSaturationLightness = Main.rgbToHsl(Color);
            Color = Main.hslToRgb((hueSaturationLightness.X + HueShift) % 1f, hueSaturationLightness.Y, hueSaturationLightness.Z);

            // Fade out and spin.
            Opacity *= 0.98f;
            Rotation += Spin * Velocity.X.NonZeroSign();

            // Slow down.
            Velocity *= 0.98f;

            // Fade out as this particle begins to die.
            Color *= InverseLerp(1f, 0.85f, LifetimeRatio);
        }

        public override void Draw()
        {
            int timeFrame = (int)Math.Floor(Time / (Lifetime / 6f));
            Rectangle frame = new(Frame * 80, timeFrame * 80, 80, 80);
            SpriteEffects visualDirection = Direction.ToSpriteDirection();
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale, visualDirection, 0f);
        }
    }
}
