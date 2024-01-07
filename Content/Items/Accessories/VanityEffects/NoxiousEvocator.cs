using NoxusBoss.Content.NPCs.Bosses.Noxus;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Accessories.VanityEffects
{
    public class NoxiousEvocator : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.UseVioletRarity();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                NoxusFumes.CreateIllusions(player);
        }

        public override void UpdateVanity(Player player) => NoxusFumes.CreateIllusions(player);
    }
}
