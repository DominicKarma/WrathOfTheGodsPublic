using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class MetalSparkParticle : Particle
    {
        public Color GlowColor;

        public new Vector2 Scale;

        public bool AffectedByGravity;

        public static Texture2D GlowTexture
        {
            get;
            private set;
        }

        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.MetalSparkParticle.png";

        public MetalSparkParticle(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, Vector2 scale, float opacity, Color color, Color glowColor)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Opacity = opacity;
            Lifetime = lifetime;
            DrawColor = color;
            GlowColor = glowColor;
        }

        public override void Update()
        {
            if (AffectedByGravity)
            {
                Velocity.X *= 0.9f;
                Velocity.Y += 1.1f;
            }
            Rotation = Velocity.ToRotation() + PiOver2;
            DrawColor = Color.Lerp(DrawColor, new(122, 108, 95), 0.06f);
            GlowColor *= 0.95f;

            Scale.X *= 0.98f;
            Scale.Y *= 0.95f;
        }
    }
}
