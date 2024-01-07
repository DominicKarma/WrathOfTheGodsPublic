using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Pets
{
    public class Starseed : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<StarPet>();
            Item.buffType = ModContent.BuffType<StarPetBuff>();
            Item.useStyle = ItemUseStyleID.RaiseLamp;
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

        private static void DrawBackglow(Vector2 drawPosition)
        {
            // Draw a bloom flare behind everything.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.77f;
            Color bloomFlareColor1 = Color.LightGoldenrodYellow;
            Color bloomFlareColor2 = Color.Red;
            float flareScale = 0.06f;
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor1 * 0.7f, bloomFlareRotation, BloomFlare.Size() * 0.5f, flareScale, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor2 * 0.45f, -bloomFlareRotation, BloomFlare.Size() * 0.5f, flareScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, Color.Coral * 0.7f, 0f, BloomCircleSmall.Size() * 0.5f, flareScale * 8f, 0, 0f);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.UIScaleMatrix);

            DrawBackglow(position);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive);

            DrawBackglow(Item.position - Main.screenPosition + new Vector2(6f, 24f));

            Main.spriteBatch.ResetToDefault();

            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // The item applies the buff, the buff spawns the projectile.
            player.AddBuff(Item.buffType, 2);
            return false;
        }
    }
}

