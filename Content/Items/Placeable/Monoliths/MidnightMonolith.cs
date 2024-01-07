using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Tiles;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.Monoliths
{
    public class MidnightMonolith : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public const string HasMidnightMonolithAccessoryFieldName = "HasMidnightMonolithAccessory";

        public override void SetStaticDefaults()
        {
            NoxusPlayer.ResetEffectsEvent += ResetAccesoryValue;
        }

        private void ResetAccesoryValue(NoxusPlayer p)
        {
            p.GetValueRef<bool>(HasMidnightMonolithAccessoryFieldName).Value = false;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<MidnightMonolithTile>());
            Item.UseVioletRarity();
            Item.value = 0;
            Item.accessory = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetValueRef<bool>(HasMidnightMonolithAccessoryFieldName).Value = true;
            NoxusSky.InProximityOfMidnightMonolith = true;
            NoxusSky.TimeSinceCloseToMidnightMonolith = 0;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => UpdateVanity(player);
    }
}
