using System;
using System.IO;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    // Yes, this exists exclusively for the purpose of making Noxus appear in the sky in interesting ways.
    // Yes, this uses 3D orbital math to achieve this effect.
    // Yes, this is probably excessive, but sometimes in life the detours are what make the journey.
    public class CelestialOrbitDetails : ModSystem
    {
        public static int DayCounter
        {
            get;
            set;
        }

        public static float DayCompletion
        {
            get;
            set;
        }

        public static float FractionalDayCounter => DayCounter + DayCompletion;

        public static float WorldSpecificAngularOffset
        {
            get
            {
                ulong seed = (ulong)Main.ActiveWorldFileData.Seed;
                return Lerp(-TwoPi * 200f, TwoPi * 200f, Utils.RandomFloat(ref seed));
            }
        }

        public static float PlanetOrbitalPeriodInDays => 8f;

        public static float NoxusOrbitalPeriodInDays => 2.333f;

        public static Vector3 PlanetOrbitPosition => Orbit((TwoPi * FractionalDayCounter + WorldSpecificAngularOffset) / PlanetOrbitalPeriodInDays, 1f, ToRadians(43.5f));

        public static Vector3 NoxusOrbitOffset => Orbit((TwoPi * FractionalDayCounter + WorldSpecificAngularOffset) / NoxusOrbitalPeriodInDays, 1f, ToRadians(8.1f));

        public static Vector3 NoxusOrbitPosition => PlanetOrbitPosition + NoxusOrbitOffset;

        public static float NoxusHorizontalOffset => Main.screenWidth * NoxusOrbitOffset.X * 0.5f + Main.screenWidth * 0.5f;

        public static float NoxusVerticalOffset => Main.screenHeight * (NoxusOrbitPosition.Y * 0.33f + 0.1f) + 216f;

        public static bool WouldEclipseHappenAtDay(float fractionalDayCounter, out float distanceBetweenNoxusAndSun)
        {
            // Temporarily set the day counter to the hypothetical value.
            int oldDayCounter = DayCounter;
            float oldDayCompletion = DayCompletion;
            DayCounter = (int)fractionalDayCounter;
            DayCompletion = fractionalDayCounter % 1f;

            // Calculate the proximity of Noxus to the sun.
            Vector2 noxusPosition = new Vector2(NoxusHorizontalOffset, NoxusVerticalOffset) + NoxusSkySceneSystem.PreviousSceneDetails.SceneLocalScreenPositionOffset;
            noxusPosition.Y += NoxusSkySceneSystem.PreviousSceneDetails.bgTopY;
            Vector2 sunPosition = NoxusSkySceneSystem.GetSunPosition(NoxusSkySceneSystem.PreviousSceneDetails, InverseLerp(0.1875f, 0.8125f, DayCompletion));
            distanceBetweenNoxusAndSun = sunPosition.Distance(noxusPosition);

            // Eclipses cannot happen at night or if Noxus is behind the planet. If the time happens to be at night, then it automatically is considered to have not happened.
            // 0.1875 = 4.5 / 24. 4.5 equates to four and a half hours past midnight.
            // 0.8125 = 19.5 / 24. 19.5 equates to seven and a half hours past noon.
            if (DayCompletion <= 0.1875f || DayCompletion >= 0.8125f || NoxusOrbitOffset.Z < 0f)
            {
                DayCounter = oldDayCounter;
                DayCompletion = oldDayCompletion;
                return false;
            }

            // Set the day counter back to the their original values.
            DayCounter = oldDayCounter;
            DayCompletion = oldDayCompletion;

            return distanceBetweenNoxusAndSun <= 82f;
        }

        public override void PreUpdateWorld()
        {
            // Increment the day counter at 12:00AM.
            // This is used instead of, say, 4:30AM because Terraria has a convenient method for getting the fractional hours in a day, but since it's
            // relative to midnight it's easiest to also follow suit and not have to make manual corrections to use it.
            if (!Main.dayTime && (int)Math.Round(Main.time) == 16190)
            {
                DayCounter++;
                NetMessage.SendData(MessageID.WorldData);
            }
            DayCompletion = Utils.GetDayTimeAs24FloatStartingFromMidnight() / 24f;
        }

        public override void OnWorldLoad() => DayCounter = 0;

        public override void OnWorldUnload() => DayCounter = 0;

        public override void SaveWorldData(TagCompound tag)
        {
            tag["DayCounter"] = DayCounter;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            DayCounter = tag.GetInt("DayCounter");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(DayCounter);
        }

        public override void NetReceive(BinaryReader reader)
        {
            DayCounter = reader.ReadInt32();
        }

        public static Vector3 Orbit(float angle, float radius, float tilt)
        {
            angle = WrapAngle(angle);

            float x = Cos(angle) * radius;
            float z = Sin(angle);
            float y = Tan(tilt) * z;
            return new(x, y, z);
        }
    }
}
