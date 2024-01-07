using Terraria.Audio;

namespace NoxusBoss.Assets
{
    public static class CommonSoundsRegistry
    {
        public static readonly SoundStyle EarRingingSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/EarRinging") with { Volume = 0.56f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 };

        public static readonly SoundStyle GlitchSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/Glitch") with { PitchVariance = 0.2f, Volume = 1.3f, MaxInstances = 8 };

        public static readonly SoundStyle LargeBloodSpillSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/LargeBloodSpill") with { PitchVariance = 0.05f };

        public static readonly SoundStyle MediumBloodSpillSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/MediumBloodSpill") with { PitchVariance = 0.05f };

        public static readonly SoundStyle PulseSound = new("NoxusBoss/Assets/Sounds/Custom/Common/BossRushPulse");

        public static readonly SoundStyle ShatterSound = new("NoxusBoss/Assets/Sounds/Custom/Common/ScreenShatter");

        public static readonly SoundStyle TeleportInSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/TeleportIn") with { Volume = 0.65f, MaxInstances = 5, PitchVariance = 0.16f };

        public static readonly SoundStyle TeleportOutSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/TeleportOut") with { Volume = 0.65f, MaxInstances = 5, PitchVariance = 0.16f };

        public static readonly SoundStyle TwinkleSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Common/Twinkle") with { MaxInstances = 5, PitchVariance = 0.16f };
    }
}
