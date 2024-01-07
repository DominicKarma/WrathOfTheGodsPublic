using NoxusBoss.Core;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace NoxusBoss.Common.DropRules
{
    public class BeforeNamelessDefeatedDropRule : IItemDropRuleCondition, IProvideItemConditionDescription
    {
        public bool CanDrop(DropAttemptInfo info) => !WorldSaveSystem.HasDefeatedNamelessDeity;

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription()
        {
            return Language.GetTextValue("Mods.NoxusBoss.Bestiary.ItemDropConditions.FirstTimeExclusive");
        }
    }
}
