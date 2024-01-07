using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class NoxusEclipseCalculator : ModItem
    {
        public const int TotalDaysToCheck = 100;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.UseVioletRarity();
            Item.value = 0;
        }

        public override bool? UseItem(Player player)
        {
            float previousEclipseTime = -2f;

            // Check upcoming days for eclipses.
            float dayIncrement = 0.02f;
            for (float day = CelestialOrbitDetails.DayCounter; day <= CelestialOrbitDetails.DayCounter + TotalDaysToCheck; day += dayIncrement)
            {
                // If an eclipse was found and it's farther than one day away from the previous eclipse, that means it's new and should be marked down.
                // The reason the distance check is used because individual time steps near an eclipse each count as an eclipse, albeit with differing distance values.
                if (CelestialOrbitDetails.WouldEclipseHappenAtDay(day, out _) && Distance(day, previousEclipseTime) >= 1f && day > CelestialOrbitDetails.FractionalDayCounter)
                {
                    previousEclipseTime = day;

                    int daysUntilEclipse = (int)Math.Round(day - CelestialOrbitDetails.FractionalDayCounter);
                    float eclipseCloseness = GetBestProximityToSun(day);

                    GetEclipseDuration(day, out float startingTime, out float endingTime);

                    DayPercentageToTimeFormat(startingTime, out int startingHour, out int startingMinute, out bool startingInAM);
                    DayPercentageToTimeFormat(endingTime, out int endingHour, out int endingMinute, out bool endingInAM);

                    string startingTimeText = $"{startingHour}:{startingMinute:00} {(startingInAM ? "AM" : "PM")}";
                    string endingTimeText = $"{endingHour}:{endingMinute:00} {(endingInAM ? "AM" : "PM")}";
                    bool partialEclipse = eclipseCloseness >= 20f;
                    string eclipseText = Language.GetText($"Mods.{Mod.Name}.Items.{Name}.InfoText{(partialEclipse ? "Partial" : "Total")}").Format(daysUntilEclipse, startingTimeText, endingTimeText);
                    Color textColor = Color.White;
                    if (NoxusWorldManager.Enabled)
                    {
                        eclipseText = Language.GetTextValue($"Mods.{Mod.Name}.Items.{Name}.InfoTextNoxusWorld");
                        textColor = Color.DarkViolet;
                    }

                    Main.NewText(eclipseText, textColor);

                    // The text only needs to be said once in the Noxus world.
                    if (NoxusWorldManager.Enabled)
                        break;
                }
            }

            return true;
        }

        public static float GetBestProximityToSun(float day)
        {
            float closestDistance = 9999f;
            for (float d = -0.35f; d < 0.35f; d += 0.005f)
            {
                CelestialOrbitDetails.WouldEclipseHappenAtDay(day + d, out float distance);
                closestDistance = MathF.Min(closestDistance, distance);
            }

            return closestDistance;
        }

        public static void GetEclipseDuration(float day, out float startingTime, out float endingTime)
        {
            startingTime = day;
            endingTime = day;
            for (float d = 0f; d < 1f; d += 0.001f)
            {
                if (startingTime == day && !CelestialOrbitDetails.WouldEclipseHappenAtDay(day - d - 0.001f, out _))
                    startingTime = day - d;
                if (endingTime == day && !CelestialOrbitDetails.WouldEclipseHappenAtDay(day + d + 0.001f, out _))
                    endingTime = day + d;
            }
        }

        public static void DayPercentageToTimeFormat(float dayPercentage, out int hours, out int minutes, out bool am)
        {
            // Normalize the day percentage into a 24 hour range.
            dayPercentage = dayPercentage % 1f * 24f;

            hours = (int)dayPercentage;
            minutes = (int)(dayPercentage % 1f * 60f);
            am = hours <= 12;

            if (!am)
                hours -= 12;
            if (hours == 0)
                hours = 12;
        }
    }
}
