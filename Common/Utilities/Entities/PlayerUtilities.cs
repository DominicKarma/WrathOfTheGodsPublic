using System.Reflection;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static FieldInfo adrenalineField;

        private static FieldInfo rageField;

        /// <summary>
        /// Gives a given <see cref="Player"/> the Boss Effects buff from Calamity, if it's enabled. This buff provides a variety of common effects, such as the near complete removal of natural enemy spawns.
        /// </summary>
        /// <param name="p">The player to apply the buff to.</param>
        public static void GrantBossEffectsBuff(this Player p)
        {
            if (ModReferences.BaseCalamity is null)
                return;

            if (!ModReferences.BaseCalamity.TryFind("BossEffects", out ModBuff bossEffects))
                return;

            p.AddBuff(bossEffects.Type, 2);
        }

        /// <summary>
        /// Resets rage and adrenaline for a given <see cref="Player"/>.
        /// </summary>
        /// <param name="p">The player to reset ripper values for.</param>
        public static void ResetRippers(this Player p)
        {
            foreach (ModPlayer modPlayer in p.ModPlayers)
            {
                if (modPlayer.Name != "CalamityPlayer")
                    continue;

                // Initialize field information if necessary.
                adrenalineField ??= modPlayer.GetType().GetField("adrenaline");
                rageField ??= modPlayer.GetType().GetField("rage");

                adrenalineField?.SetValue(modPlayer, 0f);
                rageField?.SetValue(modPlayer, 0f);
            }
        }

        public static Referenced<T> GetValueRef<T>(this Player player, string key) where T : struct =>
            player.GetModPlayer<NoxusPlayer>().valueRegistry.GetValueRef<T>(key);

        public static Referenced<T> GetValueRef<T>(this NoxusPlayer player, string key) where T : struct =>
            player.valueRegistry.GetValueRef<T>(key);
    }
}
