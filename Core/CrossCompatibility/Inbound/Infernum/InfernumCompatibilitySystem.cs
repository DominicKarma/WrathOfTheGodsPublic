using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class InfernumCompatibilitySystem : ModSystem
    {
        public static bool InfernumModeIsActive
        {
            get
            {
                if (Infernum is null)
                    return false;

                return (bool)Infernum.Call("GetInfernumActive");
            }
        }

        public static bool BossCardModCallsExist => Infernum is not null && Infernum.Version >= new Version(2, 0, 0);

        public static bool BossBarModCallsExist => Infernum is not null && Infernum.Version >= new Version(2, 0, 0);

        public override void PostSetupContent()
        {
            LoadBossIntroCardSuport();
            LoadBossBarSuport();
        }

        internal void LoadBossIntroCardSuport()
        {
            if (!BossCardModCallsExist)
                return;

            // Collect all bosses that should adhere to Infernum's boss card mod calls.
            var modNPCsWithIntroSupport = Mod.LoadInterfacesFromContent<ModNPC, IInfernumBossBarSupport>();

            // Use the mod call for boss intro cards.
            foreach (var modNPC in modNPCsWithIntroSupport)
                PrepareIntroCard(modNPC as IInfernumBossIntroCardSupport);
        }

        internal void LoadBossBarSuport()
        {
            if (!BossBarModCallsExist || Main.netMode == NetmodeID.Server)
                return;

            // Collect all bosses that should adhere to Infernum's boss card mod calls.
            var modNPCsWithBarSupport = Mod.LoadInterfacesFromContent<ModNPC, IInfernumBossBarSupport>();

            // Use the mod call for boss intro cards.
            foreach (var modNPC in modNPCsWithBarSupport)
            {
                Texture2D iconTexture = ModContent.Request<Texture2D>(modNPC.BossHeadTexture, AssetRequestMode.ImmediateLoad).Value;
                Infernum.Call("RegisterBossBarPhaseInfo", modNPC.Type, (modNPC as IInfernumBossBarSupport).PhaseThresholdLifeRatios.ToList(), iconTexture);
            }
        }

        public static void PrepareIntroCard(IInfernumBossIntroCardSupport bossIntroCard)
        {
            // Initialize the base instance for the intro card. Alternative effects may be added separately.
            Func<bool> isActiveDelegate = bossIntroCard.ShouldDisplayIntroCard;
            Func<float, float, Color> textColorSelectionDelegate = bossIntroCard.GetIntroCardTextColor;
            object instance = Infernum.Call("InitializeIntroScreen", bossIntroCard.IntroCardTitleName, bossIntroCard.IntroCardAnimationDuration, bossIntroCard.ShouldIntroCardTextBeCentered, isActiveDelegate, textColorSelectionDelegate);
            Infernum.Call("IntroScreenSetupLetterDisplayCompletionRatio", instance, new Func<int, float>(animationTimer => Clamp(animationTimer / (float)bossIntroCard.IntroCardAnimationDuration * 1.36f, 0f, 1f)));

            // Check for optional data and then apply things as needed via optional mod calls.

            // On-completion effects.
            Action onCompletionDelegate = bossIntroCard.OnIntroCardCompletion;
            Infernum.Call("IntroScreenSetupCompletionEffects", instance, onCompletionDelegate);

            // Letter addition sound.
            Func<SoundStyle> chooseLetterSoundDelegate = bossIntroCard.ChooseIntroCardLetterSound;
            Infernum.Call("IntroScreenSetupLetterAdditionSound", instance, chooseLetterSoundDelegate);

            // Main sound.
            Func<SoundStyle> chooseMainSoundDelegate = bossIntroCard.ChooseIntroCardMainSound;
            Func<int, int, float, float, bool> why = (_, _2, _3, _4) => true;
            Infernum.Call("IntroScreenSetupMainSound", instance, why, chooseMainSoundDelegate);

            // Letter shader draw application.
            if (bossIntroCard.LetterDrawShaderEffect is not null)
                Infernum.Call("IntroScreenSetupLetterShader", instance, bossIntroCard.LetterDrawShaderEffect, (object)bossIntroCard.PrepareLetterDrawShader);

            // Text scale.
            Infernum.Call("IntroScreenSetupTextScale", instance, bossIntroCard.IntroCardScale);

            // Register the intro card.
            Infernum.Call("RegisterIntroScreen", instance);
        }
    }
}
