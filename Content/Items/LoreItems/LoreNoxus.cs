using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.CalRemixCompatibilitySystem;

namespace NoxusBoss.Content.Items.LoreItems
{
    public class LoreNoxus : BaseLoreItem, IToastyQoLChecklistItemSupport
    {
        public override int TrophyID => ModContent.ItemType<NoxusTrophy>();

        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetStaticDefaults()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // Load (evil) fanny rants pertaining to this lore item.
                var lore1 = new FannyDialog("NoxusLoreText1", "Idle").WithDuration(4.5f).WithCondition(_ => FannyDialog.JustReadLoreItem(Type)).WithoutClickability();

                // All messages henceforth are evil and edgy.
                var lore2 = new FannyDialog("NoxusLoreText2", "EvilIdle").WithDuration(5f).WithEvilness().WithParentDialog(lore1, 0.5f);
                var lore3 = new FannyDialog("NoxusLoreText3", "EvilIdle").WithDuration(6f).WithEvilness().WithParentDialog(lore2, 0.5f);
                var lore4 = new FannyDialog("NoxusLoreText4", "EvilIdle").WithDuration(6f).WithEvilness().WithParentDialog(lore3, 0.5f);
                var lore5 = new FannyDialog("NoxusLoreText5", "EvilIdle").WithDuration(5f).WithEvilness().WithParentDialog(lore4, 0.5f);

                lore1.Register();
                lore2.Register();
                lore3.Register();
                lore4.Register();
                lore5.Register();
            }

            MakeCountAsLoreItem(Type);
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.UseVioletRarity();
            base.SetDefaults();
        }
    }
}
