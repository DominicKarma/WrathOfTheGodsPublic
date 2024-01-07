using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class BossChecklistCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            // Don't load anything if boss checklist is not enabled.
            if (BossChecklist is null)
                return;

            // Collect all NPCs that should adhere to boss checklist.
            var modNPCsWithBossChecklistSupport = Mod.LoadInterfacesFromContent<ModNPC, IBossChecklistSupport>();

            // Load boss checklist information via mod calls.
            foreach (var modNPC in modNPCsWithBossChecklistSupport)
            {
                IBossChecklistSupport checklistInfo = modNPC as IBossChecklistSupport;
                string registerCall = checklistInfo.IsMiniboss ? "LogMiniBoss" : "LogBoss";

                Dictionary<string, object> extraInfo = new()
                {
                    ["collectibles"] = checklistInfo.Collectibles
                };
                if (checklistInfo.SpawnItem is not null)
                    extraInfo["spawnItems"] = checklistInfo.SpawnItem.Value;
                if (checklistInfo.UsesCustomPortraitDrawing)
                    extraInfo["customPortrait"] = new Action<SpriteBatch, Rectangle, Color>(checklistInfo.DrawCustomPortrait);
                extraInfo["despawnMessage"] = Language.GetText($"Mods.{Mod.Name}.NPCs.{modNPC.Name}.BossChecklistIntegration.DespawnMessage");

                // Use the mod call.
                string result = (string)BossChecklist.Call(new object[]
                {
                    registerCall,
                    Mod,
                    checklistInfo.ChecklistEntryName,
                    checklistInfo.ProgressionValue,
                    () => checklistInfo.IsDefeated,
                    modNPC.Type,
                    extraInfo
                });
            }

            RemoveBossRushFromChecklist();
        }

        // This is cursed af but Boss Checklist does not allow easy REMOVAL of content in the same way as ADDING content via mod calls.
        private static void RemoveBossRushFromChecklist()
        {
            // Get the universal list of Boss Rush entries.
            object bossTracker = BossChecklist.GetType().GetField("bossTracker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            FieldInfo sortedEntriesField = bossTracker.GetType().GetField("SortedEntries", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            object entriesBase = sortedEntriesField.GetValue(bossTracker);
            List<object> entries = ((IEnumerable)entriesBase).Cast<object>().ToList();

            // Calculate entry name data for later.
            string forbiddenEntryName = Language.GetTextValue("Mods.CalamityMod.BossChecklistIntegration.BossRush.EntryName");
            PropertyInfo entryDisplayNameProperty = entries.First().GetType().GetProperty("DisplayName", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            // Since SetValue doesn't accept List<object> it's necessary to create a new list of the hidden type so that it can be passed in.
            // From there, it needs to be casted to an IList so its contents can be indirectly populated. Only THEN can it be passed into SetValue.
            // This sucks.
            object newEntries = Activator.CreateInstance(entriesBase.GetType());
            IList newEntriesCasted = (IList)newEntries;
            foreach (object entry in entries)
            {
                // Remove the boss rush entry.
                string displayName = (string)entryDisplayNameProperty.GetValue(entry);
                if (displayName != forbiddenEntryName)
                    newEntriesCasted.Add(entry);
            }

            sortedEntriesField.SetValue(bossTracker, newEntries);
        }
    }
}
