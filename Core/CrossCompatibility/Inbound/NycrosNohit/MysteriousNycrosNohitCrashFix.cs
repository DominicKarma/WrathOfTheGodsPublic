using System;
using System.Reflection;
using MonoMod.Cil;
using NoxusBoss.Common.Subworlds;
using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    // I dunno what the root cause is but people have reported that the death code for Nycro's mod for some reasons causes duplicate and erroneous player file reads, and thus results in a crash.
    public class MysteriousNycrosNohitCrashFix : ModSystem
    {
        private static bool disableNextPlayerSave;

        public override void PostSetupContent()
        {
            // Terminate setup immediately if Nycro's Mod is not enabled.
            if (NycrosNohitMod is null)
                return;

            // Terminate setup immediately if Nycro's mod does not contain the EffPlayer class. This is likely indicative of the class being changed.
            string warningText = "This may result in unexpected crashes for the Nameless Deity fight!";
            Type nycrosNohitPlayerType = NycrosNohitMod.Code.GetType($"{NycrosNohitMod.Name}.EffPlayer");
            if (nycrosNohitPlayerType is null)
            {
                Mod.Logger.Warn($"Nycro's Nohit Mod was found, but its player class could not be loaded to apply a fix patch! {warningText}");
                return;
            }

            MethodInfo onHurtMethod = nycrosNohitPlayerType.GetMethod("OnHurt", BindingFlags.Instance | BindingFlags.Public);
            if (onHurtMethod is null)
            {
                Mod.Logger.Warn($"Nycro's Nohit Mod was found, but its player class' OnHurt hook could not be loaded to apply a fix patch! {warningText}");
                return;
            }

            // Apply the fixing IL edit and detour.
            On_WorldGen.saveToonWhilePlayingCallBack += DisallowPlayerSaving;
            MonoModHooks.Modify(onHurtMethod, DisableManualPlayerSavingInNamelessDeityFight);
        }

        private void DisallowPlayerSaving(On_WorldGen.orig_saveToonWhilePlayingCallBack orig, object threadContext)
        {
            if (disableNextPlayerSave)
            {
                disableNextPlayerSave = false;
                return;
            }

            orig(threadContext);
        }

        private void DisableManualPlayerSavingInNamelessDeityFight(ILContext context)
        {
            // Define the IL cursor.
            ILCursor cursor = new(context);

            // Apply the disabling effects at the very start of the method.
            cursor.EmitDelegate(() =>
            {
                if (EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                    disableNextPlayerSave = true;
            });
        }
    }
}
