using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class SquishyLightParticle : Particle
    {
        public float SquishStrength;

        public float MaxSquish;

        public float HueShift;

        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => "NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/ParticleLight";

        public SquishyLightParticle(Vector2 position, Vector2 velocity, float scale, Color color, int lifetime, float opacity = 1f, float squishStrenght = 1f, float maxSquish = 3f, float hueShift = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Color = color;
            Opacity = opacity;
            Rotation = 0f;
            Lifetime = lifetime;
            SquishStrength = squishStrenght;
            MaxSquish = maxSquish;
            HueShift = hueShift;
        }

        public override void Update()
        {
            Velocity *= (LifetimeRatio >= 0.34f) ? 0.93f : 1.02f;
            Opacity = LifetimeRatio > 0.5f ? (Convert01To010(LifetimeRatio) * 0.2f + 0.8f) : Convert01To010(LifetimeRatio);
            Scale *= 0.95f;
            Color = Main.hslToRgb(Main.rgbToHsl(Color).X + HueShift, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z, byte.MaxValue);
        }

        public override void Draw()
        {
            float squish = Clamp(Velocity.Length() / 10f * SquishStrength, 1f, MaxSquish);
            float rotation = Velocity.ToRotation() + PiOver2;
            Vector2 origin = Texture.Size() / 2f;
            Vector2 scale = new(Scale - Scale * squish * 0.3f, Scale * squish);
            float properBloomSize = Texture.Height / (float)BloomCircleSmall.Height;
            Vector2 drawPosition = Position - Main.screenPosition;
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, Color * Opacity * 0.8f, rotation, BloomCircleSmall.Size() / 2f, scale * 2f * properBloomSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Texture, drawPosition, null, Color * Opacity * 0.8f, rotation, origin, scale * 1.1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Texture, drawPosition, null, Color.White * Opacity * 0.9f, rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
