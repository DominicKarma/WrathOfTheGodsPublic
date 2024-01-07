using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Projectiles.Typeless;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.MiscOPTools
{
    public class ThePurifier : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public static readonly SoundStyle BuildupSound = new("NoxusBoss/Assets/Sounds/Item/PurifierBuildup");

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 16;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.UseCalamityRedRarity();
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<ThePurifierProj>();
            Item.shootSpeed = 8f;
            Item.maxStack = 9999;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Ensure that the tooltip is colored with a danger-indicating red.
            foreach (var tooltip in tooltips)
            {
                if (!tooltip.Name.Contains("Tooltip"))
                    continue;

                tooltip.OverrideColor = Color.Lerp(DialogColorRegistry.PurifierWarningTextColor, DialogColorRegistry.NamelessDeityTextColor, Cos01(Main.GlobalTimeWrappedHourly * 5f) * 0.32f);
            }
        }
    }
}
