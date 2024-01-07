using System;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.SummonItems
{
    public class Terminal : ModItem
    {
        // Disallow this item from being loaded if Calamity is disabled, since without Calamity boss rush does not exist.
        public override bool IsLoadingEnabled(Mod mod)
        {
            return ModLoader.TryGetMod("CalamityMod", out _);
        }

        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 70;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Blue;
        }

        public override bool? UseItem(Player player)
        {
            // Don't bother if this is somehow called when Calamity is not enabled.
            if (ModReferences.BaseCalamity is null)
                return true;

            Type bossRushType = ModReferences.BaseCalamity.Code.GetType("CalamityMod.Events.BossRushEvent");
            if (bossRushType is null)
                return true;

            if ((bool)bossRushType.GetField("BossRushActive").GetValue(null))
                bossRushType.GetMethod("End").Invoke(null, Array.Empty<object>());
            else
            {
                bossRushType.GetMethod("SyncStartTimer").Invoke(null, new object[]
                {
                    120
                });

                for (int doom = 0; doom < Main.maxNPCs; doom++)
                {
                    NPC n = Main.npc[doom];
                    if (!n.active)
                        continue;

                    // Will also correctly despawn EoW because none of his segments are boss flagged.
                    bool shouldDespawn = n.boss || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail;
                    if (shouldDespawn)
                    {
                        n.active = false;
                        n.netUpdate = true;
                    }
                }

                bossRushType.GetField("BossRushStage").SetValue(null, 0);
                bossRushType.GetField("BossRushActive").SetValue(null, true);
            }

            return true;
        }
    }
}
