using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class CartoonAngerParticle : Particle
    {
        public int RandomID
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

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Content/Particles/CartoonAngerParticle";

        public CartoonAngerParticle(Vector2 relativePosition, Color startingColor, Color endingColor, int lifetime, float rotation, float scale)
        {
            Position = relativePosition;
            Velocity = Vector2.Zero;
            StartingScale = scale;
            StartingColor = startingColor;
            EndingColor = endingColor;
            Scale = 0.01f;
            Rotation = rotation;
            Lifetime = lifetime;
            RandomID = Main.rand.Next(1000);
        }

        public override void Update()
        {
            float scaleFactor = Lerp(0.7f, 1.3f, Sin01(TwoPi * Time / 27f + RandomID));
            Scale = Remap(Time, 0f, 30f, 0.01f, StartingScale * scaleFactor);
            Color = Color.Lerp(StartingColor, EndingColor, LifetimeRatio);
            Color = Color.Lerp(Color, Color.Transparent, Pow(LifetimeRatio, 3.5f));
        }

        public override void Draw()
        {
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation, Texture.Size() * 0.5f, Scale, 0, 0f);
        }
    }
}
