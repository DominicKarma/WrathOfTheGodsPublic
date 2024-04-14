using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class GoodAppleTreeTopParticle : Particle
    {
        public int FallOverDirection;

        public bool HasHitGround;

        public override string AtlasTextureName => "NoxusBoss.GoodAppleTreeTopParticle.png";

        public GoodAppleTreeTopParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Lifetime = lifetime;
            Scale = Vector2.One * scale;
            FallOverDirection = 1;
            Rotation = FallOverDirection * 0.003f;
            Direction = FallOverDirection;
        }

        public override void Update()
        {
            // Fade out when close to death.
            Opacity = InverseLerp(Lifetime, Lifetime - 120f, Time);

            // Decide rotation.
            Rotation = Clamp(Rotation * 1.06f, -0.7f, 0.7f);

            // Move.
            if (Abs(Rotation) >= 0.7f)
            {
                Velocity.X *= 0.1f;
                Velocity.Y += 0.2f;
                Velocity = Collision.TileCollision(Position - Vector2.One * Scale * 11f, Velocity, (int)(Scale.X * 22f), (int)(Scale.Y * 100f));

                if (Abs(Velocity.Y) >= 0.0001f && !HasHitGround)
                {
                    HasHitGround = true;
                }
            }
            else
            {
                Velocity = Vector2.Lerp(Velocity, (Rotation - PiOver2).ToRotationVector2() * 8f, 0.32f);
                if (Velocity.Y < 0f)
                    Velocity.Y = 0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SpriteEffects visualDirection = Direction.ToSpriteDirection();
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, Texture.Frame.Size() * new Vector2(0.5f, 1f), Scale, visualDirection);
        }
    }
}
