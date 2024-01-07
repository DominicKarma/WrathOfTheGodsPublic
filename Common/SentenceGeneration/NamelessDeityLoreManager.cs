using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Common.SentenceGeneration
{
    public class NamelessDeityLoreManager : ModSystem
    {
        private static ulong textID
        {
            get
            {
                ulong result = 0uL;

                for (int i = 0; i < Main.LocalPlayer.name.Length; i++)
                {
                    char nameCharacter = Main.LocalPlayer.name[i];
                    unchecked
                    {
                        result += (ulong)nameCharacter << (i * 4);
                    }
                }

                return result;
            }
        }

        public static bool LookingAtNamelessDeityLoreItem
        {
            get;
            set;
        }

        public static float SeedTimer
        {
            get;
            private set;
        }

        public static int Seed1 => (int)(SeedTimer % 100000f);

        public static int Seed2 => Seed1 + 1;

        public static float SeedInterpolant => SeedTimer % 1f;

        // When enabled, lore is "personalized", with the Nameless Deity lore entry, varying based on the player's steam ID and only changing across long timespans.
        // When disabled, lore text slowly shifts and becomes something completely different if the player stops reading the text and then starts reading again later.
        public static bool UseTrollingMode
        {
            get;
            set;
        } = true;

        // There is an exceedingly rare chance for a given lore text line to be manually replaced with special text.
        // Text affected by this is colored separately from everything else.
        public const int EasterEggLineChance = 10000;

        public static string EasterEggLine => Language.GetTextValue("Mods.NoxusBoss.Items.LoreNamelessDeity.EasterEggSentence");

        public override void UpdateUI(GameTime gameTime)
        {
            // Lock the seeds in place if trolling mode is enabled.
            if (UseTrollingMode)
            {
                int daysSince95 = (int)(DateTime.Now - new DateTime(1517, 10, 31)).TotalDays;
                SeedTimer = (int)((textID >> 11) % 9920000uL + (ulong)(daysSince95 / 22 * 991 % 500 - 12318));
            }

            // Ensure that the seed timer cycles naturally otherwise.
            else
            {
                SeedTimer += LookingAtNamelessDeityLoreItem ? 0.003f : 1f;
                if (SeedTimer >= 2000f)
                    SeedTimer = 0f;
            }

            // If the lore item isn't being looked at, reset the seed interpolant to zero by removing the fractional part.
            // This way, if the player looks at the lore text again it won't be in the middle of blending between two dialog sets.
            if (!LookingAtNamelessDeityLoreItem)
                SeedTimer = (int)SeedTimer;

            // Reset the looking at Nameless Deity lore item bool for the next frame.
            LookingAtNamelessDeityLoreItem = false;
        }
    }
}
