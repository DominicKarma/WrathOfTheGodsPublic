using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable
{
    public class Cattail : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public static readonly SoundStyle CelebrationSound = new SoundStyle("NoxusBoss/Assets/Sounds/Item/CattailPlacementCelebration") with { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 50;
            new ManagedILEdit("Play Special Sound for Cattail", edit =>
            {
                IL_Player.PlaceThing_Tiles_PlaceIt += edit.SubscriptionWrapper;
            }, PlayAwesomeSoundForCattail).Apply();
        }

        private void PlayAwesomeSoundForCattail(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt<Player>("PlaceThing_Tiles_PlaceIt_KillGrassForSolids")))
            {
                edit.LogFailure("The Player.PlaceThing_Tiles_PlaceIt_KillGrassForSolids call could not be found.");
                return;
            }

            cursor.Emit(OpCodes.Ldarg_3);
            cursor.EmitDelegate((int tileType) =>
            {
                if (tileType == TileID.Cattail && !WorldSaveSystem.HasPlacedCattail)
                {
                    SoundEngine.PlaySound(CelebrationSound);
                    CattailAnimationSystem.StartAnimation();
                    WorldSaveSystem.HasPlacedCattail = true;
                }
            });
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID.Cattail);
            Item.width = 16;
            Item.height = 10;
            Item.UseCalamityRedRarity();
            Item.value = Item.sellPrice(100, 0, 0, 0);
            Item.consumable = true;
        }
    }
}

