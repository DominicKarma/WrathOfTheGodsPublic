using System.Collections.Generic;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Common.DropRules;
using NoxusBoss.Content.Items.SummonItems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalNPC : GlobalNPC
    {
        private static int wyrmID = -1;

        internal ReferencedValueRegistry valueRegistry = new();

        public delegate void EditSpawnRateDelegate(Player player, ref int spawnRate, ref int maxSpawns);

        public static event EditSpawnRateDelegate EditSpawnRateEvent;

        public delegate void EditSpawnPoolDelegate(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo);

        public static event EditSpawnPoolDelegate EditSpawnPoolEvent;

        public delegate void NPCActionDelegate(NPC npc);

        public static event NPCActionDelegate OnKillEvent;

        public delegate void NPCSpawnDelegate(NPC npc, IEntitySource source);

        public static event NPCSpawnDelegate OnSpawnEvent;

        public override bool InstancePerEntity => true;

        public override void Load()
        {
            // Load custom NPC IDs.
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal) && cal.TryFind("PrimordialWyrmHead", out ModNPC wyrm))
                wyrmID = wyrm.Type;
        }

        public override void Unload()
        {
            // Reset all events on mod unload.
            EditSpawnRateEvent = null;
            OnKillEvent = null;
            OnSpawnEvent = null;
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            OnSpawnEvent?.Invoke(npc, source);
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // The Primordial Wyrm drops the Terminus in Infernum instead of being in an abyss chest, since there is no abyss chest in Infernum.
            // For compatibility reasons, the Wyrm drops the boss rush starter item as well with this mod.
            // This item is only shown in the bestiary if Infernum is active, because in all other contexts it's unobtainable.
            if (npc.type == wyrmID)
            {
                LeadingConditionRule infernumActive = new(new InfernumActiveDropRule());
                infernumActive.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Terminal>()));

                npcLoot.Add(infernumActive);
            }
        }

        public override void OnKill(NPC npc)
        {
            OnKillEvent?.Invoke(npc);
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            // Apply spawn rate alterations in accordance with the event.
            EditSpawnRateEvent?.Invoke(player, ref spawnRate, ref maxSpawns);
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            // Apply spawn pool alterations in accordance with the event.
            EditSpawnPoolEvent?.Invoke(pool, spawnInfo);
        }
    }
}
