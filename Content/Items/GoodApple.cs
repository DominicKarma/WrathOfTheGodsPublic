using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Content.Items
{
    public class GoodApple : ModItem, IToastyQoLChecklistItemSupport
    {
        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> BittenTexture
        {
            get;
            private set;
        }

        public const string TotalApplesConsumedFieldName = "TotalGoodApplesConsumed";

        // Acquired in the Eternal Garden (which is only accessible post-Noxus) but does not necessarily require Nameless to be defeated.
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public static readonly SoundStyle AppleBiteSound = new("NoxusBoss/Assets/Sounds/Item/AppleBite", new ReadOnlySpan<(int variant, float weight)>(new[]
        {
            (1, 1f),
            (2, 1f),
            (3, 0.005f) // Was that the bite of 87?
        }));

        // We have sinned.
        public static readonly SoundStyle AppleBiteSoundGFB = new SoundStyle("NoxusBoss/Assets/Sounds/Item/NamelessGoesInsaneOverGoodApple") with { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 20;

            if (Main.netMode != NetmodeID.Server)
            {
                MyTexture = ModContent.Request<Texture2D>(Texture);
                BittenTexture = ModContent.Request<Texture2D>($"{Texture}Bitten");
            }

            NoxusPlayer.MaxStatsEvent += ApplyHealthBoosts;
            NoxusPlayer.SaveDataEvent += SaveAppleCount;
            NoxusPlayer.LoadDataEvent += LoadAppleCount;
        }

        private void ApplyHealthBoosts(NoxusPlayer p, ref StatModifier health, ref StatModifier mana)
        {
            health.Base += p.GetValueRef<int>(TotalApplesConsumedFieldName);
        }

        private void LoadAppleCount(NoxusPlayer p, TagCompound tag)
        {
            p.GetValueRef<int>(TotalApplesConsumedFieldName).Value = tag.GetInt(TotalApplesConsumedFieldName);
        }

        private void SaveAppleCount(NoxusPlayer p, TagCompound tag)
        {
            tag[TotalApplesConsumedFieldName] = p.GetValueRef<int>(TotalApplesConsumedFieldName).Value;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 99999;
            Item.consumable = true;
            Item.DefaultToFood(22, 22, 0, 0, false, 15);
            Item.width = 22;
            Item.height = 22;
            Item.UseSound = Main.zenithWorld ? AppleBiteSoundGFB : AppleBiteSound;
            Item.UseCalamityRedRarity();
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && player.itemTime == 0)
            {
                player.UseHealthMaxIncreasingItem(1);
                player.GetValueRef<int>(TotalApplesConsumedFieldName).Value++;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Rewrite tooltips post-Nameless Deity.
            if (WorldSaveSystem.HasDefeatedNamelessDeity)
            {
                // Remove the default tooltips.
                tooltips.RemoveAll(t => t.Name.Contains("Tooltip"));

                // Generate and use custom tooltips.
                string specialTooltip = this.GetLocalizedValue("TooltipPostNamelessDeity");
                TooltipLine[] tooltipLines = specialTooltip.Split('\n').Select((t, index) =>
                {
                    return new TooltipLine(Mod, $"NamelessDeityTooltip{index + 1}", t);
                }).ToArray();

                // Color the last tooltip line.
                tooltipLines.Last().OverrideColor = DialogColorRegistry.NamelessDeityTextColor;
                tooltips.AddRange(tooltipLines);
                return;
            }

            // Make the final tooltip line about needing to pass the test use Nameless' dialog.
            tooltips.FirstOrDefault(t => t.Name == "Tooltip1").OverrideColor = DialogColorRegistry.NamelessDeityTextColor;
        }

        public override bool CanUseItem(Player player)
        {
            // Prevent the consumption of the apples until the player has maxed out all vanilla max life boosters.
            if (player.ConsumedLifeCrystals < Player.LifeCrystalMax || player.ConsumedLifeFruit < Player.LifeFruitMax)
                return false;

            // Prevent the consumption of the apples until to the player has defeated Nameless.
            return WorldSaveSystem.HasDefeatedNamelessDeity;
        }
    }
}
