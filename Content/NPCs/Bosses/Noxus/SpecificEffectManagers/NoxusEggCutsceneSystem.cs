using System;
using System.Collections.Generic;
using System.Linq;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.NPCs.Bosses.Noxus.PreFightForm;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusEggCutsceneSystem : ModSystem
    {
        public static bool WillTryToSummonNoxusTonight
        {
            get;
            set;
        }

        public static bool NoxusHasFallenFromSky
        {
            get;
            set;
        }

        public static string PostWoFDefeatText => Language.GetTextValue($"Mods.NoxusBoss.Dialog.PostWoFDefeatNoxusIndicator");

        public static string PostMLNightText => Language.GetTextValue($"Mods.NoxusBoss.Dialog.PostMLNightNoxusIndicator");

        public static string FinalMainBossDefeatText
        {
            get
            {
                if (NoxusHasFallenFromSky)
                    return Language.GetTextValue($"Mods.NoxusBoss.Dialog.FinalBossDefeatNoxusIndicator_SeenNoxus");

                return Language.GetTextValue($"Mods.NoxusBoss.Dialog.FinalBossDefeatNoxusIndicator");
            }
        }

        public static bool NoxusBeganOrbitingPlanet => Main.hardMode || NoxusWorldManager.Enabled;

        public static bool NoxusCanCommitSkydivingFromSpace => NPC.downedMoonlord && !NoxusWorldManager.Enabled;

        public static List<Player> PlayersOnSurface
        {
            get
            {
                List<Player> surfacePlayers = new();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (!p.active || p.dead || !p.ShoppingZone_Forest || p.ZoneSkyHeight)
                        continue;

                    surfacePlayers.Add(p);
                }

                return surfacePlayers;
            }
        }

        public override void PreUpdateWorld()
        {
            // Randomly make Noxus appear at the start of a given night if:
            // 1. Noxus hasn't appeared yet.
            // 2. It's post-ML.
            // 3. A player is on the surface.
            // 4. There are no bosses being fought.
            if ((int)Math.Round(Main.time) == 10 && !NoxusHasFallenFromSky && NoxusCanCommitSkydivingFromSpace && Main.rand.NextBool(3) && PlayersOnSurface.Any() && !Main.dayTime && !AnyBosses())
            {
                BroadcastText(PostMLNightText, DialogColorRegistry.VanillaEventTextColor);
                WillTryToSummonNoxusTonight = true;
            }

            // Randomly spawn Noxus.
            // If a boss is present during this, don't spawn him and try again a different time.
            if (WillTryToSummonNoxusTonight && AnyBosses())
                WillTryToSummonNoxusTonight = false;
            if (WillTryToSummonNoxusTonight && Main.rand.NextBool(7200) && PlayersOnSurface.Any() && !NoxusHasFallenFromSky)
            {
                Player playerToSpawnNear = Main.rand.Next(PlayersOnSurface);
                NPC.NewNPC(new EntitySource_WorldEvent(), (int)playerToSpawnNear.Center.X, (int)playerToSpawnNear.Center.Y - 1200, ModContent.NPCType<NoxusEggCutscene>(), 1);

                NoxusHasFallenFromSky = true;
            }

            // Try again later if Noxus couldn't spawn at night.
            if (Main.dayTime)
                WillTryToSummonNoxusTonight = false;
        }
    }
}
