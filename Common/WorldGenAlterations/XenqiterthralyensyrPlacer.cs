using System.Collections.Generic;
using System.Linq;
using NoxusBoss.Content.Items.Placeable.Paintings;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace NoxusBoss.Common.WorldGenAlterations
{
    public class XenqiterthralyensyrPlacer : ModSystem
    {
        /// <summary>
        /// The amount of Xenqiterthralyensyr paintings to add to the world in chests.
        /// </summary>
        public const int TotalPaintingsPerWorld = 1;

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            tasks.Add(new PassLegacy("Hiding a forgotten artwork", AddXenqiterthralyensyrToChests));
        }

        public static void AddXenqiterthralyensyrToChests(GenerationProgress progress, GameConfiguration config)
        {
            // Attempt to place Xenqiterthralyensyr randomly into chests throughout the world.
            // This process ensures that the same chest doesn't get provided with the painting twice.
            List<int> accessedChests = new();
            for (int tries = 0; tries < 1000; tries++)
            {
                int chestIndex = WorldGen.genRand.Next(Main.maxChests);
                Chest c = Main.chest[chestIndex];

                // Ignore chests that have no items, are invalid, or were already accessed.
                bool invalidChest = c is null || (c.x == 0 && c.y == 0);
                if (invalidChest || !c.item.Any(i => !i.IsAir && i.stack >= 1) || accessedChests.Contains(chestIndex))
                    continue;

                // Place the painting in the chest.
                c.AddItemToShop(new Item(ModContent.ItemType<Xenqiterthralyensyr>()));
                accessedChests.Add(chestIndex);

                // Stop if the amount of chest placements has exceeded the intended limit.
                if (accessedChests.Count >= TotalPaintingsPerWorld)
                    break;
            }
        }
    }
}
