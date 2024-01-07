using System.Collections.Generic;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.PreFightForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.SummonItems
{
    public class Genesis : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostDraedonAndCal;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 34;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.UseVioletRarity();
            Item.value = 0;
        }

        public override bool CanUseItem(Player player) =>
            !NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>()) && !NPC.AnyNPCs(ModContent.NPCType<EntropicGod>()) && !NPC.AnyNPCs(ModContent.NPCType<NoxusEggCutscene>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int noxusID = player.altFunctionUse == 2 || !WorldSaveSystem.HasDefeatedNoxusEgg ? ModContent.NPCType<NoxusEgg>() : ModContent.NPCType<EntropicGod>();

                // If the player is not in multiplayer, spawn Noxus.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, noxusID);

                // If the player is in multiplayer, request a boss spawn.
                else
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: noxusID);
            }

            return true;
        }

        public override bool AltFunctionUse(Player player) => WorldSaveSystem.HasDefeatedNoxusEgg;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var toolip = tooltips.Find(l => l.Name == "Tooltip0");
            if (WorldSaveSystem.HasDefeatedNoxusEgg)
                toolip.Text = Language.GetTextValue($"Mods.{Mod.Name}.Items.{Name}.AlternateTooltip");
        }

        public override void AddRecipes()
        {
            ModTile forge = null;
            ModItem bar = null;
            bool calExists = ModLoader.TryGetMod("CalamityMod", out Mod cal);
            bool draedonForgeExists = cal?.TryFind("DraedonsForge", out forge) ?? false;
            bool shadowspecBarExists = cal?.TryFind("ShadowspecBar", out bar) ?? false;

            // Calamity Recipe, uses shadowspec bars and the Draedon's Forge.
            if (calExists && draedonForgeExists && shadowspecBarExists)
            {
                CreateRecipe(1).
                    AddTile(forge.Type).
                    AddIngredient(ItemID.StoneBlock, 50).
                    AddIngredient(bar.Type, 10).
                    Register();
            }

            // Vanilla Recipe, swaps the shadowspec with luminite and the forge with the Ancient Manipulator.
            else
            {
                CreateRecipe(1).
                    AddTile(TileID.LunarCraftingStation).
                    AddIngredient(ItemID.StoneBlock, 50).
                    AddIngredient(ItemID.LunarBar, 10).
                    Register();
            }
        }
    }
}
