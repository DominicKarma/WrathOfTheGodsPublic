using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles.Metaballs
{
    public class NoxusBloodMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "NoxusBoss.BasicMetaballCircle.png";

        public override Color EdgeColor => new(159, 151, 255);

        public override bool ShouldRender => ActiveParticleCount >= 1;

        public override bool LayerIsFixedToScreen(int layerIndex) => false;

        public override Func<Texture2D>[] LayerTextures => [() => ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/Metaballs/NoxusBloodLayer").Value];

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity.X *= 0.981f;
            particle.Velocity.Y += particle.ExtraInfo[0];
            if (Collision.SolidCollision(particle.Center, 1, 1))
                particle.Velocity.Y = 0f;

            particle.Size *= 0.96f;
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 4f;
    }
}
