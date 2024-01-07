using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class CartoonMusicNoteParticle : Particle
    {
        public float StartingScale
        {
            get;
            set;
        }

        public Color StartingColor
        {
            get;
            set;
        }

        public Color EndingColor
        {
            get;
            set;
        }

        public bool FlyToRight
        {
            get;
            set;
        }

        public override int FrameCount => 2;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/CartoonMusicNoteParticle";

        public CartoonMusicNoteParticle(Vector2 relativePosition, Vector2 velocity, Color startingColor, Color endingColor, int lifetime, float rotation, float scale, bool flyToRight)
        {
            Position = relativePosition;
            Velocity = velocity;
            StartingScale = scale;
            StartingColor = startingColor;
            EndingColor = endingColor;
            Scale = 0.01f;
            Rotation = rotation;
            Lifetime = lifetime;
            Frame = Main.rand.Next(FrameCount);
            FlyToRight = flyToRight;
            Direction = FlyToRight.ToDirectionInt();
        }

        public override void Update()
        {
            Scale = Remap(Time, 0f, 15f, 0.01f, StartingScale);
            Color = Color.Lerp(StartingColor, EndingColor, LifetimeRatio);
            Opacity = 1f - Pow(LifetimeRatio, 3f);

            // Make the note gradually fly horizontally in a sinusoidal pattern.
            Vector2 sinusoidalVelocity = new(FlyToRight.ToDirectionInt() * 2.4f, Sin(Position.X * 0.08f) * 2.6f);
            Velocity = Vector2.Lerp(Velocity, sinusoidalVelocity, 0.065f);
        }

        public override void Draw()
        {
            Rectangle frame = Texture.Frame(1, FrameCount, 0, Frame);
            SpriteEffects visualDirection = Direction.ToSpriteDirection();

            // Draw the backglow.
            float backglowHue = (Main.rgbToHsl(Color).X + 0.56f) % 1f;
            Color backglowColor = Main.hslToRgb(backglowHue, 0.85f, 0.6f) * 0.54f;
            Main.spriteBatch.Draw(BloomCircleSmall, Position - Main.screenPosition, null, backglowColor * Opacity, Rotation, BloomCircleSmall.Size() * 0.5f, Scale, visualDirection, 0f);

            // Draw the note.
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale, visualDirection, 0f);
        }
    }
}
