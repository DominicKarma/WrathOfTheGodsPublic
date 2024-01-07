using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.InfiniteStairways;
using SubworldLibrary;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Content.Items.Debugging
{
    public class StaircaseStarter : DebugItem
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
            if (Main.myPlayer != player.whoAmI || player.itemAnimation != player.itemAnimationMax - 1)
                return false;

            if (Main.dayTime)
            {
                Main.NewText("This item can only be used during night time.");
                return false;
            }
            if (!Main.LocalPlayer.ZoneOverworldHeight)
            {
                Main.NewText("This item can only be used during on the surface.");
                return false;
            }

            if (!NoxusEggCutsceneSystem.NoxusCanCommitSkydivingFromSpace)
            {
                Main.NewText("This item can only be used post-ML.");
                return false;
            }
            if (!WorldSaveSystem.HasDefeatedNoxusEgg || WorldSaveSystem.HasDefeatedNoxus)
            {
                Main.NewText("Noxus has been defeated. In typical circumstances, this event would not happen.");
            }

            if (SubworldSystem.AnyActive())
            {
                Main.NewText("This item cannot be used in subworlds.");
                return false;
            }

            if (AnyBosses() || AnyInvasionsOrEvents())
            {
                Main.NewText("This item cannot be used if an event/invasion is ongoing or a boss is present.");
                return false;
            }

            float x = player.Center.X;
            float y = player.Center.Y;
            if (x <= 5000f || x >= Main.maxTilesX * 16f - 5000f)
            {
                Main.NewText("This item cannot be used if you're close to the world edge.");
                return false;
            }
            if (y <= 3600f)
            {
                Main.NewText("This item cannot be used if you're close to the world top.");
                return false;
            }

            if (!NamelessDeityInfiniteStairwayManager.HasEnteredEmptySpace)
            {
                Main.NewText("This item cannot be used if you're not in open space.");
                return false;
            }

            if (Main.myPlayer == player.whoAmI)
                NamelessDeityInfiniteStairwayManager.Start(player.whoAmI);

            return null;
        }
    }
}
