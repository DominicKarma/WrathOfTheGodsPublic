using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Accessories.VanityEffects
{
    public class DeificTouch : ModItem, IToastyQoLChecklistItemSupport
    {
        public static bool UsingEffect => !Main.gameMenu && Main.LocalPlayer.GetValueRef<bool>("DeificTouch") && NamelessDeityBoss.Myself is null;

        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            NoxusPlayer.ResetEffectsEvent += ResetValue;
        }

        private void ResetValue(NoxusPlayer p)
        {
            p.GetValueRef<bool>("DeificTouch").Value = false;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.UseCalamityRedRarity();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                player.GetValueRef<bool>("DeificTouch").Value = true;
        }

        public override void UpdateVanity(Player player) => player.GetValueRef<bool>("DeificTouch").Value = true;
    }
}
