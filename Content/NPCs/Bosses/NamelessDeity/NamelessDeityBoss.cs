using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Music;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    [AutoloadBossHead]
    public partial class NamelessDeityBoss : ModNPC, IInfernumBossBarSupport
    {
        #region Custom Types and Enumerations

        public enum WingMotionState
        {
            Flap,
            RiseUpward,
        }

        public enum NamelessAIType
        {
            // Spawn animation behaviors.
            Awaken,
            OpenScreenTear,
            RoarAnimation,

            // Magic attacks.
            ConjureExplodingStars,
            ArcingEyeStarbursts,
            RealityTearDaggers,
            SuperCosmicLaserbeam,

            // Fire attacks.
            SunBlenderBeams,
            PerpendicularPortalLaserbeams,

            // General cosmic attacks.
            CrushStarIntoQuasar,
            InwardStarPattenedExplosions,
            BackgroundStarJumpscares,
            SwordConstellation,
            MomentOfCreation,

            // Reality manipulation attacks.
            VergilScreenSlices,
            RealityTearPunches,
            ClockConstellation,
            DarknessWithLightSlashes,

            // Phase transitions.
            EnterPhase2,
            EnterPhase3,

            // Death animation variants.
            DeathAnimation,
            DeathAnimation_GFB,

            // GFB exclusive things.
            RodOfHarmonyRant,
            Glock,

            // Intermediate states.
            Teleport,
            ResetCycle,

            // Useful count constant.
            Count
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties

        /// <summary>
        /// Private backing field for <see cref="Myself"/>.
        /// </summary>
        private static NPC myself;

        /// <summary>
        /// The sound slot that handles Nameless' idle ambience.
        /// </summary>
        public SlotId IdleSoundSlot;

        /// <summary>
        /// A list of all of Nameless' hands. Hands may be created and destroyed at will via <see cref="ConjureHandsAtPosition(Vector2, Vector2, bool)"/> and <see cref="DestroyAllHands(bool)"/>.
        /// </summary>
        public List<NamelessDeityHand> Hands = new();

        /// <summary>
        /// The current phase represented as an integer. Zero corresponds to Phase 1, One corresponds to Phase 2, etc.
        /// </summary>
        public int CurrentPhase
        {
            get;
            set;
        }

        /// <summary>
        /// The length of the current fight. This represents the amount of frames it's been since Nameless spawned in.
        /// </summary>
        public int FightLength
        {
            get;
            set;
        }

        /// <summary>
        /// Nameless' Z position value. This is critical for attacks where he enters the background.<br></br>
        /// When in the background, his aesthetic is overall darker and smaller, to sell the illusion.
        /// </summary>
        public float ZPosition
        {
            get;
            set;
        }

        /// <summary>
        /// A general purpose hover offset vector for attacks.
        /// </summary>
        public Vector2 GeneralHoverOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Whether Nameless should draw behind tiles in its layering due to his current <see cref="ZPosition"/>.
        /// </summary>
        public bool ShouldDrawBehindTiles => ZPosition >= 0.2f;

        /// <summary>
        /// The intensity of afterimages relative to Nameless' speed.
        /// </summary>
        public float AfterimageOpacityFactor => Utils.Remap(NPC.velocity.Length(), 13f, 37.5f, 0.7f, 1.05f);

        /// <summary>
        /// The chance for an afterimage to spawn for a given frame.
        /// </summary>
        public float AfterimageSpawnChance => Clamp(InverseLerp(23f, 50f, NPC.velocity.Length()) * 0.6f + Clamp(ZPosition - 0.4f, 0f, 50f) * 0.3f, 0f, 1f);

        /// <summary>
        /// Nameless' life ratio as a 0-1 value. Used notably for phase transition triggers.
        /// </summary>
        public float LifeRatio => Clamp(NPC.life / (float)NPC.lifeMax, 0f, 1f);

        /// <summary>
        /// Nameless' current target. Can be either a player or Noxus' second phase form.
        /// </summary>
        public NPCAimedTarget Target => NPC.GetTargetData();

        /// <summary>
        /// A shorthand for the <see cref="Target"/>'s horizontal direction. Useful for "Teleport behind the target" effects.
        /// </summary>
        public int TargetDirection
        {
            get
            {
                if (NPC.HasPlayerTarget)
                    return Main.player[NPC.target].direction;

                NPC target = Main.npc[NPC.TranslatedTargetIndex];
                return target.velocity.X.NonZeroSign();
            }
        }

        /// <summary>
        /// Nameless' scale, relative to the effects of <see cref="TeleportVisualsInterpolant"/>. Should be used for most contexts instead of <see cref="NPC.scale"/>.
        /// </summary>
        public Vector2 TeleportVisualsAdjustedScale
        {
            get
            {
                float maxStretchFactor = 1.4f;
                Vector2 scale = Vector2.One * NPC.scale;
                if (TeleportVisualsInterpolant > 0f && TeleportVisualsInterpolant < 1f)
                {
                    // 1. Horizontal stretch.
                    if (TeleportVisualsInterpolant <= 0.25f)
                    {
                        float localInterpolant = InverseLerp(0f, 0.25f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, maxStretchFactor, Convert01To010(localInterpolant));
                        scale.Y *= Lerp(1f, 0.1f, Pow(localInterpolant, 2f));
                    }

                    // 2. Vertical collapse.
                    else if (TeleportVisualsInterpolant <= 0.5f)
                    {
                        float localInterpolant = Pow(InverseLerp(0.5f, 0.25f, TeleportVisualsInterpolant), 1.02f);
                        scale.X = localInterpolant;
                        scale.Y = localInterpolant * 0.1f;
                    }

                    // 3. Return to normal scale, use vertical overshoot at the end.
                    else
                    {
                        float localInterpolant = InverseLerp(0.5f, 0.92f, TeleportVisualsInterpolant);

                        // 1.594424 = 1 / sin(1.96)^6, acting as a correction factor to ensure that the final scale in the sinusoidal overshoot is one.
                        float verticalScaleOvershot = Pow(Sin(localInterpolant * 1.96f), 6f) * 1.594424f;
                        scale.X = localInterpolant;
                        scale.Y = verticalScaleOvershot;
                    }
                }

                // AWESOME!
                if (NamelessDeityFormPresetRegistry.UsingAmmyanPreset)
                    scale.X *= 2.4763f;

                return scale;
            }
        }

        /// <summary>
        /// The current AI state Nameless is using. This uses the <see cref="StateMachine"/> under the hood.
        /// </summary>
        public NamelessAIType CurrentState
        {
            get
            {
                // Add the relevant phase cycle if it has been exhausted, to ensure that Nameless' attacks are cyclic.
                if ((StateMachine?.StateStack?.Count ?? 1) <= 0)
                    StateMachine.StateStack.Push(StateMachine.StateRegistry[NamelessAIType.ResetCycle]);

                return StateMachine?.CurrentState?.Identifier ?? NamelessAIType.Awaken;
            }
        }

        /// <summary>
        /// A 0-1 interpolant for teleport effects. This affects Nameless' scale.<br></br>
        /// In the 0 to 0.5 range, Nameless fades out, with the expectation that at 0.5 the teleport will happen.<br></br>
        /// In the 0.5 to 1 range, Nameless fade in again, with the expectation that the teleport has concluded.
        /// </summary>
        public ref float TeleportVisualsInterpolant => ref NPC.localAI[0];

        /// <summary>
        /// The flap animation type for Nameless' large wings. Should be <see cref="WingMotionState.Flap"/> for most contexts.
        /// </summary>
        public WingMotionState WingsMotionState
        {
            get => (WingMotionState)NPC.localAI[3];
            set => NPC.localAI[3] = (int)value;
        }

        /// <summary>
        /// The world position for the censored eye flower
        /// </summary>
        public Vector2 EyePosition => NPC.Center - Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale.Y * 226f;

        /// <summary>
        /// The ideal world position for the censor overlay.
        /// </summary>
        public Vector2 IdealCensorPosition => EyePosition + Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale.Y * 120f;

        /// <summary>
        /// A shorthand accessor for the Nameless Deity NPC. Returns null if not currently present.
        /// </summary>
        public static NPC Myself
        {
            get
            {
                if (myself is not null && !myself.active)
                    return null;

                return myself;
            }
            internal set => myself = value;
        }

        /// <summary>
        /// The amount of damage starburst projectiles (Such as <see cref="Starburst"/>) do.
        /// </summary>
        public static int StarburstDamage => Main.expertMode ? 400 : 300;

        /// <summary>
        /// The amount of damage supernova energy (aka the little moving things during the quasar attack) do.
        /// </summary>
        public static int SupernovaEnergyDamage => Main.expertMode ? 400 : 300;

        /// <summary>
        /// The amount of damage exploding stars do.
        /// </summary>
        public static int ExplodingStarDamage => Main.expertMode ? 450 : 360;

        /// <summary>
        /// The amount of damage light daggers do.
        /// </summary>
        public static int DaggerDamage => Main.expertMode ? 450 : 360;

        /// <summary>
        /// The amount of damage screen slices do.
        /// </summary>
        public static int ScreenSliceDamage => Main.expertMode ? 450 : 360;

        /// <summary>
        /// The amount of damage primordial stardust (the stuff created from Nameless' fans during the perpendicular laser attack) does.
        /// </summary>
        public static int PrimordialStardustDamage => Main.expertMode ? 450 : 360;

        /// <summary>
        /// The amount of damage sun laserbeams (the ones from the big held-in-hand sun attack) do.
        /// </summary>
        public static int SunBeamDamage => Main.expertMode ? 540 : 385;

        /// <summary>
        /// The amount of damage light slashes (the ones during the scary background attack) do.
        /// </summary>
        public static int LightSlashDamage => Main.expertMode ? 540 : 400;

        /// <summary>
        /// The amount of damage portal laserbeams do.
        /// </summary>
        public static int PortalLaserbeamDamage => Main.expertMode ? 600 : 420;

        /// <summary>
        /// The amount of damage the sword constellation does.
        /// </summary>
        public static int SwordConstellationDamage => Main.expertMode ? 650 : 450;

        /// <summary>
        /// The amount of damage falling galaxies (the ones from the moment of creation attack) do.
        /// </summary>
        public static int GalaxyDamage
        {
            get
            {
                if (Main.zenithWorld)
                    return 50000;

                return Main.expertMode ? 675 : 475;
            }
        }

        /// <summary>
        /// The amount of damage the flying quasar does.
        /// </summary>
        public static int QuasarDamage => Main.expertMode ? 700 : 500;

        /// <summary>
        /// The amount of damage the super cosmic laserbeam does.<br></br>
        /// <i>Hits several times per second, resulting in a shredding effect. This is why the damage values are abnormally low.</i>
        /// </summary>
        public static int SuperLaserbeamDamage => Main.expertMode ? 336 : 275;

        /// <summary>
        /// The ideal duration of a Nameless Deity fight in frames.<br></br>
        /// This value is used in the Timed DR system to make Nameless more resilient if the player is killing him unusually quickly.
        /// </summary>
        public static int IdealFightDuration => MinutesToFrames(4.5f);

        /// <summary>
        /// The maximum amount of damage reduction Nameless should have as a result of timed DR. A value of 0.75 for example would correspond to 75% damage reduction.<br></br>
        /// This mechanic exists to make balance more uniform (Yes it is evil, blame the absurdity of shadowspec tier balancing).
        /// </summary>
        public static float MaxTimedDRDamageReduction => 0.76f;

        /// <summary>
        /// The life ratio at which Nameless may transition to his second phase.<br></br>
        /// Where reasonable, this tries to wait for the current attack to conclude before transitioning.
        /// </summary>
        public const float Phase2LifeRatio = 0.65f;

        /// <summary>
        /// The life ratio at which Nameless may transition to his third phase.<br></br>
        /// Unlike the second phase, this happens immediately and is a much more sudden effect.
        /// </summary>
        public const float Phase3LifeRatio = 0.3f;

        /// <summary>
        /// A list of all phase threshold life ratios. This is used by Infernum's boss bar, assuming it's enabled.
        /// </summary>
        public IEnumerable<float> PhaseThresholdLifeRatios
        {
            get
            {
                yield return Phase2LifeRatio;
                yield return Phase3LifeRatio;
            }
        }

        /// <summary>
        /// The amount of damage reduction Nameless should have by default in a 0-1 range. A value of 0.25 for example would correspond to 25% damage reduction.
        /// </summary>
        public const float DefaultDR = 0.25f;

        /// <summary>
        /// How big Nameless should be in general. Be careful with this number, as it will affect things like design impact and hitboxes.
        /// </summary>
        public const float DefaultScaleFactor = 0.875f;

        #endregion Fields and Properties

        #region AI
        public override void AI()
        {
            // Pick a target if the current one is invalid.
            bool invalidTargetIndex = Target.Invalid;
            if (invalidTargetIndex)
                TargetClosest();

            if (!NPC.WithinRange(Target.Center, 4600f))
                TargetClosest();

            // Nope.
            if (!Lighting.NotRetro)
                Lighting.Mode = LightMode.Color;

            // Hey bozo the player's gone. Leave.
            if (Target.Invalid)
            {
                SoundMufflingSystem.EarRingingIntensity = 0f;
                SoundMufflingSystem.MuffleFactor = 1f;
                MusicVolumeManipulationSystem.MuffleFactor = 1f;
                NamelessDeitySky.KaleidoscopeInterpolant = 0f;
                NamelessDeitySky.HeavenlyBackgroundIntensity = 0f;
                NamelessDeitySky.SeamScale = 0f;
                NPC.active = false;
            }

            // Grant the target infinite flight and ensure that they receive the boss effects buff.
            if (NPC.HasPlayerTarget)
            {
                Player playerTarget = Main.player[NPC.target];
                playerTarget.wingTime = playerTarget.wingTimeMax;
                playerTarget.GrantInfiniteFlight();

                playerTarget.GrantBossEffectsBuff();
            }

            // Take damage from NPCs and projectiles if fighting Noxus.
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = NPC.HasNPCTarget;
            if (NPC.HasNPCTarget && NPC.position.Y <= 1600f)
                NPC.position.Y = Main.maxTilesY * 16f - 250f;
            if (NPC.HasNPCTarget && NPC.position.Y >= Main.maxTilesY * 16f - 900f)
                NPC.position.Y = 2300f;

            if (NPC.HasNPCTarget && NPC.position.X <= 400f)
                NPC.position.X = Main.maxTilesX * 16f - 900f;
            if (NPC.HasNPCTarget && NPC.position.X >= Main.maxTilesX * 16f + 400f)
                NPC.position.X = 900f;

            NPC.takenDamageMultiplier = NPC.HasNPCTarget ? 100f : 1f;

            // Disable rain.
            Main.StopRain();
            for (int i = 0; i < Main.maxRain; i++)
                Main.rain[i].active = false;

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.immortal = false;
            NPC.ShowNameOnHover = true;
            NPC.SetDR(DefaultDR);
            TeleportVisualsInterpolant = 0f;
            CosmicBackgroundSystem.StarZoomIncrement = 0f;

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Say NO to weather that destroys the ambience!
            // They cannot take away your gameplay aesthetic without your consent.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Sandstorm.StopSandstorm();

                if (Main.netMode != NetmodeID.Server)
                    Filters.Scene["Graveyard"].Deactivate();
            }

            // Set the global NPC instance.
            Myself = NPC;

            // Disable lifesteal for all players.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                p.moonLeech = true;
            }

            // Perform behaviors.
            switch (CurrentState)
            {
                case NamelessAIType.Awaken:
                    DoBehavior_Awaken();
                    break;
                case NamelessAIType.OpenScreenTear:
                    DoBehavior_OpenScreenTear();
                    break;
                case NamelessAIType.RoarAnimation:
                    DoBehavior_RoarAnimation();
                    break;
                case NamelessAIType.ConjureExplodingStars:
                    DoBehavior_ConjureExplodingStars();
                    break;
                case NamelessAIType.ArcingEyeStarbursts:
                    DoBehavior_ArcingEyeStarbursts();
                    break;
                case NamelessAIType.RealityTearDaggers:
                    DoBehavior_RealityTearDaggers();
                    break;
                case NamelessAIType.SuperCosmicLaserbeam:
                    DoBehavior_SuperCosmicLaserbeam();
                    break;
                case NamelessAIType.SunBlenderBeams:
                    DoBehavior_SunBlenderBeams();
                    break;
                case NamelessAIType.PerpendicularPortalLaserbeams:
                    DoBehavior_PerpendicularPortalLaserbeams();
                    break;
                case NamelessAIType.CrushStarIntoQuasar:
                    DoBehavior_CrushStarIntoQuasar();
                    break;
                case NamelessAIType.InwardStarPattenedExplosions:
                    DoBehavior_InwardStarPattenedExplosions();
                    break;
                case NamelessAIType.BackgroundStarJumpscares:
                    DoBehavior_BackgroundStarJumpscares();
                    break;
                case NamelessAIType.SwordConstellation:
                    DoBehavior_SwordConstellation();
                    break;
                case NamelessAIType.MomentOfCreation:
                    DoBehavior_MomentOfCreation();
                    break;
                case NamelessAIType.VergilScreenSlices:
                    DoBehavior_VergilScreenSlices();
                    break;
                case NamelessAIType.RealityTearPunches:
                    DoBehavior_RealityTearPunches();
                    break;
                case NamelessAIType.ClockConstellation:
                    DoBehavior_ClockConstellation();
                    break;
                case NamelessAIType.DarknessWithLightSlashes:
                    DoBehavior_DarknessWithLightSlashes();
                    break;
                case NamelessAIType.EnterPhase2:
                    DoBehavior_EnterPhase2();
                    break;
                case NamelessAIType.EnterPhase3:
                    DoBehavior_EnterPhase3();
                    break;
                case NamelessAIType.DeathAnimation:
                    DoBehavior_DeathAnimation();
                    break;
                case NamelessAIType.DeathAnimation_GFB:
                    DoBehavior_DeathAnimation_GFB();
                    break;
                case NamelessAIType.RodOfHarmonyRant:
                    DoBehavior_RodOfHarmonyRant();
                    break;
                case NamelessAIType.Glock:
                    DoBehavior_Glock();
                    break;
                case NamelessAIType.Teleport:
                    DoBehavior_Teleport();
                    break;
                default:
                    NPC.velocity = Vector2.Zero;
                    DefaultUniversalHandMotion(950f * (ZPosition + 1f));
                    UpdateWings(AttackTimer / 50f % 1f);
                    break;
            }

            // Update the state machine.
            StateMachine.PerformBehaviors();
            StateMachine.PerformStateTransitionCheck();

            // Disable the silly Alicorn on a Stick in GFB.
            if (Main.zenithWorld && ModReferences.BaseCalamity is not null && NPC.HasPlayerTarget && Target.Center.WithinRange(NPC.Center, TeleportVisualsAdjustedScale.X * 420f))
            {
                Item item = Main.player[NPC.TranslatedTargetIndex].HeldMouseItem();
                int cheatItemID = ModReferences.BaseCalamity.Find<ModItem>("AlicornonaStick").Type;
                if (item.type == cheatItemID && Myself is not null)
                    item.SetDefaults(ItemID.UnicornHorn);
            }

            // Calculate the afterimage count based on velocity and Z position.
            if (Main.netMode != NetmodeID.Server && Main.rand.NextFloat() < AfterimageSpawnChance)
            {
                Vector2 afterimageSpawnPosition = NPC.Center + Main.rand.NextVector2Circular(20f, 20f);
                Vector2 afterimageVelocity = Main.rand.NextVector2Circular(9f, 9f) * AfterimageOpacityFactor;
                NewProjectileBetter(NPC.GetSource_FromAI(), afterimageSpawnPosition, afterimageVelocity, ModContent.ProjectileType<NamelessDeityAfterimage>(), 0, 0f, -1, MathF.Max(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y), NPC.rotation);
            }

            // Handle mumble sounds.
            if (MumbleTimer >= 1)
            {
                MumbleTimer++;

                float mumbleCompletion = MumbleTimer / 45f;
                if (mumbleCompletion >= 1f)
                    MumbleTimer = 0;

                // Play the sound.
                if (MumbleTimer == 16)
                {
                    SoundEngine.PlaySound(MumbleSound, Target.Center);
                    StartShakeAtPoint(NPC.Center, 7.5f);
                }
            }

            // Disable damage when invisible.
            if (NPC.Opacity <= 0.35f)
            {
                NPC.ShowNameOnHover = false;
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
            }

            // Get rid of all falling stars. Their noises completely ruin the ambience.
            // active = false must be used over Kill because the Kill method causes them to drop their fallen star items.
            var fallingStars = AllProjectilesByID(ProjectileID.FallingStar);
            foreach (Projectile star in fallingStars)
                star.active = false;

            // Make the censor intentionally move in a bit of a "choppy" way, where it tries to stick to the ideal position, but only if it's far
            // enough away.
            // As a failsafe, it sticks perfectly if Nameless is moving really quickly so that it doesn't gain too large of a one-frame delay. Don't want to be
            // accidentally revealing what's behind there, after all.
            if (NPC.position.Distance(NPC.oldPosition) >= 76f)
                CensorPosition = IdealCensorPosition;
            else if (!CensorPosition.WithinRange(IdealCensorPosition, 34f) || ZPosition >= 2f)
                CensorPosition = IdealCensorPosition;

            // Increment timers.
            AttackTimer++;
            FightLength++;

            // Update keyboard shader effects.
            NamelessDeityKeyboardShader.EyeBrightness = NPC.Opacity;

            // Perform Z position visual effects.
            PerformZPositionEffects();

            // Update the idle sound.
            UpdateIdleSound();

            // Update swappable textures.
            UpdateSwappableTextures();

            // Make it night time. This does not apply if time is being manipulated by the clock.
            if (!AnyProjectiles(ModContent.ProjectileType<ClockConstellation>()))
            {
                Main.dayTime = false;
                Main.time = Lerp((float)Main.time, 16200f, 0.14f);
            }

            // Hold HP in place while waiting for phase 2 or death.
            if (!WaitingForDeathAnimation && CurrentPhase == 0 && NPC.life <= NPC.lifeMax * Phase2LifeRatio)
                WaitingForPhase2Transition = true;
            if (WaitingForDeathAnimation)
            {
                NamelessDeitySky.SeamScale = 0f;
                NPC.life = 1;
                NPC.immortal = true;
            }
            if (WaitingForPhase2Transition)
            {
                NPC.life = (int)(NPC.lifeMax * Phase2LifeRatio);
                NPC.immortal = true;
            }

            // Update hands.
            foreach (NamelessDeityHand hand in Hands)
            {
                // Move.
                hand.Center += hand.Velocity;

                // Make hands fade in.
                hand.Opacity = Clamp(hand.Opacity + 0.03f, 0f, 1f);

                // Update the trail position cache.
                for (int i = hand.OldCenters.Length - 1; i >= 1; i--)
                    hand.OldCenters[i] = hand.OldCenters[i - 1];
                hand.OldCenters[0] = hand.Center;
            }

            // Rotate based on horizontal speed by default.
            // As an exception, Nameless spins wildly if the player's name is smh as a """dev preset""".
            if (NamelessDeityFormPresetRegistry.UsingSmhPreset)
                NPC.rotation += NPC.velocity.X.NonZeroSign() * 0.6f;
            else
            {
                float idealRotation = Clamp(NPC.velocity.X * 0.0033f, -0.16f, 0.16f);
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.3f).AngleTowards(idealRotation, 0.045f);
            }
        }

        /// <summary>
        /// Finds a new target for Nameless.<br></br>
        /// This performs the standard player distance search check, but gives Noxus' second form absolute targeting if present.
        /// </summary>
        public void TargetClosest()
        {
            NPCUtils.TargetSearchResults targetSearchResults = NPCUtils.SearchForTarget(NPC, NPCUtils.TargetSearchFlag.NPCs | NPCUtils.TargetSearchFlag.Players, _ => EntropicGod.Myself is null, NoxusSearchCheck);
            if (!targetSearchResults.FoundTarget)
                return;

            // Check for players. Prioritize Noxus if he's present.
            int targetIndex = targetSearchResults.NearestTargetIndex;
            if (targetSearchResults.FoundNPC)
                targetIndex = targetSearchResults.NearestNPCIndex + 300;

            NPC.target = targetSearchResults.NearestTargetIndex;
            NPC.targetRect = targetSearchResults.NearestTargetHitbox;
        }

        /// <summary>
        /// Search filter for Noxus.
        /// </summary>
        /// <param name="npc">The NPC to check.</param>
        public static bool NoxusSearchCheck(NPC npc)
        {
            return npc.type == ModContent.NPCType<EntropicGod>() && npc.Opacity > 0f && !npc.immortal && !npc.dontTakeDamage && npc.active;
        }

        #endregion AI
    }
}
