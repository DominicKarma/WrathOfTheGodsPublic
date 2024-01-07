using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusPlayer : ModPlayer
    {
        internal readonly ReferencedValueRegistry valueRegistry = new();

        public delegate void PlayerActionDelegate(NoxusPlayer p);

        public static event PlayerActionDelegate ResetEffectsEvent;

        public delegate void SaveLoadDataDelegate(NoxusPlayer p, TagCompound tag);

        public delegate void ColorChangeDelegate(NoxusPlayer p, ref Color drawColor);

        public delegate void MaxStatsDelegate(NoxusPlayer p, ref StatModifier health, ref StatModifier mana);

        public static event SaveLoadDataDelegate SaveDataEvent;

        public static event SaveLoadDataDelegate LoadDataEvent;

        public static event PlayerActionDelegate PostUpdateEvent;

        public static event ColorChangeDelegate GetAlphaEvent;

        public static event MaxStatsDelegate MaxStatsEvent;

        public override void Unload()
        {
            // Reset all events on mod unload.
            ResetEffectsEvent = null;
            SaveDataEvent = null;
            LoadDataEvent = null;
            PostUpdateEvent = null;
            GetAlphaEvent = null;
            MaxStatsEvent = null;
        }

        public override void SetStaticDefaults()
        {
            On_Player.GetImmuneAlpha += GetAlphaOverride;
            On_Player.GetImmuneAlphaPure += GetAlphaOverride2;
        }

        private static Color ApplyGetAlpha(Player player, Color result)
        {
            // Don't do anything if the event has no subcribers.
            if (GetAlphaEvent is null)
                return result;

            // Get the mod player instance.
            NoxusPlayer p = player.GetModPlayer<NoxusPlayer>();

            // Apply subscriber contents to the resulting color.
            foreach (Delegate d in GetAlphaEvent.GetInvocationList())
                ((ColorChangeDelegate)d).Invoke(p, ref result);

            return result;
        }

        private static Color GetAlphaOverride(On_Player.orig_GetImmuneAlpha orig, Player self, Color newColor, float alphaReduction)
        {
            return ApplyGetAlpha(self, orig(self, newColor, alphaReduction));
        }

        private static Color GetAlphaOverride2(On_Player.orig_GetImmuneAlphaPure orig, Player self, Color newColor, float alphaReduction)
        {
            return ApplyGetAlpha(self, orig(self, newColor, alphaReduction));
        }

        public override void ResetEffects()
        {
            // Apply the reset effects event.
            ResetEffectsEvent?.Invoke(this);
        }

        public override void SaveData(TagCompound tag)
        {
            // Apply the save data event.
            SaveDataEvent?.Invoke(this, tag);
        }

        public override void LoadData(TagCompound tag)
        {
            // Apply the load data event.
            LoadDataEvent?.Invoke(this, tag);
        }

        public override void PostUpdate()
        {
            // Apply the post-update event.
            PostUpdateEvent?.Invoke(this);
        }

        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            // Do nothing by default to stats.
            health = StatModifier.Default;
            mana = StatModifier.Default;

            // Apply the stat modification event.
            MaxStatsEvent?.Invoke(this, ref health, ref mana);
        }
    }
}
