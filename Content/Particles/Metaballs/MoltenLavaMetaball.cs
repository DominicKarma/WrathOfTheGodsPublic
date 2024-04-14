using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles.Metaballs
{
    public class MoltenLavaMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "NoxusBoss.BasicMetaballCircle.png";

        public override bool ShouldRender => ActiveParticleCount >= 1;

        public override Color EdgeColor => Color.Wheat;

        public override Func<Texture2D>[] LayerTextures => new Func<Texture2D>[]
        {
            () => ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/Pixel").Value
        };

        public override bool LayerIsFixedToScreen(int layerIndex) => true;

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 4f;

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity.X *= 0.981f;
            particle.Velocity.Y += particle.ExtraInfo[0];
            if (Collision.SolidCollision(particle.Center, 1, 1))
                particle.Velocity.Y = 0f;

            particle.Size *= 0.8f;
        }
    }
}
