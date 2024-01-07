using NoxusBoss.Content.Tiles;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.Monoliths
{
    public class DivineMonolith : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DivineMonolithTile>());
            Item.UseCalamityRedRarity();
            Item.value = 0;
            Item.accessory = true;
        }

        public override void UpdateVanity(Player player)
        {
            NamelessDeityDimensionSkyGenerator.InProximityOfDivineMonolith = true;
            NamelessDeityDimensionSkyGenerator.TimeSinceCloseToDivineMonolith = 0;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => UpdateVanity(player);
    }
}
