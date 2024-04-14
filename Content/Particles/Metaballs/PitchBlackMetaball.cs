using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Projectiles.Pets;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles.Metaballs
{
    public class PitchBlackMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "NoxusBoss.BasicMetaballCircle.png";

        public override Color EdgeColor => Color.MediumPurple;

        public override bool DrawnManually => true;

        // Default rendering is disabled.
        public override bool ShouldRender => ActiveParticleCount >= 1 || AnyProjectiles(ModContent.ProjectileType<DarkComet>());

        public override Func<Texture2D>[] LayerTextures => [() => ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/Metaballs/PitchBlackLayer").Value];

        public override void Load()
        {
            On_Main.DrawNPCs += FuckingDrawTheStupidMetaballs;
        }

        private void FuckingDrawTheStupidMetaballs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            orig(self, behindTiles);
        }

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Size *= 0.93f;
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 2f;

        public override void ExtraDrawing()
        {
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
