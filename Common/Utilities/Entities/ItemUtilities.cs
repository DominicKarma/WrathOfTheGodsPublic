using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    // Convenient utilities that basically just allow for the safe setting of Calamity rarities and sell prices without a strong reference.
    public static partial class Utilities
    {
        public static readonly int RarityVioletBuyPrice = Item.buyPrice(1, 50, 0, 0);

        public static readonly int RarityCalamityRedBuyPrice = Item.buyPrice(2, 0, 0, 0);

        public static void UseVioletRarity(this Item item)
        {
            item.rare = ItemRarityID.Purple;
            item.value = RarityVioletBuyPrice;
            if (ModReferences.BaseCalamity?.TryFind("Violet", out ModRarity rarity) ?? false)
                item.rare = rarity.Type;
        }

        public static void UseCalamityRedRarity(this Item item)
        {
            item.rare = ItemRarityID.Purple;
            item.value = RarityCalamityRedBuyPrice;
            if (ModReferences.BaseCalamity?.TryFind("CalamityRed", out ModRarity rarity) ?? false)
                item.rare = rarity.Type;
        }
    }
}
