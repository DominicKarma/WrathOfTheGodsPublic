﻿using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static bool miracleBlightWarningDisplayed;

        /// <summary>
        /// Makes an <see cref="NPC"/> immune to miracle blight, since it's notorious for creating odd visuals over the bosses.
        /// </summary>
        /// <param name="npc">The NPC to apply the immunity to.</param>
        public static void MakeImmuneToMiracleblight(this NPC npc)
        {
            // Don't do anything if Calamity is not enabled.
            if (ModReferences.BaseCalamity is null)
                return;

            // If miracle-blight could not be found, stop here and make a note in the logs that it couldn't be found.
            if (!ModReferences.BaseCalamity.TryFind("MiracleBlight", out ModBuff miracleBlight))
            {
                if (!miracleBlightWarningDisplayed)
                {
                    NoxusBoss.Instance.Logger.Warn("Calamity is loaded, but its Miracle Blight debuff could not be found! Perhaps it was renamed?");
                    miracleBlightWarningDisplayed = true;
                }
                return;
            }

            // Apply the immunity.
            NPCID.Sets.SpecificDebuffImmunity[npc.type][miracleBlight.Type] = true;
        }

        /// <summary>
        /// Defines a given <see cref="NPC"/>'s HP based on the current difficulty mode.
        /// </summary>
        /// <param name="npc">The NPC to set the HP for.</param>
        /// <param name="normalModeHP">HP value for normal mode</param>
        /// <param name="expertModeHP">HP value for expert mode</param>
        /// <param name="revengeanceModeHP">HP value for revengeance mode</param>
        /// <param name="deathModeHP">Optional HP value for death mode.</param>
        public static void SetLifeMaxByMode(this NPC npc, int normalModeHP, int expertModeHP, int revengeanceModeHP, int? deathModeHP = null, int? gfbModeHP = null)
        {
            npc.lifeMax = normalModeHP;
            if (Main.expertMode)
                npc.lifeMax = expertModeHP;
            if (CommonCalamityVariables.RevengeanceModeActive)
                npc.lifeMax = revengeanceModeHP;
            if (deathModeHP.HasValue && CommonCalamityVariables.DeathModeActive)
                npc.lifeMax = deathModeHP.Value;
            if (gfbModeHP.HasValue && Main.zenithWorld)
                npc.lifeMax = gfbModeHP.Value;

            // Read from the HP config value.
            float HPBoost = GetFromCalamityConfig<float>("BossHealthBoost") * 0.01f;
            npc.lifeMax += (int)(npc.lifeMax * HPBoost);
        }

        /// <summary>
        /// Disables Calamity's special boss bar for a given <see cref="NPC"/>, such that it closes. This effect is temporarily and must be used every frame to sustain the close.
        /// </summary>
        /// <param name="npc">The NPC to change.</param>
        public static void MakeCalamityBossBarClose(this NPC npc)
        {
            if (ModReferences.BaseCalamity is null || Main.gameMenu)
                return;

            ModReferences.BaseCalamity.Call("SetShouldCloseBossHealthBar", npc, true);
        }

        /// <summary>
        /// Checks if any invasions or events, such as the eclipse, blood moon, private invasion, acid rain, etc. are ongoing.
        /// </summary>
        public static bool AnyInvasionsOrEvents()
        {
            // Check if there is an invasion ongoing, such as goblins or pirates.
            if (Main.invasionType > 0 && Main.invasionProgressNearInvasion)
                return true;

            // Check if the pillars are present.
            if (NPC.LunarApocalypseIsUp)
                return true;

            // Check if the Old One's Army is ongoing.
            if (DD2Event.Ongoing)
                return true;

            // Check if an eclipse or special moon is ongoing.
            if (Main.eclipse || Main.pumpkinMoon || Main.snowMoon || Main.bloodMoon)
                return true;

            // Check if the Acid Rain event is ongoing.
            if (CommonCalamityVariables.AcidRainIsOngoing)
                return true;

            return false;
        }

        /// <summary>
        /// Sets the DR for a given <see cref="NPC"/>, in accordance with Calamity's DR system.
        /// </summary>
        /// <param name="npc">The NPC to apply the effect to.</param>
        /// <param name="dr">The 0-1 DR fraction to apply to the NPC.</param>
        public static void SetDR(this NPC npc, float dr)
        {
            ModReferences.BaseCalamity?.Call("SetDRSpecific", npc, dr);
        }

        /// <summary>
        /// Shorthand for allowing bosses to drop Omega Healing Potions. Intended to be used with the <see cref="ModNPC.BossLoot"/> hook.
        /// </summary>
        /// <param name="potionType">The ref int that stores the potion ID.</param>
        public static void SetOmegaPotionLoot(ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
            if (ModReferences.BaseCalamity?.TryFind("OmegaHealingPotion", out ModItem potion) ?? false)
                potionType = potion.Type;
        }

        public static Referenced<T> GetValueRef<T>(this NPC npc, string key) where T : struct =>
            npc.GetGlobalNPC<NoxusGlobalNPC>().valueRegistry.GetValueRef<T>(key);
    }
}
