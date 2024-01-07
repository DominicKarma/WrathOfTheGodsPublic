using System;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.StateMachines;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC, IBossChecklistSupport, IToastyQoLChecklistBossSupport
    {
        private PushdownAutomata<BossAIState<NamelessAIType>, NamelessAIType> stateMachine;

        public PushdownAutomata<BossAIState<NamelessAIType>, NamelessAIType> StateMachine
        {
            get
            {
                if (stateMachine is null)
                    LoadStates();
                return stateMachine;
            }
            set => stateMachine = value;
        }

        public ref int AttackTimer => ref StateMachine.CurrentState.Time;

        public void LoadStates()
        {
            // Initialize the AI state machine.
            StateMachine = new(new(NamelessAIType.Awaken));
            StateMachine.OnStateTransition += ResetGenericVariables;

            // Register all nameless deity states in the machine.
            for (int i = 0; i < (int)NamelessAIType.Count; i++)
                StateMachine.RegisterState(new((NamelessAIType)i));

            // Load state transitions.
            LoadStateTransitions();
        }

        public void ResetGenericVariables(bool stateWasPopped)
        {
            GeneralHoverOffset = Vector2.Zero;
            NPC.Opacity = 1f;

            // Reset generic AI variables and the Z position if the state was popped.
            // If it wasn't popped, these should be retained for the purpose of preserving AI information when going back to the previous state.
            if (stateWasPopped)
            {
                NPC.ai[2] = NPC.ai[3] = 0f;
                ZPosition = 0f;
            }

            // Reset robe usage visuals.
            for (int i = 0; i < Hands.Count; i++)
                Hands[i].HasArms = true;

            // Reset things when preparing for entering the new phase.
            if (CurrentState == NamelessAIType.EnterPhase2)
            {
                CurrentPhase = 1;
                WaitingForPhase2Transition = false;
                ClearAllProjectiles();
            }
            if (CurrentState == NamelessAIType.EnterPhase3)
            {
                CurrentPhase = 2;
                ClearAllProjectiles();
            }

            // Mark the Moment of Creation attack as witnessed if it was just selected.
            if (CurrentState == NamelessAIType.MomentOfCreation)
                HasExperiencedFinalAttack = true;

            // Reset texture variants if Nameless isn't visible.
            if (NPC.Opacity <= 0.01f || TeleportVisualsAdjustedScale.Length() <= 0.05f || !NPC.WithinRange(Target.Center, 1100f))
                RerollAllSwappableTextures();

            DestroyAllHands();
            TargetClosest();
            NPC.netUpdate = true;
        }

        public void ForceNextAttack(NamelessAIType state)
        {
            if (IsAttackState(CurrentState))
                StateMachine.StateStack.Push(StateMachine.StateRegistry[state]);
        }

        public void LoadStateTransitions()
        {
            // Load introductory states.
            LoadStateTransitions_Awaken();
            LoadStateTransitions_OpenScreenTear();
            LoadStateTransitions_RoarAnimation();

            // Load Phase 1 attack states.
            LoadStateTransitions_ConjureExplodingStars();
            LoadStateTransitions_ArcingEyeStarbursts();
            LoadStateTransitions_RealityTearDaggers();
            LoadStateTransitions_PerpendicularPortalLaserbeams();
            LoadStateTransitions_SunBlenderBeams();
            LoadStateTransitions_CrushStarIntoQuasar();

            // Load Phase 2 attack states.
            LoadStateTransitions_VergilScreenSlices();
            LoadStateTransitions_RealityTearPunches();
            LoadStateTransitions_InwardStarPattenedExplosions();
            LoadStateTransitions_BackgroundStarJumpscares();
            LoadStateTransitions_SwordConstellation();

            // Load Phase 3 attack states.
            LoadStateTransitions_DarknessWithLightSlashes();
            LoadStateTransitions_SuperCosmicLaserbeam();
            LoadStateTransitions_ClockConstellation();
            LoadStateTransitions_MomentOfCreation();

            // Load GFB attack states.
            LoadStateTransitions_Glock();

            // Load phase transition states.
            LoadStateTransitions_Phase2TransitionStart();
            LoadStateTransitions_Phase2TransitionEnd();
            LoadStateTransitions_Phase3TransitionStart();
            LoadStateTransitions_Phase3TransitionEnd();
            LoadStateTransitions_RodOfHarmonyRant();

            // Load death animation states.
            LoadStateTransitions_DeathAnimations();

            // Load the cycle reset state.
            LoadStateTransitions_ResetCycle();

            // Load intermediate AI states.
            LoadStateTransitions_Teleport();
        }

        public static bool IsAttackState(NamelessAIType state) => Phase1Cycle.Contains(state) || Phase2Cycle.Contains(state) || Phase3Cycle.Contains(state);

        public static void ApplyToAllStatesWithCondition(Action<NamelessAIType> action, Func<NamelessAIType, bool> condition)
        {
            for (int i = 0; i < (int)NamelessAIType.Count; i++)
            {
                NamelessAIType attack = (NamelessAIType)i;
                if (!condition(attack))
                    continue;

                action(attack);
            }
        }

        public static void ApplyToAllStatesExcept(Action<NamelessAIType> action, params NamelessAIType[] exceptions)
        {
            for (int i = 0; i < (int)NamelessAIType.Count; i++)
            {
                NamelessAIType attack = (NamelessAIType)i;
                if (exceptions.Contains(attack))
                    continue;

                action(attack);
            }
        }
    }
}
