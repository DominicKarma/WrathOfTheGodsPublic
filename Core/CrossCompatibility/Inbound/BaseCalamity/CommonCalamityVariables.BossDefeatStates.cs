using System.Reflection;
using Terraria.ModLoader.IO;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public static partial class CommonCalamityVariables
    {
        public static bool DesertScourgeDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "DesertScourge") && defeated;
            set => SetDownedValue("_downedDesertScourge", value);
        }

        public static bool CrabulonDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Crabulon") && defeated;
            set => SetDownedValue("_downedCrabulon", value);
        }

        public static bool HiveMindDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "HiveMind") && defeated;
            set => SetDownedValue("_downedHiveMind", value);
        }

        public static bool PerforatorHiveDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Perforator") && defeated;
            set => SetDownedValue("_downedPerforator", value);
        }

        public static bool SlimeGodDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "SlimeGod") && defeated;
            set => SetDownedValue("_downedSlimeGod", value);
        }

        public static bool CryogenDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Cryogen") && defeated;
            set => SetDownedValue("_downedCryogen", value);
        }

        public static bool AquaticScourgeDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "AquaticScourge") && defeated;
            set => SetDownedValue("_downedAquaticScourge", value);
        }

        public static bool BrimstoneElementalDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "BrimstoneElemental") && defeated;
            set => SetDownedValue("_downedBrimstoneElemental", value);
        }

        public static bool CalamitasCloneDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "CalamitasClone") && defeated;
            set => SetDownedValue("_downedCalamitasClone", value);
        }

        public static bool LeviathanDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "AnahitaLeviathan") && defeated;
            set => SetDownedValue("_downedLeviathan", value);
        }

        public static bool AstrumAureusDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "AstrumAureus") && defeated;
            set => SetDownedValue("_downedAstrumAureus", value);
        }

        // 2019
        public static bool PeanutButterGoliathDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "PlaguebringerGoliath") && defeated;
            set => SetDownedValue("_downedPlaguebringer", value);
        }

        public static bool RavagerDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Ravager") && defeated;
            set => SetDownedValue("_downedRavager", value);
        }

        public static bool AstrumDeusDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "AstrumDeus") && defeated;
            set => SetDownedValue("_downedAstrumDeus", value);
        }

        public static bool ProfanedGuardiansDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Guardians") && defeated;
            set => SetDownedValue("_downedGuardians", value);
        }

        public static bool DragonfollyDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Dragonfolly") && defeated;
            set => SetDownedValue("_downedDragonfolly", value);
        }

        public static bool ProvidenceDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Providence") && defeated;
            set => SetDownedValue("_downedProvidence", value);
        }

        public static bool CeaselessVoidDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "CeaselessVoid") && defeated;
            set => SetDownedValue("_downedCeaselessVoid", value);
        }

        public static bool StormWeaverDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "StormWeaver") && defeated;
            set => SetDownedValue("_downedStormWeaver", value);
        }

        public static bool SignusDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Signus") && defeated;
            set => SetDownedValue("_downedSignus", value);
        }

        public static bool PolterghastDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Polterghast") && defeated;
            set => SetDownedValue("_downedPolterghast", value);
        }

        public static bool OldDukeDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "OldDuke") && defeated;
            set => SetDownedValue("_downedBoomerDuke", value);
        }

        public static bool DevourerOfGodsDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "DevourerOfGods") && defeated;
            set => SetDownedValue("_downedDoG", value);
        }

        public static bool YharonDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "Yharon") && defeated;
            set => SetDownedValue("_downedYharon", value);
        }

        public static bool DraedonDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "ExoMechs") && defeated;
            set => SetDownedValue("_downedExoMechs", value);
        }

        public static bool CalamitasDefeated
        {
            get => TryGetFromModCall(out bool defeated, "BossDowned", "SupremeCalamitas") && defeated;
            set => SetDownedValue("_downedCalamitas", value);
        }

        private static void SetDownedValue(string fieldName, bool value)
        {
            BaseCalamity?.Code?.GetType("CalamityMod.DownedBossSystem")?.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
        }

        private static void SetWorldValue(string fieldName, bool value)
        {
            BaseCalamity?.Code?.GetType("CalamityMod.World.CalamityWorld")?.GetField(fieldName, BindingFlags.Public | BindingFlags.Static).SetValue(null, value);
        }

        public static void SaveDefeatStates(TagCompound tag)
        {
            if (DesertScourgeDefeated)
                tag[nameof(DesertScourgeDefeated)] = true;
            if (CrabulonDefeated)
                tag[nameof(CrabulonDefeated)] = true;
            if (HiveMindDefeated)
                tag[nameof(HiveMindDefeated)] = true;
            if (PerforatorHiveDefeated)
                tag[nameof(PerforatorHiveDefeated)] = true;
            if (SlimeGodDefeated)
                tag[nameof(SlimeGodDefeated)] = true;
            if (CryogenDefeated)
                tag[nameof(CryogenDefeated)] = true;
            if (AquaticScourgeDefeated)
                tag[nameof(AquaticScourgeDefeated)] = true;
            if (BrimstoneElementalDefeated)
                tag[nameof(BrimstoneElementalDefeated)] = true;
            if (CalamitasCloneDefeated)
                tag[nameof(CalamitasCloneDefeated)] = true;
            if (LeviathanDefeated)
                tag[nameof(LeviathanDefeated)] = true;
            if (AstrumAureusDefeated)
                tag[nameof(AstrumAureusDefeated)] = true;
            if (PeanutButterGoliathDefeated)
                tag[nameof(PeanutButterGoliathDefeated)] = true;
            if (RavagerDefeated)
                tag[nameof(RavagerDefeated)] = true;
            if (AstrumDeusDefeated)
                tag[nameof(AstrumDeusDefeated)] = true;
            if (ProfanedGuardiansDefeated)
                tag[nameof(ProfanedGuardiansDefeated)] = true;
            if (DragonfollyDefeated)
                tag[nameof(DragonfollyDefeated)] = true;
            if (ProvidenceDefeated)
                tag[nameof(ProvidenceDefeated)] = true;
            if (CeaselessVoidDefeated)
                tag[nameof(CeaselessVoidDefeated)] = true;
            if (StormWeaverDefeated)
                tag[nameof(StormWeaverDefeated)] = true;
            if (SignusDefeated)
                tag[nameof(SignusDefeated)] = true;
            if (PolterghastDefeated)
                tag[nameof(PolterghastDefeated)] = true;
            if (OldDukeDefeated)
                tag[nameof(OldDukeDefeated)] = true;
            if (DevourerOfGodsDefeated)
                tag[nameof(DevourerOfGodsDefeated)] = true;
            if (YharonDefeated)
                tag[nameof(YharonDefeated)] = true;
            if (DraedonDefeated)
                tag[nameof(DraedonDefeated)] = true;
            if (CalamitasDefeated)
                tag[nameof(CalamitasDefeated)] = true;
        }

        public static void LoadDefeatStates(TagCompound tag)
        {
            DesertScourgeDefeated = tag.ContainsKey(nameof(DesertScourgeDefeated));
            CrabulonDefeated = tag.ContainsKey(nameof(CrabulonDefeated));
            HiveMindDefeated = tag.ContainsKey(nameof(HiveMindDefeated));
            SlimeGodDefeated = tag.ContainsKey(nameof(SlimeGodDefeated));
            CryogenDefeated = tag.ContainsKey(nameof(CryogenDefeated));
            AquaticScourgeDefeated = tag.ContainsKey(nameof(AquaticScourgeDefeated));
            BrimstoneElementalDefeated = tag.ContainsKey(nameof(BrimstoneElementalDefeated));
            CalamitasCloneDefeated = tag.ContainsKey(nameof(CalamitasCloneDefeated));
            LeviathanDefeated = tag.ContainsKey(nameof(LeviathanDefeated));
            AstrumAureusDefeated = tag.ContainsKey(nameof(AstrumAureusDefeated));
            PeanutButterGoliathDefeated = tag.ContainsKey(nameof(PeanutButterGoliathDefeated));
            RavagerDefeated = tag.ContainsKey(nameof(RavagerDefeated));
            AstrumDeusDefeated = tag.ContainsKey(nameof(AstrumDeusDefeated));
            ProfanedGuardiansDefeated = tag.ContainsKey(nameof(ProfanedGuardiansDefeated));
            DragonfollyDefeated = tag.ContainsKey(nameof(DragonfollyDefeated));
            ProvidenceDefeated = tag.ContainsKey(nameof(ProvidenceDefeated));
            CeaselessVoidDefeated = tag.ContainsKey(nameof(CeaselessVoidDefeated));
            StormWeaverDefeated = tag.ContainsKey(nameof(StormWeaverDefeated));
            SignusDefeated = tag.ContainsKey(nameof(SignusDefeated));
            PolterghastDefeated = tag.ContainsKey(nameof(PolterghastDefeated));
            OldDukeDefeated = tag.ContainsKey(nameof(OldDukeDefeated));
            DevourerOfGodsDefeated = tag.ContainsKey(nameof(DevourerOfGodsDefeated));
            YharonDefeated = tag.ContainsKey(nameof(YharonDefeated));
            DraedonDefeated = tag.ContainsKey(nameof(DraedonDefeated));
            CalamitasDefeated = tag.ContainsKey(nameof(CalamitasDefeated));
        }
    }
}
