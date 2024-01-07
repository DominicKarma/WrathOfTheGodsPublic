using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Fixes
{
    /* CONTEXT: 
     * Screen shaders were not built for what they have become. Terraria really only uses them for tinting effects such as pillar color overlays.
     * Things like screen distortions and sophisticated post-processing shader draws are foreign to it.
     * As such, the vanilla game works under the assumption that it's acceptable to disallow screen shaders on Retro and Trippy lighting modes.
     * Unfortunately, this is far from true when used more broadly in the way that mods such as this use screen shaders.
     * An extreme example is the quasar visual. That literally is just a screen shader due to the inbuilt gravitational lensing effects. Imagine it not being allowed
     * to draw in even the most remote capacity due to the aforementioned limitations.
     * 
     * As such, this Retro/Trippy "no screen shaders" behavior is completely removed.
     * 
     * It would be beneficial to implement a separate screen overlay shader system, but this may not be logistically feasible.
     */
    public class ScreenShaderFixerSystem : ModSystem
    {
        public override void OnModLoad()
        {
            new ManagedILEdit("Disable Retro/Trippy Lighting Disabling Screen Shaders", edit =>
            {
                IL_Main.DoDraw += edit.SubscriptionWrapper;
            }, SubjugateTheRetroPilled).Apply(false);
        }

        private void SubjugateTheRetroPilled(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Lighting>("get_NotRetro")))
            {
                edit.LogFailure("The Lighting.NotRetro property could not be found.");
                return;
            }

            // Do OR 1 on the "can screen shaders be drawn" bool to make it always true, regardless of lighting mode.
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Or);

            // Go to the "if (Main.gameMenu || Main.netMode == 2)" check that disallows screen shaders and disable the game menu check if the screen should be shaked.
            if (!cursor.TryGotoPrev(MoveType.After, i => i.MatchLdsfld<Main>("gameMenu")))
            {
                edit.LogFailure("The Main.gameMenu field load could not be found.");
                return;
            }

            cursor.EmitDelegate(() => MainMenuScreenShakeShaderData.ScreenShakeIntensity <= 0.01f);
            cursor.Emit(OpCodes.And);
        }
    }
}
