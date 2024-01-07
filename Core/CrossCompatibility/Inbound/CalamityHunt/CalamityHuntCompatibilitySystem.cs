using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class CalamityHuntCompatibilitySystem : ModSystem
    {
        internal delegate Vector3[] ColorArrayDelegate();

        internal delegate Vector3[] ColorArrayHook(ColorArrayDelegate orig);

        public override void PostSetupContent()
        {
            // Don't load anything if the calamity hunt mod is not enabled.
            if (CalamityHunt is null)
                return;

            AddAppleShimmer();
            AddCustomGoozmaColor();
        }

        internal static void AddAppleShimmer()
        {
            // Make the good and bad apple interchangeable in shimmer.
            int badAppleID = CalamityHunt.Find<ModItem>("BadApple").Type;
            int goodAppleID = ModContent.ItemType<GoodApple>();
            ItemID.Sets.ShimmerTransformToItem[badAppleID] = goodAppleID;
            ItemID.Sets.ShimmerTransformToItem[goodAppleID] = badAppleID;
        }

        internal static void AddCustomGoozmaColor()
        {
            Type slimeUtilsType = CalamityHunt.Code.GetType("CalamityHunt.Content.Bosses.Goozma.SlimeUtils");
            MethodInfo goozmaColorsGetter = slimeUtilsType?.GetMethod("get_GoozColorsVector3") ?? null;

            if (goozmaColorsGetter is not null)
                MonoModHooks.Add(goozmaColorsGetter, ChangeGoozmaColors);
        }

        internal static Vector3[] ChangeGoozmaColors(ColorArrayDelegate orig)
        {
            // Give Goozma the Good Apple palette after Nameless has been defeated.
            if (WorldSaveSystem.HasDefeatedNamelessDeity)
            {
                float universalHueShift = Sin01(Main.GlobalTimeWrappedHourly * 10f) * -0.08f;
                return new Vector3[10]
                {
                    new Color(0, 0, 0).HueShift(universalHueShift).ToVector3(),
                    new Color(79, 0, 39).HueShift(universalHueShift).ToVector3(),
                    new Color(114, 16, 50).HueShift(universalHueShift).ToVector3(),
                    new Color(158, 28, 71).HueShift(universalHueShift).ToVector3(),
                    new Color(217, 107, 154).HueShift(universalHueShift - 0.1f).ToVector3(),
                    new Color(214, 167, 178).HueShift(universalHueShift).ToVector3(),
                    new Color(251, 214, 157).HueShift(universalHueShift + 0.15f).ToVector3(),
                    new Color(114, 16, 50).HueShift(universalHueShift).ToVector3(),
                    new Color(0, 0, 0).HueShift(universalHueShift).ToVector3(),
                    new Color(0, 0, 0).HueShift(universalHueShift).ToVector3(),
                };
            }

            return orig();
        }
    }
}
