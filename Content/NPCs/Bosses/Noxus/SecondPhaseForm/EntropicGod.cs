using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Particles.Metaballs;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static NoxusBoss.Core.Graphics.Shaders.Keyboard.NoxusKeyboardShader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    // My pride and joy of Terraria.

    // I at one point sought to replicate the magic of others. To create something "special" that stands above almost all others in enjoyment and graphical fidelity.
    // I at one point sought to create a Seth of my own. A MEAC Empress. Something so incredible that people would pay attention to it long after it has finished.
    // Something that would elevate me to the point of being a "somebody". A "master".
    // I no longer hold that desire. I have no need to prove myself to this community any longer. I need only to prove myself to myself.
    //
    // I have done exactly that here.
    //
    // And amusingly, with that paradigm shift, the object of the abandoned desire will be realized.
    [AutoloadBossHead]
    public partial class EntropicGod : ModNPC, IBossChecklistSupport, IToastyQoLChecklistBossSupport
    {
        #region Custom Types and Enumerations
        public enum EntropicGodAttackType
        {
            // Phase 1 attacks.
            DarkExplosionCharges,
            DarkEnergyBoltHandWave,
            FireballBarrage,
            HoveringHandGasBursts,
            RapidExplosiveTeleports,
            TeleportAndShootNoxusGas,

            // Phase 2 attacks.
            Phase2Transition,
            GeometricSpikesTeleportAndFireballs,
            ThreeDimensionalNightmareDeathRay,
            PortalChainCharges,
            RealityWarpSpinCharge,
            OrganizedPortalCometBursts,

            // Phase 3 attacks.
            Phase3Transition,
            BrainFogAndThreeDimensionalCharges,
            PortalChainCharges2,

            // Lol lmao get FUCKED Noxus!!!!!!!!! (Cooldown ""attack"")
            MigraineAttack,

            // Self-explanatory.
            DeathAnimation
        }

        public class EntropicGodHand
        {
            public int Frame;

            public int FrameTimer;

            public bool ShouldOpen;

            public float? RotationOverride;

            public Vector2 Center;

            public Vector2 Velocity;

            public Vector2 DefaultOffset;

            public void WriteTo(BinaryWriter writer)
            {
                writer.WriteVector2(Center);
                writer.WriteVector2(Velocity);
                writer.WriteVector2(DefaultOffset);
            }

            public void ReadFrom(BinaryReader reader)
            {
                Center = reader.ReadVector2();
                Velocity = reader.ReadVector2();
                DefaultOffset = reader.ReadVector2();
            }
        }
        #endregion Custom Types and Enumerations

        #region Fields and Properties
        private static NPC myself;

        public EntropicGodHand[] Hands = new EntropicGodHand[2];

        public int CurrentPhase
        {
            get;
            set;
        }

        public int PhaseCycleIndex
        {
            get;
            set;
        }

        public int PortalChainDashCounter
        {
            get;
            set;
        }

        public int BrainFogChargeCounter
        {
            get;
            set;
        }

        public int FightLength
        {
            get;
            set;
        }

        public float LaserSpinDirection
        {
            get;
            set;
        }

        public float LaserTelegraphOpacity
        {
            get;
            set;
        }

        public float LaserSquishFactor
        {
            get;
            set;
        }

        public float LaserLengthFactor
        {
            get;
            set;
        }

        public float FogIntensity
        {
            get;
            set;
        }

        public float FogSpreadDistance
        {
            get;
            set;
        }

        public float HeadSquishiness
        {
            get;
            set;
        }

        public float EyeGleamInterpolant
        {
            get;
            set;
        }

        public float BigEyeOpacity
        {
            get;
            set;
        }

        public Vector2 TeleportPosition
        {
            get;
            set;
        }

        public Vector2 TeleportDirection
        {
            get;
            set;
        }

        public Vector2 PortalArcSpawnCenter
        {
            get;
            set;
        }

        public Vector3 LaserRotation
        {
            get;
            set;
        }

        public float LifeRatio => NPC.life / (float)NPC.lifeMax;

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

        public NPCAimedTarget Target => NPC.GetTargetData();

        public Color GeneralColor => Color.Lerp(Color.White, Color.Black, Clamp(Abs(ZPosition) * 0.35f, 0f, 1f));

        public EntropicGodAttackType CurrentAttack
        {
            get => (EntropicGodAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float SpinAngularOffset => ref NPC.ai[2];

        public ref float ZPosition => ref NPC.ai[3];

        public ref float TeleportVisualsInterpolant => ref NPC.localAI[0];

        public ref float ChargeAfterimageInterpolant => ref NPC.localAI[1];

        public ref float HeadRotation => ref NPC.localAI[2];

        public Vector2 TeleportVisualsAdjustedScale
        {
            get
            {
                float maxStretchFactor = 1.3f;
                Vector2 scale = Vector2.One * NPC.scale;
                if (TeleportVisualsInterpolant > 0f)
                {
                    // 1. Horizontal stretch.
                    if (TeleportVisualsInterpolant <= 0.166f)
                    {
                        float localInterpolant = InverseLerp(0f, 0.166f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, maxStretchFactor, Convert01To010(localInterpolant));
                        scale.Y *= Lerp(1f, 0.2f, Convert01To010(localInterpolant));
                    }

                    // 2. Vertical stretch.
                    else if (TeleportVisualsInterpolant <= 0.333f)
                    {
                        float localInterpolant = InverseLerp(0.166f, 0.333f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, 0.2f, Convert01To010(localInterpolant));
                        scale.Y *= Lerp(1f, maxStretchFactor, Convert01To010(localInterpolant));
                    }

                    // 3. Shrink into nothing on both axes.
                    else if (TeleportVisualsInterpolant <= 0.5f)
                    {
                        float localInterpolant = InverseLerp(0.333f, 0.5f, TeleportVisualsInterpolant);
                        scale *= Pow(1f - localInterpolant, 4f);
                    }

                    // 4. Return to normal scale, use vertical overshoot at the end.
                    else
                    {
                        float localInterpolant = InverseLerp(0.5f, 0.73f, TeleportVisualsInterpolant);

                        // 1.17234093 = 1 / sin(1.8)^6, acting as a correction factor to ensure that the final scale in the sinusoidal overshoot is one.
                        float verticalScaleOvershot = Pow(Sin(localInterpolant * 1.8f), 6f) * 1.17234093f;
                        scale.X = localInterpolant;
                        scale.Y = verticalScaleOvershot;
                    }
                }
                return scale;
            }
        }

        public LoopedSoundInstance AmbienceLoop
        {
            get;
            private set;
        }

        public bool ShouldDrawBehindTiles => ZPosition >= 0.2f;

        public Vector2 HeadOffset => -Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale * 60f;

        public static NPC Myself
        {
            get
            {
                if (myself is not null && !myself.active)
                    return null;

                return myself;
            }
            private set => myself = value;
        }

        public static int CometDamage => Main.expertMode ? 425 : 275;

        public static int FireballDamage => Main.expertMode ? 400 : 250;

        public static int NoxusGasDamage => Main.expertMode ? 425 : 275;

        public static int SpikeDamage => Main.expertMode ? 400 : 250;

        public static int ExplosionDamage => Main.expertMode ? 450 : 300;

        public static int NightmareDeathrayDamage => Main.expertMode ? 750 : 480;

        public static int DebuffDuration_RegularAttack => SecondsToFrames(5f);

        public static int DebuffDuration_PowerfulAttack => SecondsToFrames(10f);

        public static int IdealFightDuration => SecondsToFrames(180f);

        public static float MaxTimedDRDamageReduction => 0.45f;

        public static readonly Vector2 DefaultHandOffset = new(226f, 108f);

        public static readonly SoundStyle AmbienceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/Ambience") with { Volume = 0.9f };

        // Used during the migraine stun behavior.
        public static readonly SoundStyle BrainRotSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/BrainRot");

        public static readonly SoundStyle ClapSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/Clap") with { Volume = 1.5f };

        public static readonly SoundStyle ExplosionSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/Explosion");

        public static readonly SoundStyle ExplosionTeleportSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/ExplosionTeleport");

        public static readonly SoundStyle FireballShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/FireballShoot") with { Volume = 0.65f, MaxInstances = 20 };

        public static readonly SoundStyle HitSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCHit/NoxusHurt") with { PitchVariance = 0.4f, Volume = 0.5f };

        public static readonly SoundStyle JumpscareSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/Jumpscare");

        public static readonly SoundStyle NightmareDeathrayShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NightmareDeathray") with { Volume = 1.56f };

        public static readonly SoundStyle ScreamSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/Scream") with { Volume = 0.45f, MaxInstances = 20 };

        public const int DefaultTeleportDelay = 22;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.25f;

        public IEnumerable<float> PhaseThresholdLifeRatios
        {
            get
            {
                yield return Phase2LifeRatio;
                yield return Phase3LifeRatio;
            }
        }

        public const float DefaultDR = 0.23f;
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

            // Hey bozo the player's gone. Leave.
            if (Target.Invalid)
                NPC.active = false;

            // Grant the target infinite flight and ensure that they receive the boss effects buff.
            if (NPC.HasPlayerTarget)
            {
                Player playerTarget = Main.player[NPC.target];
                playerTarget.wingTime = playerTarget.wingTimeMax;
                playerTarget.GrantInfiniteFlight();

                playerTarget.GrantBossEffectsBuff();
            }

            // Take damage from NPCs and projectiles if fighting the Nameless deity.
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = NPC.HasNPCTarget;
            if (NPC.HasNPCTarget && NPC.position.Y <= 1600f)
                NPC.position.Y = Main.maxTilesY * 16f - 250f;
            if (NPC.HasNPCTarget && NPC.position.Y >= Main.maxTilesY * 16f - 900f)
                NPC.position.Y = 2300f;

            if (NPC.HasNPCTarget && NPC.position.X <= 400f)
                NPC.position.X = Main.maxTilesX * 16f - 900f;
            if (NPC.HasNPCTarget && NPC.position.X >= Main.maxTilesX * 16f + 400f)
                NPC.position.X = 900f;

            NPC.takenDamageMultiplier = NPC.HasNPCTarget ? 50f : 1f;
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.Glock)
                NPC.takenDamageMultiplier = 2500f;

            // Disable rain.
            Main.StopRain();

            // Update ambience.
            if (AmbienceLoop is null || AmbienceLoop.HasBeenStopped)
            {
                AmbienceLoop = LoopedSoundManager.CreateNew(AmbienceSound, () =>
                    {
                        return !NPC.active || CurrentAttack == EntropicGodAttackType.DeathAnimation;
                    });
            }
            AmbienceLoop.Update(Main.LocalPlayer.Center, sound =>
            {
                float idealVolume = Pow(NoxusSky.WindSpeedFactor, 1.62f) * 0.8f;
                if (sound.Volume != idealVolume)
                    sound.Volume = idealVolume;
            });

            // Set the global NPC instance.
            Myself = NPC;

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            NPC.SetDR(DefaultDR);

            // Make hands by default close and not use a rotation override.
            for (int i = 0; i < Hands.Length; i++)
            {
                Hands[i].ShouldOpen = false;
                Hands[i].RotationOverride = null;
            }

            // Make the head spin back into place.
            HeadRotation = HeadRotation.AngleTowards(0f, 0.02f);
            HeadSquishiness = Clamp(HeadSquishiness - 0.02f, 0f, 0.5f);

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Make the charge afterimage interpolant dissipate.
            ChargeAfterimageInterpolant = Clamp(ChargeAfterimageInterpolant * 0.98f - 0.02f, 0f, 1f);

            // Make the laser telegraph opacity dissipate. This is useful for cases where Noxus changes phases in the middle of the telegraph being prepared.
            LaserTelegraphOpacity = Clamp(LaserTelegraphOpacity - 0.01f, 0f, 1f);

            switch (CurrentAttack)
            {
                case EntropicGodAttackType.DarkExplosionCharges:
                    DoBehavior_DarkExplosionCharges();
                    break;
                case EntropicGodAttackType.DarkEnergyBoltHandWave:
                    DoBehavior_DarkEnergyBoltHandWave();
                    break;
                case EntropicGodAttackType.FireballBarrage:
                    DoBehavior_FireballBarrage();
                    break;
                case EntropicGodAttackType.RealityWarpSpinCharge:
                    DoBehavior_RealityWarpSpinCharge();
                    break;
                case EntropicGodAttackType.OrganizedPortalCometBursts:
                    DoBehavior_OrganizedPortalCometBursts();
                    break;
                case EntropicGodAttackType.HoveringHandGasBursts:
                    DoBehavior_HoveringHandGasBursts();
                    break;
                case EntropicGodAttackType.RapidExplosiveTeleports:
                    DoBehavior_RapidExplosiveTeleports();
                    break;
                case EntropicGodAttackType.Phase2Transition:
                    DoBehavior_Phase2Transition();
                    break;
                case EntropicGodAttackType.GeometricSpikesTeleportAndFireballs:
                    DoBehavior_GeometricSpikesTeleportAndFireballs();
                    break;
                case EntropicGodAttackType.TeleportAndShootNoxusGas:
                    DoBehavior_TeleportAndShootNoxusGas();
                    break;
                case EntropicGodAttackType.ThreeDimensionalNightmareDeathRay:
                    DoBehavior_ThreeDimensionalNightmareDeathRay();
                    break;
                case EntropicGodAttackType.Phase3Transition:
                    DoBehavior_Phase3Transition();
                    break;
                case EntropicGodAttackType.BrainFogAndThreeDimensionalCharges:
                    DoBehavior_BrainFogAndThreeDimensionalCharges();
                    break;
                case EntropicGodAttackType.PortalChainCharges:
                    DoBehavior_PortalChainCharges();
                    break;
                case EntropicGodAttackType.PortalChainCharges2:
                    DoBehavior_PortalChainCharges2();
                    break;
                case EntropicGodAttackType.MigraineAttack:
                    DoBehavior_MigraineAttack();
                    break;
                case EntropicGodAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation();
                    break;
            }

            // Handle phase transition triggers.
            PreparePhaseTransitionsIfNecessary();

            // Update all hands.
            UpdateHands();

            // Perform Z position visual effects.
            PerformZPositionEffects();

            // Disable damage when invisible.
            if (NPC.Opacity <= 0.35f)
            {
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
            }

            // Rotate slightly based on horizontal movement.
            bool teleported = NPC.position.Distance(NPC.oldPosition) >= 80f;
            NPC.rotation = Clamp((NPC.position.X - NPC.oldPosition.X) * 0.0024f, -0.16f, 0.16f);
            if (teleported)
                NPC.rotation = 0f;

            // Emit pitch black metaballs around based on movement.
            else if (NPC.Opacity >= 0.5f)
            {
                int metaballSpawnLoopCount = (int)Utils.Remap(NPC.Opacity, 1f, 0f, 9f, 1f) - (int)Utils.Remap(ZPosition, 0.1f, 1.2f, 0f, 5f);

                for (int i = 0; i < metaballSpawnLoopCount; i++)
                {
                    Vector2 gasSpawnPosition = NPC.Center + Main.rand.NextVector2Circular(82f, 82f) * TeleportVisualsAdjustedScale + (NPC.position - NPC.oldPosition).SafeNormalize(Vector2.UnitY) * 3f;
                    float gasSize = NPC.width * TeleportVisualsAdjustedScale.X * NPC.Opacity * 0.45f;
                    float angularOffset = Sin(Main.GlobalTimeWrappedHourly * 1.1f) * 0.77f;
                    ModContent.GetInstance<PitchBlackMetaball>().CreateParticle(gasSpawnPosition, Main.rand.NextVector2Circular(2f, 2f) + NPC.velocity.RotatedBy(angularOffset).RotatedByRandom(0.6f) * 0.26f, gasSize);
                }
            }

            // Gain permanent afterimages if in phase 2 and onward.
            if (CurrentPhase >= 1)
                ChargeAfterimageInterpolant = 1f;

            // Set the keyboard shader's eye intensity.
            EyeBrightness = BigEyeOpacity;

            // Increment timers.
            AttackTimer++;
            FightLength++;
        }

        public void TargetClosest()
        {
            NPCUtils.TargetSearchResults targetSearchResults = NPCUtils.SearchForTarget(NPC, NPCUtils.TargetSearchFlag.NPCs | NPCUtils.TargetSearchFlag.Players, _ => NamelessDeityBoss.Myself is null, NamelessSearchCheck);
            if (!targetSearchResults.FoundTarget)
                return;

            // Check for players. Prioritize the Nameless Deity if he's present.
            int targetIndex = targetSearchResults.NearestTargetIndex;
            if (targetSearchResults.FoundNPC)
                targetIndex = targetSearchResults.NearestNPCIndex + 300;

            NPC.target = targetSearchResults.NearestTargetIndex;
            NPC.targetRect = targetSearchResults.NearestTargetHitbox;
        }

        public static bool NamelessSearchCheck(NPC npc)
        {
            return npc.type == ModContent.NPCType<NamelessDeityBoss>() && npc.Opacity > 0f && !npc.immortal && !npc.dontTakeDamage;
        }
        #endregion AI
    }
}
