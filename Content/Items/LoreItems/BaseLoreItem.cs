using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.LoreItems
{
    public abstract class BaseLoreItem : ModItem
    {
        // Used for the purpose of creating custom lore tooltip colors. Overriding is not required.
        public virtual Color? LoreColor => null;

        // Used for automated crafting recipe loading.
        public abstract int TrophyID
        {
            get;
        }

        public const string TextKey = "NoxusBoss:Lore";

        public override void SetStaticDefaults()
        {
            // All lore items float in the air.
            ItemID.Sets.ItemNoGravity[Type] = true;

            // All lore items only require a single acquirement to duplicate in Journey Mode.
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = 0;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            // All lore items are stored in a separate item group.
            // Base Calamity uses a custom value of 12000 to represent said group, which is not a named variant in the ItemGroup enumeration, hence the explicit cast.
            itemGroup = (ContentSamples.CreativeHelper.ItemGroup)12000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine fullLore = new(Mod, TextKey, this.GetLocalizedValue("Lore"))
            {
                OverrideColor = LoreColor
            };

            // Override vanilla tooltips and display the lore tooltip instead.
            DrawHeldShiftTooltip(tooltips, new TooltipLine[]
            {
                fullLore
            }, true);
        }

        /// <summary>
        /// Generates special tooltip text for an item when they're holding the <see cref="Keys.LeftShift"/> button. Notably used for lore items.
        /// </summary>
        /// <param name="tooltips">The original tooltips.</param>
        /// <param name="holdShiftTooltips">The tooltips to display when holding shift.</param>
        /// <param name="hideNormalTooltip">Whether the original tooltips should be hidden when holding shift. Defaults to false.</param>
        public static void DrawHeldShiftTooltip(List<TooltipLine> tooltips, TooltipLine[] holdShiftTooltips, bool hideNormalTooltip = false)
        {
            // Do not override anything if the Left Shift key is not being held.
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                return;

            // Acquire base tooltip data.
            int firstTooltipIndex = -1;
            int lastTooltipIndex = -1;
            int standardTooltipCount = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    if (firstTooltipIndex == -1)
                    {
                        firstTooltipIndex = i;
                    }
                    lastTooltipIndex = i;
                    standardTooltipCount++;
                }
            }

            // Replace tooltips.
            if (firstTooltipIndex != -1)
            {
                if (hideNormalTooltip)
                {
                    tooltips.RemoveRange(firstTooltipIndex, standardTooltipCount);
                    lastTooltipIndex -= standardTooltipCount;
                }
                tooltips.InsertRange(lastTooltipIndex + 1, holdShiftTooltips);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile(TileID.Bookcases).
                AddIngredient(TrophyID).
                Register();
        }
    }
}
