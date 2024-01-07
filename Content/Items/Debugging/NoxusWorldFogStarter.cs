using NoxusBoss.Common.SpecialWorldEvents;
using NoxusBoss.Content.CustomWorldSeeds;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Content.Items.Debugging
{
    public class NoxusWorldFogStarter : DebugItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == NetmodeID.MultiplayerClient || player.itemAnimation != player.itemAnimationMax - 1)
                return false;

            if (!NoxusWorldManager.Enabled)
            {
                Main.NewText($"This event can only be started on the Noxus world seed! If you've forgotten, the seed is 'darkness falls'");
                return null;
            }

            NoxusFogEventManager.Start();
            return null;
        }
    }
}
