using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class CheatPermissionSlip : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public static bool PlayerHasLegitimateSlip(Player p) => WorldSaveSystem.HasDefeatedNamelessDeity && p.HasItem(ModContent.ItemType<CheatPermissionSlip>());

        public static bool PlayerHasIllegitimateSlip(Player p) => !WorldSaveSystem.HasDefeatedNamelessDeity && p.HasItem(ModContent.ItemType<CheatPermissionSlip>());

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.UseCalamityRedRarity();
            Item.value = 0;
        }
    }
}
