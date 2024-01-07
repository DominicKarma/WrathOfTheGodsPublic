using System.Collections.Generic;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class ToastyQoLCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            // Don't load anything if Toasty's QoL mod is not enabled.
            if (ToastyQoL is null)
                return;

            // Load item support.
            LoadItemSupport();

            // Load boss support.
            LoadBossSupport();
        }

        public void LoadItemSupport()
        {
            // Collect all items that should adhere to Toasty's QoL.
            var modItemsWithQoLSupport = Mod.LoadInterfacesFromContent<ModItem, IToastyQoLChecklistItemSupport>(content =>
            {
                return AutoloadAttribute.GetValue(content.GetType()).NeedsAutoloading;
            });

            Dictionary<ToastyQoLRequirement, List<int>> requirementItems = new();

            // Load information into the list.
            foreach (var modItem in modItemsWithQoLSupport)
            {
                IToastyQoLChecklistItemSupport qolInfo = modItem as IToastyQoLChecklistItemSupport;

                if (!requirementItems.ContainsKey(qolInfo.Requirement))
                    requirementItems[qolInfo.Requirement] = new();

                requirementItems[qolInfo.Requirement].Add(modItem.Type);
            }

            // Use the mod call.
            foreach (var requirement in requirementItems.Keys)
                ToastyQoL.Call("AddNewBossLockInformation", requirement.Requirement, requirement.RequirementName, requirementItems[requirement], false);
        }

        public void LoadBossSupport()
        {
            // Collect all bosses that should adhere to Toasty's QoL.
            var modNPCsWithQoLSupport = Mod.LoadInterfacesFromContent<ModNPC, IToastyQoLChecklistBossSupport>();

            // Use the mod call.
            foreach (var modNPC in modNPCsWithQoLSupport)
            {
                IToastyQoLChecklistBossSupport qolInfo = modNPC as IToastyQoLChecklistBossSupport;
                string singularName = Language.GetTextValue($"Mods.{Mod.Name}.NPCs.{modNPC.Name}.DisplayNameSingular");
                ToastyQoL.Call("AddBossToggle", modNPC.BossHeadTexture, singularName, qolInfo.IsDefeatedField, qolInfo.ProgressionValue + 6f, 1f);
            }
        }
    }
}
