using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria.GameContent.ItemDropRules;

namespace NoxusBoss.Common.DropRules
{
    public class InfernumActiveDropRule : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info) => InfernumCompatibilitySystem.InfernumModeIsActive;

        public bool CanShowItemDropInUI() => ModReferences.Infernum is not null;

        public string GetConditionDescription() => string.Empty;
    }
}
