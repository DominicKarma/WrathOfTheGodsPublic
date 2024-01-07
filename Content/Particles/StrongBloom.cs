using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class StrongBloom : Particle
    {
        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => $"{GreyscaleTexturesPath}/BloomCircleSmall";

        public StrongBloom(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
        }

        public override void Update()
        {
            Opacity = Convert01To010(LifetimeRatio);
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }
    }
}
