using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles;
using NoxusBoss.Core;
using NoxusBoss.Core.GlobalItems;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.SummonItems
{
    public class FakeTerminus : ModItem
    {
        private static int realTerminusID;

        public static Asset<Texture2D> ClosedEyeTexture
        {
            get;
            private set;
        }

        public static bool Exists
        {
            get;
            private set;
        }

        public static int TerminusID
        {
            get
            {
                if (Exists)
                    return ModContent.ItemType<FakeTerminus>();

                return realTerminusID;
            }
        }

        public override string Texture => "NoxusBoss/Content/Items/SummonItems/Terminus";

        public override bool IsLoadingEnabled(Mod mod)
        {
            // Determine if the fake Terminus should exist.
            // It does not exists if Calamity is loaded and contains the Terminus, but serves as a backup otherwise.
            Exists = !ModLoader.TryGetMod("CalamityMod", out Mod cal);
            if (cal?.TryFind("Terminus", out ModItem terminus) ?? false)
                realTerminusID = terminus.Type;
            else
                Exists = true;

            // Regardless of whether this item exists, however, apply Terminus functionality alterations.
            // This is done in a GlobalItem class, and will affect either this fake Terminus or the real one under the same terms, whichever exists.
            // The reason this loading is done in this hook is because it's not guaranteed that this fake item will have a chance to load, and if it doesn't then
            // the traditional loading hooks (such as SetStaticDefaults) are unreliable.
            NoxusGlobalItem.SetDefaultsEvent += ChangeTerminusProjectileSpawnType;
            NoxusGlobalItem.ModifyTooltipsEvent += ChangeTerminusTooltipDialog;
            NoxusGlobalItem.PreDrawInInventoryEvent += UseUnopenedEyeForm_Inventory;
            NoxusGlobalItem.PreDrawInWorldEvent += UseUnopenedEyeForm_World;
            NoxusGlobalItem.CanUseItemEvent += ModifyTerminusUseConditions;
            return Exists;
        }

        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

        private void ChangeTerminusProjectileSpawnType(Item item)
        {
            // Replace Terminus' projectile with a custom one that has nothing to do with Boss Rush.
            if (item.type == TerminusID)
            {
                item.shoot = ModContent.ProjectileType<TerminusProj>();
                item.channel = false;
            }
        }

        private void ChangeTerminusTooltipDialog(Item item, List<TooltipLine> tooltips)
        {
            // Alter the Terminus' tooltip text.
            if (item.type == TerminusID && !Main.zenithWorld)
            {
                EditTooltipByNum(0, item, tooltips, line => line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.FakeTerminus.BaseTooltip"));
                EditTooltipByNum(1, item, tooltips, line =>
                {
                    if (WorldSaveSystem.HasDefeatedNoxus)
                    {
                        line.OverrideColor = new(240, 76, 76);
                        line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.FakeTerminus.OpenedTooltip");
                    }
                    else
                    {
                        line.OverrideColor = new(239, 174, 174);
                        line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.FakeTerminus.UnopenedTooltip");
                    }
                });
            }
        }

        private bool UseUnopenedEyeForm_Inventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Let Terminus use its closed eye form if Noxus is not yet defeated.
            if (item.type == TerminusID && !WorldSaveSystem.HasDefeatedNoxus && !Main.zenithWorld)
            {
                ClosedEyeTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Items/SummonItems/TerminusClosedEye");
                spriteBatch.Draw(ClosedEyeTexture.Value, position, null, Color.White, 0f, origin, scale, 0, 0);
                return false;
            }
            return true;
        }

        private bool UseUnopenedEyeForm_World(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // Let Terminus use its closed eye form if Noxus is not yet defeated.
            if (item.type == TerminusID && !WorldSaveSystem.HasDefeatedNoxus && !Main.zenithWorld)
            {
                ClosedEyeTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Items/SummonItems/TerminusClosedEye");
                spriteBatch.Draw(ClosedEyeTexture.Value, item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0);
                return false;
            }
            return true;
        }

        private bool ModifyTerminusUseConditions(Item item, Player player)
        {
            // Make the Terminus only usable after Noxus. Also disallow it being usable to create multiple instances of Terminus in the world. That'd be weird.
            if (item.type == TerminusID)
                return WorldSaveSystem.HasDefeatedNoxus && player.ownedProjectileCounts[ModContent.ProjectileType<TerminusProj>()] <= 0;

            return true;
        }

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
    }
}
