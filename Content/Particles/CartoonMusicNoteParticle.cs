using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class CartoonMusicNoteParticle : Particle
    {
        public int FrameY
        {
            get;
            set;
        }

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

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.CartoonMusicNoteParticle.png";

        public CartoonMusicNoteParticle(Vector2 relativePosition, Vector2 velocity, Color startingColor, Color endingColor, int lifetime, float rotation, float scale, bool flyToRight)
        {
            Position = relativePosition;
            Velocity = velocity;
            StartingScale = scale;
            StartingColor = startingColor;
            EndingColor = endingColor;
            Scale = Vector2.One * 0.01f;
            Rotation = rotation;
            Lifetime = lifetime;
            FrameY = Main.rand.Next(FrameCount);
            FlyToRight = flyToRight;
            Direction = FlyToRight.ToDirectionInt();
        }

        public override void Update()
        {
            Scale = Vector2.One * Utils.Remap(Time, 0f, 15f, 0.01f, StartingScale);
            DrawColor = Color.Lerp(StartingColor, EndingColor, LifetimeRatio);
            Opacity = 1f - LifetimeRatio.Cubed();

            // Make the note gradually fly horizontally in a sinusoidal pattern.
            Vector2 sinusoidalVelocity = new(FlyToRight.ToDirectionInt() * 2.4f, Sin(Position.X * 0.08f) * 2.6f);
            Velocity = Vector2.Lerp(Velocity, sinusoidalVelocity, 0.065f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Rectangle frame = Texture.Frame.Subdivide(1, FrameCount, 0, FrameY);
            SpriteEffects visualDirection = Direction.ToSpriteDirection();

            // Draw the backglow.
            float backglowHue = (Main.rgbToHsl(DrawColor).X + 0.56f) % 1f;
            Color backglowColor = Main.hslToRgb(backglowHue, 0.85f, 0.6f) * 0.54f;
            spriteBatch.Draw(BloomCircleSmall, Position - Main.screenPosition, null, backglowColor * Opacity, Rotation, BloomCircleSmall.Size() * 0.5f, Scale, visualDirection, 0f);

            // Draw the note.
            spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, DrawColor * Opacity, Rotation, frame.Size() * 0.5f, Scale, visualDirection);
        }
    }
}
