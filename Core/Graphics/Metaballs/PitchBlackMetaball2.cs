using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Metaballs
{
    public class PitchBlackMetaball2 : Metaball
    {
        public static readonly List<BasicMetaballParticle> GasParticles = new();

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeBlack;

        public override Color EdgeColor
        {
            get
            {
                Color c = Color.Lerp(Color.LightCoral, Color.MediumPurple, NamelessDeitySky.DifferentStarsInterpolant);
                if (NamelessDeityBoss.Myself is not null)
                    c = Color.Lerp(c, Color.Black, InverseLerp(1f, 4f, NamelessDeityBoss.Myself.As<NamelessDeityBoss>().ZPosition) * 0.5f);

                return c;
            }
        }

        public override bool AnythingToDraw => GasParticles.Any() || (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.Opacity >= 0.5f);

        public override List<Texture2D> Layers => new()
        {
            ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/PitchBlackLayer").Value
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
            foreach (BasicMetaballParticle particle in GasParticles)
            {
                particle.Velocity *= 0.99f;
                particle.Size *= 0.93f;
                particle.Center += particle.Velocity;
            }
            GasParticles.RemoveAll(p => p.Size <= 2f);
        }

        public override void DrawInstances()
        {
            Texture2D circle = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/BasicCircle").Value;
            foreach (BasicMetaballParticle particle in GasParticles)
                Main.spriteBatch.Draw(circle, particle.Center - Main.screenPosition, null, Color.White, 0f, circle.Size() * 0.5f, new Vector2(particle.Size) / circle.Size(), 0, 0f);
        }
    }
}
