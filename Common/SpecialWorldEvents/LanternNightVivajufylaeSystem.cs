using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Critters.EternalGarden;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.SpecialWorldEvents
{
    public class LanternNightVivajufylaeSystem : ModSystem
    {
        public static bool ThumbsUpAlreadyHappenedTonight
        {
            get;
            set;
        }

        public override void PreUpdateEntities()
        {
            // Reset the thumbs up animation for the next night.
            if (Main.dayTime)
                ThumbsUpAlreadyHappenedTonight = false;

            // Attempt to summon the Vivajuyfylae.
            Player closestToSurface = Main.player[Player.FindClosest(new(Main.maxTilesX * 8f, 3000f), 1, 1)];
            bool atSurface = closestToSurface.ZoneForest;
            bool canSpawn = !ThumbsUpAlreadyHappenedTonight && NPC.downedMoonlord && !Main.dayTime && atSurface && LanternNight.LanternsUp && !NPC.AnyNPCs(ModContent.NPCType<Vivajuyfylae>());
            if (Main.netMode != NetmodeID.MultiplayerClient && canSpawn && Main.rand.NextBool(156000))
            {
                int vivajuyfylaeID = ModContent.NPCType<Vivajuyfylae>();
                for (int i = 0; i < 60; i++)
                {
                    Vector2 spawnPosition = closestToSurface.Center + new Vector2(Lerp(-750f, 750f, i / 59f), -600f) + Main.rand.NextVector2Circular(100f, 150f);
                    NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPosition.X, (int)spawnPosition.Y, vivajuyfylaeID, 0, 1f);
                }

                ThumbsUpAlreadyHappenedTonight = true;
            }
        }
    }
}
