using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class TwinkleParticle : Particle
    {
        public readonly struct LockOnDetails
        {
            public readonly Func<Vector2> LockOnCenter;

            public readonly Vector2 LockOnOffset;

            public void Apply(ref Vector2 position)
            {
                if (LockOnCenter is null)
                    return;

                position = LockOnCenter() + LockOnOffset;
            }

            public LockOnDetails(Vector2 lockOnOffset, Func<Vector2> lockOnCenter)
            {
                LockOnOffset = lockOnOffset;
                LockOnCenter = lockOnCenter;
            }
        }

        public LockOnDetails LockOnThing;

        public int TotalStarPoints;

        public Color BackglowBloomColor;

        public Vector2 ScaleFactor;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/VerticalLightStreak";

        public TwinkleParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, int totalStarPoints, Vector2 scaleFactor, Color backglowBloomColor = default, LockOnDetails lockOnDetails = default)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            ScaleFactor = scaleFactor;
            TotalStarPoints = totalStarPoints;
            Lifetime = lifetime;
            BackglowBloomColor = backglowBloomColor;
            LockOnThing = lockOnDetails;
        }

        public override void Update()
        {
            Opacity = InverseLerpBump(0f, 10f, 16f, Lifetime, Time);
            LockOnThing.Apply(ref Position);

            Velocity *= 0.94f;
        }

        public override void Draw()
        {
            Vector2 scale = ScaleFactor * Opacity * 0.1f;
            scale *= Sin(Main.GlobalTimeWrappedHourly * 30f + Time * 0.08f) * 0.125f + 1f;
            DrawTwinkle(Texture, Position - Main.screenPosition, TotalStarPoints, Rotation, BackglowBloomColor * Opacity, Color * Opacity, scale);
        }

        public static void DrawTwinkle(Texture2D texture, Vector2 drawPosition, int totalStarPoints, float rotation, Color backglowBloomColor, Color color, Vector2 scale, float spokesExtendOffset = 0.6f)
        {
            int instanceCount = totalStarPoints / 2;

            // Draw the backglow.
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, backglowBloomColor * 0.83f, 0f, BloomCircleSmall.Size() * 0.5f, scale * 7.2f, 0, 0f);

            // Draw the bloom flare.
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, color, rotation - Main.GlobalTimeWrappedHourly * 0.9f, BloomFlare.Size() * 0.5f, scale * 0.42f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, color, rotation + Main.GlobalTimeWrappedHourly * 0.91f, BloomFlare.Size() * 0.5f, scale * 0.42f, 0, 0f);

            // Draw the points of the twinkle.
            for (int i = 0; i < instanceCount; i++)
            {
                float rotationOffset = Pi * i / instanceCount;
                Vector2 localScale = scale;

                if (rotationOffset != 0f)
                    localScale *= Pow(Sin(rotationOffset), 1.5f);

                for (float s = 1f; s > 0.3f; s -= 0.2f)
                    Main.spriteBatch.Draw(texture, drawPosition, null, color, rotationOffset, texture.Size() * 0.5f, new Vector2(1f - (spokesExtendOffset - 0.6f) * 0.4f, spokesExtendOffset) * localScale * s, 0, 0f);
            }
        }
    }
}
