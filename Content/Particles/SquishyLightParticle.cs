using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class SquishyLightParticle : Particle
    {
        public float SquishStrength;

        public float MaxSquish;

        public float HueShift;

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.SquishyLightParticle.png";

        public SquishyLightParticle(Vector2 position, Vector2 velocity, float scale, Color color, int lifetime, float opacity = 1f, float squishStrength = 1f, float maxSquish = 3f, float hueShift = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = Vector2.One * scale;
            DrawColor = color;
            Opacity = opacity;
            Rotation = 0f;
            Lifetime = lifetime;
            SquishStrength = squishStrength;
            MaxSquish = maxSquish;
            HueShift = hueShift;
        }

        public override void Update()
        {
            Velocity *= (LifetimeRatio >= 0.34f) ? 0.93f : 1.02f;
            Opacity = LifetimeRatio > 0.5f ? (Convert01To010(LifetimeRatio) * 0.2f + 0.8f) : Convert01To010(LifetimeRatio);
            Scale *= 0.95f;
            DrawColor = Main.hslToRgb(Main.rgbToHsl(DrawColor).X + HueShift, Main.rgbToHsl(DrawColor).Y, Main.rgbToHsl(DrawColor).Z, byte.MaxValue);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float squish = Clamp(Velocity.Length() / 10f * SquishStrength, 1f, MaxSquish);
            float rotation = Velocity.ToRotation() + PiOver2;
            Vector2 scale = new Vector2(1f - 1f * squish * 0.3f, squish) * Scale;
            Vector2 drawPosition = Position - Main.screenPosition;
            spriteBatch.Draw(BloomCircleSmall, drawPosition, null, DrawColor * Opacity * 0.8f, rotation, BloomCircleSmall.Size() * 0.5f, scale * 2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor * Opacity * 0.8f, rotation, null, scale * 1.1f, SpriteEffects.None);
            spriteBatch.Draw(Texture, drawPosition, null, Color.White * Opacity * 0.9f, rotation, null, scale, SpriteEffects.None);
        }
    }
}
