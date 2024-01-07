using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static readonly SoundStyle AppearSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Appear") with { Volume = 1.2f };

        public static readonly SoundStyle ChantSoundLooped = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ChantLoop", 2) with { Volume = 1.1f };

        public static readonly SoundStyle ChuckleSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Chuckle") with { Volume = 1.6f };

        public static readonly SoundStyle ClockStrikeSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ClockStrike") with { Volume = 1.35f };

        public static readonly SoundStyle ClockTickSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ClockTick") with { Volume = 1.1f, IsLooped = true };

        public static readonly SoundStyle ClockTickSoundReversed = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ClockTickReversed") with { Volume = 1.1f, IsLooped = true };

        public static readonly SoundStyle ComicalExplosionDeathSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCKilled/DeltaruneExplosion") with { Volume = 1.4f };

        public static readonly SoundStyle CosmicLaserStartSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/CosmicLaserStart") with { Volume = 1.2f };

        public static readonly SoundStyle CosmicLaserLoopSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/CosmicLaserLoop") with { Volume = 1.2f };

        public static readonly SoundStyle CosmicLaserObliterationSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/CosmicBeamObliteration") with { Volume = 3f };

        public static readonly SoundStyle DoNotVoiceActedSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/DoNotVocals", 2) with { Volume = 1.32f };

        public static readonly SoundStyle EyeRollSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/EyeRoll") with { Volume = 1.2f };

        public static readonly SoundStyle FastHandMovementSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/FastHandMovement") with { Volume = 1.25f };

        public static readonly SoundStyle FingerSnapSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/FingerSnap") with { Volume = 1.4f };

        public static readonly SoundStyle FireBeamShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SunBeamShoot") with { Volume = 1.05f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest, PitchVariance = 0.12f };

        public static readonly SoundStyle GalaxyTelegraphSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/GalaxyTelegraph") with { MaxInstances = 10, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public static readonly SoundStyle GalaxyFallSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/GalaxyFallGlitch", 6) with { MaxInstances = 20, Volume = 0.56f };

        public static readonly SoundStyle GalaxyExplodeSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/GalaxyImpact", 4) with { Volume = 0.85f, MaxInstances = 15 };

        public static readonly SoundStyle GameBreakSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/GameBreak") with { Volume = 1.1f };

        public static readonly SoundStyle GenericBurstSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/GenericBurst") with { Volume = 1.3f, PitchVariance = 0.15f };

        public static readonly SoundStyle GFBDeathSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCKilled/NamelessDeityFuckingDies_GFB") with { Volume = 1.5f };

        public static readonly SoundStyle GrazeSound = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/DaggerGraze", 2);

        public static readonly SoundStyle GrazeSoundEcho = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/DaggerGrazeEcho", 2);

        public static readonly SoundStyle HeartbeatSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Heartbeat") with { Volume = 1.75f };

        public static readonly SoundStyle HitSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCHit/NamelessDeityHurt") with { PitchVariance = 0.32f, Volume = 0.67f };

        public static readonly SoundStyle HummSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/HummLoop") with { Volume = 0.4f, IsLooped = true };

        public static readonly SoundStyle IntroDroneSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/IntroDrone") with { Volume = 1.5f };

        public static readonly SoundStyle IntroSuspenseSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/IntroSuspenseBuild") with { Volume = 1.5f };

        public static readonly SoundStyle IntroScreenSliceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/IntroScreenSlice") with { Volume = 1.2f };

        public static readonly SoundStyle JermaImKillingYouSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/JermaImKillingYou") with { Volume = 1.5f };

        public static readonly SoundStyle LowQualityGunShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/LowQualityGunShootSound") with { Volume = 0.96f, MaxInstances = 10 };

        public static readonly SoundStyle MomentOfCreationSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/MomentOfCreation") with { Volume = 2f };

        public static readonly SoundStyle MumbleSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Mumble", 5) with { Volume = 0.89f };

        public static readonly SoundStyle NotActuallyAnEagleSound = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/NotActuallyAnEagleSound");

        public static readonly SoundStyle Phase3TransitionSound = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Phase3Transition");

        public static readonly SoundStyle PortalCastSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/PortalCast") with { Volume = 1.2f };

        public static readonly SoundStyle PortalLaserShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/PortalLaserShoot") with { Volume = 1.32f, PitchVariance = 0.1f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest };

        public static readonly SoundStyle QuasarSoundLooped = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/QuasarLoop") with { Volume = 1.2f };

        public static readonly SoundStyle QuasarSoundLooped_Start = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/QuasarLoopStart") with { Volume = 1.2f };

        public static readonly SoundStyle RealityCrackSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/RealityCrack", 3) with { Volume = 1.32f, PitchVariance = 0.16f };

        public static readonly SoundStyle RealityTearSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/RealityTear") with { Volume = 1.7f, PitchVariance = 0.24f, MaxInstances = 10 };

        public static readonly SoundStyle ScreamSoundDistant = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamDistant");

        public static readonly SoundStyle ScreamSoundDistorted = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamDistorted", 2);

        public static readonly SoundStyle ScreamSoundLong = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamLong") with { Volume = 1.2f };

        public static readonly SoundStyle ScreamSoundLooped = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamLoop");

        public static readonly SoundStyle ScreamSoundLooped_Start = new("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamLoopStart");

        public static readonly SoundStyle ScreamSoundShort = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreamShort") with { Volume = 1.05f, MaxInstances = 20 };

        public static readonly SoundStyle ScreenTearSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScreenTear");

        public static readonly SoundStyle SliceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SliceTelegraph") with { Volume = 1.05f, MaxInstances = 20 };

        public static readonly SoundStyle StandingInLightSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/StandingInLight") with { Volume = 1.15f };

        public static readonly SoundStyle StarConvergenceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/StarConvergence") with { Volume = 0.8f };

        public static readonly SoundStyle StarConvergenceFastSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/StarConvergenceFast") with { Volume = 0.8f };

        public static readonly SoundStyle StarCrushSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/StarCrush") with { Volume = 1.4f };

        public static readonly SoundStyle StarRecedeSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/StarRecede") with { Volume = 1.2f };

        public static readonly SoundStyle SuddenMovementSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SuddenMovement") with { Volume = 0.95f, MaxInstances = 10 };

        public static readonly SoundStyle SunFireballShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SunFireballShootSound") with { Volume = 1.05f, MaxInstances = 5 };

        public static readonly SoundStyle SunFireballShootSoundReversed = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SunFireballShootSoundReversed") with { Volume = 1.05f, MaxInstances = 5 };

        public static readonly SoundStyle SwordSlashSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/SwordSlash") with { Volume = 1.3f, MaxInstances = 4 };

        public static readonly SoundStyle SupernovaSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Supernova") with { Volume = 0.8f, MaxInstances = 20 };

        public static readonly SoundStyle Supernova2Sound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/Supernova2") with { Volume = 1.5f, MaxInstances = 10 };

        public static readonly SoundStyle WingFlapSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/WingFlap", 4) with { Volume = 1.6f };
    }
}
