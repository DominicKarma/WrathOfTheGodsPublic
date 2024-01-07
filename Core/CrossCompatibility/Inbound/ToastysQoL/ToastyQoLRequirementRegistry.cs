namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public static class ToastyQoLRequirementRegistry
    {
        public static readonly ToastyQoLRequirement PostDraedonAndCal = new("Endgame", () => CommonCalamityVariables.DraedonDefeated && CommonCalamityVariables.CalamitasDefeated);

        public static readonly ToastyQoLRequirement PostNoxus = new("Entropic God", () => WorldSaveSystem.HasDefeatedNoxus);

        public static readonly ToastyQoLRequirement PostNamelessDeity = new("Nameless Deity", () => WorldSaveSystem.HasDefeatedNamelessDeity);
    }
}
