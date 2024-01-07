using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Metaballs
{
    public class NamelessDeityDimensionMetaball : Metaball
    {
        public class GasParticle
        {
            public float Size;

            public Vector2 Velocity;

            public Vector2 Center;
        }

        public static readonly List<GasParticle> GasParticles = new();

        public override bool FixedToScreen => true;

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeNPCs;

        public override Color EdgeColor => Color.IndianRed;

        public override bool AnythingToDraw => GasParticles.Any();

        public override List<Texture2D> Layers => new()
        {
            Main.gameMenu ? TextureAssets.MagicPixel.Value : NamelessDeityDimensionSkyGenerator.NamelessDeityDimensionTarget
        };

        public static void CreateParticle(Vector2 spawnPosition, Vector2 velocity, float size)
        {
            GasParticles.Add(new()
            {
                Center = spawnPosition,
                Velocity = velocity,
                Size = size
            });
        }

        public override void Update()
        {
            foreach (GasParticle particle in GasParticles)
            {
                particle.Velocity *= 0.984f;
                particle.Size *= 0.89f;
                particle.Center += particle.Velocity;
            }
            GasParticles.RemoveAll(p => p.Size <= 2f);
        }

        public override void DrawInstances()
        {
            Texture2D circle = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/BasicCircle").Value;
            foreach (GasParticle particle in GasParticles)
                Main.spriteBatch.Draw(circle, particle.Center - Main.screenPosition, null, Color.White, 0f, circle.Size() * 0.5f, new Vector2(particle.Size) / circle.Size(), 0, 0f);
        }
    }
}
