using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.CalRemixCompatibilitySystem;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public partial class NamelessDeityInfiniteStairwayManager : ModSystem
    {
        private static void LoadFannyRamblings()
        {
            if (CalamityRemix is null)
                return;

            // Load text for when going up the stairs.
            var stairs1 = new FannyDialog("StairwayText1", "Idle").WithDuration(6f).WithCondition(_ => StairwayIsVisible && Main.rand.NextBool(100)).WithoutClickability();
            var stairs2 = new FannyDialog("StairwayText2", "Idle").WithDuration(4f).WithParentDialog(stairs1, 2f);
            var stairs3 = new FannyDialog("StairwayText3", "Awooga").WithDuration(5f).WithParentDialog(stairs2, 4f);
            var stairs4 = new FannyDialog("StairwayText4", "Sob").WithDuration(5f).WithParentDialog(stairs3, 4f);
            var stairs5 = new FannyDialog("StairwayText5", "Idle").WithDuration(5.5f).WithParentDialog(stairs4, 4f);
            stairs5.Register();
            stairs4.Register();
            stairs3.Register();
            stairs2.Register();
            stairs1.Register();

            // Load text for when at the top of the stairs.
            float waitBetweenDialog = 1.75f;
            float namelessDeityDialogDuration = (NamelessDeityInfiniteStairwayTopAnimationManager.DialogLineExistTime - (waitBetweenDialog - 0.67f) * 60f) / 60f;
            var stairsTop1 = new FannyDialog("StairwayTextTop1", "Awooga").WithDuration(namelessDeityDialogDuration).WithCondition(_ => NamelessDeityInfiniteStairwayTopAnimationManager.AnimationTimer >= 136).WithoutClickability();
            var stairsTop2 = new FannyDialog("StairwayTextTop2", "Nuhuh").WithDuration(namelessDeityDialogDuration).WithParentDialog(stairsTop1, waitBetweenDialog);
            var stairsTop3 = new FannyDialog("StairwayTextTop3", "Sob").WithDuration(namelessDeityDialogDuration).WithParentDialog(stairsTop2, waitBetweenDialog);
            var stairsTop4 = new FannyDialog("StairwayTextTop4", "Idle").WithDuration(namelessDeityDialogDuration).WithParentDialog(stairsTop3, waitBetweenDialog);
            var stairsTop5 = new FannyDialog("StairwayTextTop5", "Idle").WithDuration(namelessDeityDialogDuration).WithParentDialog(stairsTop4, waitBetweenDialog);
            var stairsTop6 = new FannyDialog("StairwayTextTop6", "Nuhuh").WithDuration(namelessDeityDialogDuration).WithParentDialog(stairsTop5, 0.2f);
            stairsTop6.Register();
            stairsTop5.Register();
            stairsTop4.Register();
            stairsTop3.Register();
            stairsTop2.Register();
            stairsTop1.Register();
        }
    }
}
