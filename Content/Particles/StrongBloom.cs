using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class StrongBloom : Particle
    {
        public override BlendState BlendState => BlendState.Additive;

        public override string AtlasTextureName => "NoxusBoss.StrongBloom.png";

        public StrongBloom(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
        }

        public override void Update()
        {
            Opacity = Convert01To010(LifetimeRatio);
            Velocity *= 0.95f;
            Lighting.AddLight(Position, DrawColor.R / 255f, DrawColor.G / 255f, DrawColor.B / 255f);
        }
    }
}
