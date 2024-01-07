using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Pets
{
    public class OblivionRattle : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<BabyNoxus>();
            Item.buffType = ModContent.BuffType<BabyNoxusBuff>();
            Item.master = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 3600);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Find(t => t.Name == "Master").Text += " or Revengeance";
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // The item applies the buff, the buff spawns the projectile.
            player.AddBuff(Item.buffType, 2);
            return false;
        }
    }
}

