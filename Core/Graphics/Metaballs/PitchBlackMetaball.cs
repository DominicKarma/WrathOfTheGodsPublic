using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Projectiles.Pets;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Metaballs
{
    public class PitchBlackMetaball : Metaball
    {
        public static readonly List<BasicMetaballParticle> BasicMetaballParticles = new();

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeBlack;

        public override Color EdgeColor => Color.MediumPurple;

        public override bool AnythingToDraw => BasicMetaballParticles.Any() || EntropicGod.Myself is not null || AnyProjectiles(ModContent.ProjectileType<BabyNoxus>());

        public override List<Texture2D> Layers => new()
        {
            ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/PitchBlackLayer").Value
        };

        public static void CreateParticle(Vector2 spawnPosition, Vector2 velocity, float size)
        {
            BasicMetaballParticles.Add(new()
            {
                Center = spawnPosition,
                Velocity = velocity,
                Size = size
            });
        }

        public override void Update()
        {
            foreach (BasicMetaballParticle particle in BasicMetaballParticles)
            {
                particle.Velocity *= 0.99f;
                particle.Size *= 0.93f;
                particle.Center += particle.Velocity;
            }
            BasicMetaballParticles.RemoveAll(p => p.Size <= 2f);
        }

        public override void DrawInstances()
        {
            Texture2D circle = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/BasicCircle").Value;
            foreach (BasicMetaballParticle particle in BasicMetaballParticles)
                Main.spriteBatch.Draw(circle, particle.Center - Main.screenPosition, null, Color.White, 0f, circle.Size() * 0.5f, new Vector2(particle.Size) / circle.Size(), 0, 0f);

            foreach (NPC noxus in Main.npc.Where(n => n.active && n.type == ModContent.NPCType<EntropicGod>()))
            {
                Vector2 drawPosition = noxus.Center - Main.screenPosition;
                if (noxus.Opacity >= 0.8f)
                    noxus.As<EntropicGod>().DrawBack(drawPosition, noxus.GetAlpha(noxus.As<EntropicGod>().GeneralColor), noxus.rotation);
            }
            foreach (Projectile babyNoxus in Main.projectile.Where(p => p.active && p.type == ModContent.ProjectileType<BabyNoxus>()))
            {
                Vector2 drawPosition = babyNoxus.Center - Main.screenPosition;
                if (babyNoxus.Opacity >= 0.8f)
                    babyNoxus.As<BabyNoxus>().DrawBack();
            }
        }
    }
}
