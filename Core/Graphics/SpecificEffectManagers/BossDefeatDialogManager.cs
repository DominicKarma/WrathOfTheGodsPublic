using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class BossDefeatDialogManager : ModSystem
    {
        private static int calamitasID = -1;

        private static int thanatosID = -1;

        private static int aresID = -1;

        private static int apolloID = -1;

        public override void OnModLoad()
        {
            // Store endgame boss IDs.
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            {
                if (ModReferences.BaseCalamity.TryFind("SupremeCalamitas", out ModNPC calamitas))
                    calamitasID = calamitas.Type;
                if (ModReferences.BaseCalamity.TryFind("ThanatosHead", out ModNPC thanatosHead))
                    thanatosID = thanatosHead.Type;
                if (ModReferences.BaseCalamity.TryFind("AresBody", out ModNPC aresBody))
                    aresID = aresBody.Type;
                if (ModReferences.BaseCalamity.TryFind("Apollo", out ModNPC apollo))
                    apolloID = apollo.Type;
            }

            NoxusGlobalNPC.OnKillEvent += ApplyDeathChecks;
        }

        private void ApplyDeathChecks(NPC npc)
        {
            // Create some indicator text when the WoF is killed about how Noxus has begun orbiting the planet.
            if (npc.type == NPCID.WallofFlesh && !NoxusEggCutsceneSystem.NoxusBeganOrbitingPlanet)
                BroadcastText(NoxusEggCutsceneSystem.PostWoFDefeatText, DialogColorRegistry.NoxusTextColor);

            // Create some indicator text when SCal or Draedon (whichever is defeated last) is defeated as a hint to fight Noxus.
            bool draedonDefeatedLast = (npc.type == thanatosID || npc.type == aresID || npc.type == apolloID) && CommonCalamityVariables.CalamitasDefeated && !CommonCalamityVariables.DraedonDefeated;
            bool calDefeatedLast = npc.type == calamitasID && CommonCalamityVariables.DraedonDefeated && !CommonCalamityVariables.CalamitasDefeated;
            if (calDefeatedLast || draedonDefeatedLast)
            {
                // Apply a secondary check to ensure that when an Exo Mech is killed it is the last exo mech.
                bool noExtraExoMechs = NPC.CountNPCS(thanatosID) + NPC.CountNPCS(aresID) + NPC.CountNPCS(apolloID) <= 1;
                if (calDefeatedLast || noExtraExoMechs)
                    BroadcastText(NoxusEggCutsceneSystem.FinalMainBossDefeatText, DialogColorRegistry.NoxusTextColor);
            }
        }
    }
}
