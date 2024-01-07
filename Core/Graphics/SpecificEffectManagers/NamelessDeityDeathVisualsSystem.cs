using System;
using MonoMod.Cil;
using NoxusBoss.Common.Easings;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class NamelessDeityDeathVisualsSystem : ModSystem
    {
        public int DeathTimerOverride
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            IL_Main.DrawInterface_35_YouDied += ChangeNamelessDeityText;
        }

        private void ChangeNamelessDeityText(ILContext il)
        {
            ILCursor cursor = new(il);

            // Make the text offset higher up if Nameless killed the player so that the player can better see the death vfx.
            cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));
            cursor.EmitDelegate<Func<float, float>>(textOffset =>
            {
                if (Main.LocalPlayer.GetModPlayer<NamelessDeityPlayerDeathVisualsPlayer>().WasKilledByNamelessDeity)
                    textOffset -= 120f;

                return textOffset;
            });

            // Replace the "You were slain..." text with something special.
            cursor.GotoNext(i => i.MatchLdsfld<Lang>("inter"));
            cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (Main.LocalPlayer.GetModPlayer<NamelessDeityPlayerDeathVisualsPlayer>().WasKilledByNamelessDeity)
                    return Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityPlayerDeathText");

                return originalText;
            });

            // Replace the number text.
            cursor.GotoNext(i => i.MatchLdstr("Game.RespawnInSuffix"));
            cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(Language), "GetTextValue"));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                var modPlayer = Main.LocalPlayer.GetModPlayer<NamelessDeityPlayerDeathVisualsPlayer>();
                if (modPlayer.WasKilledByNamelessDeity)
                {
                    float deathTimerInterpolant = modPlayer.DeathTimerOverride / (float)NamelessDeityPlayerDeathVisualsPlayer.DeathTimerMax;
                    ulong start = 5;
                    ulong end = int.MaxValue * 2uL;
                    float smoothInterpolant = new PolynomialEasing(20f).Evaluate(EasingType.InOut, deathTimerInterpolant);
                    long textValue = (long)Lerp(start, end, smoothInterpolant);
                    if (textValue >= int.MaxValue)
                        textValue -= int.MaxValue * 2L + 2;

                    return textValue.ToString();
                }

                return originalText;
            });
        }
    }
}
