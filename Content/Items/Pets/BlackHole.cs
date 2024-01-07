using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Pets
{
    public class BlackHole : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<BlackHolePet>();
            Item.buffType = ModContent.BuffType<BlackHolePetbuff>();
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

        private static void DrawBlackHole(Vector2 drawPosition)
        {
            // Draw the black hole.
            ManagedShader lensingShader = ShaderManager.GetShader("GravitationalLensingShader");
            lensingShader.ResetCache();
            GravitationalLensingShaderData.DoGenericShaderPreparations(lensingShader.Shader.Value);
            lensingShader.TrySetParameter("uColor", Color.Wheat);
            lensingShader.TrySetParameter("uSecondaryColor", Color.LightGoldenrodYellow);
            lensingShader.TrySetParameter("aspectRatioCorrectionFactor", Vector2.One);
            lensingShader.TrySetParameter("sourcePosition", Vector2.One * 0.5f);
            lensingShader.TrySetParameter("blackRadius", 0.068f);
            lensingShader.TrySetParameter("distortionStrength", 1f);
            lensingShader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
            lensingShader.Apply();

            Main.spriteBatch.Draw(InvisiblePixel, drawPosition, null, Color.Transparent, 0f, InvisiblePixel.Size() * 0.5f, 200f, 0, 0f);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);

            DrawBlackHole(position);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Main.spriteBatch.PrepareForShaders();

            DrawBlackHole(Item.position - Main.screenPosition);

            Main.spriteBatch.ResetToDefault();

            return false;
        }
    }
}

