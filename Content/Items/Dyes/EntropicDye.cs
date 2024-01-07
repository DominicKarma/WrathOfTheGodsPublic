using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Dyes
{
    public class EntropicDye : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public static readonly Color BaseShaderColor = new(72, 48, 122);

        public static int DyeID
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;

            if (!Main.dedServ)
            {
                Effect shader = ModContent.Request<Effect>("NoxusBoss/Assets/Effects/Dyes/EntropicDyeShader", AssetRequestMode.ImmediateLoad).Value;
                Asset<Texture2D> dyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Items/Dyes/EntropicDyeTexture", AssetRequestMode.ImmediateLoad);
                GameShaders.Armor.BindShader(Type, new ArmorShaderData(new Ref<Effect>(shader), ManagedShader.DefaultPassName)).UseImage(dyeTexture).UseColor(BaseShaderColor);
            }
        }

        public override void SetDefaults()
        {
            // Cache and restore the dye ID.
            // This is necessary because CloneDefaults will automatically reset the dye ID in accordance with whatever it's copied, when in reality the BindShader
            // call already determined what ID this dye should use.
            DyeID = Item.dye;
            Item.CloneDefaults(ItemID.AcidDye);
            Item.dye = DyeID;
            Item.UseVioletRarity();
            Item.value = Item.sellPrice(0, 12, 0, 0);
        }
    }
}
