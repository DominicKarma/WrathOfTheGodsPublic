using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.Graphics.SpecificEffectManagers.NoxusSprayPlayerDeletionSystem;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class NamelessDeityTipsOverrideSystem : ModSystem
    {
        public static bool UseDeathAnimationText
        {
            get;
            set;
        }

        public static bool UseSprayText
        {
            get;
            set;
        }

        public static readonly Regex PercentageExtractor = new(@"([0-9]+%)", RegexOptions.Compiled);

        public static string SprayDeletionTipsText => Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityNoxusSprayerTip");

        public static string DeathAnimationTipsText => Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityEndScreenMainMenuTip");

        public override void OnModLoad()
        {
            Terraria.GameContent.UI.IL_GameTipsDisplay.Draw += ChangeTipText;
            On_Main.DrawMenu += ChangeStatusText;
        }

        private void ChangeStatusText(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            // Handle screen shake visuals.
            MainMenuScreenShakeShaderData.ToggleActivityIfNecessary();

            if (UseSprayText)
            {
                Main.statusText = string.Empty;
                orig(self, gameTime);

                if (MainMenuReturnDelay >= 1)
                {
                    Main.menuMode = 10;
                    MainMenuReturnDelay++;

                    // Make the screen shake when Nameless says "NOT".
                    if (MainMenuReturnDelay == 44)
                        MainMenuScreenShakeShaderData.ScreenShakeIntensity = 12f;

                    if (MainMenuReturnDelay >= 330)
                    {
                        Main.menuMode = 0;
                        MainMenuReturnDelay = 0;
                    }
                }

                return;
            }

            if (UseDeathAnimationText)
            {
                string oldStatusText = Main.statusText;

                // Incorporate the percentage into the replacement text, if one was present previously.
                if (PercentageExtractor.IsMatch(oldStatusText))
                {
                    string percentage = PercentageExtractor.Match(oldStatusText).Value;
                    Main.statusText = $"{Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityStatusPercentageText")} {percentage}";
                }

                // Otherwise simply use the regular ominous text about having "passed the test".
                else
                    Main.statusText = Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityStatusText");

                Main.oldStatusText = Main.statusText;
            }
            orig(self, gameTime);
        }

        public override void PostUpdateEverything()
        {
            if (!Main.gameMenu)
            {
                UseDeathAnimationText = false;
                UseSprayText = false;
            }
        }

        private void ChangeTipText(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(Language), "get_ActiveCulture")))
                return;

            int textLocalIndex = 0;
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStloc(out textLocalIndex)))
                return;

            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (UseDeathAnimationText)
                    return DeathAnimationTipsText;
                else if (UseSprayText)
                    return SprayDeletionTipsText;
                return originalText;
            });

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Color>("get_White")))
                return;

            cursor.EmitDelegate<Func<Color, Color>>(originalColor =>
            {
                if (UseDeathAnimationText || UseSprayText)
                    return Color.IndianRed;
                return originalColor;
            });
        }
    }
}
