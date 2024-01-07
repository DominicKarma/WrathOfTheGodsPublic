using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Music
{
    public class MusicVolumeManipulationSystem : ModSystem
    {
        public static float MuffleFactor
        {
            get;
            set;
        } = 1f;

        public static bool MusicIsPaused
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            On_Main.UpdateAudio += MakeMusicShutUp;
        }

        private void MakeMusicShutUp(On_Main.orig_UpdateAudio orig, Main self)
        {
            if (Main.gameMenu)
                MuffleFactor = 1f;

            if (MuffleFactor >= 0.01f)
                orig(self);

            if (MuffleFactor <= 0.9999f)
            {
                for (int i = 0; i < Main.musicFade.Length; i++)
                {
                    float volume = Main.musicFade[i] * Main.musicVolume * Clamp(MuffleFactor, 0f, 1f);
                    float tempFade = Main.musicFade[i];

                    if (volume <= 0f && tempFade <= 0f)
                        continue;

                    for (int j = 0; j < 50; j++)
                    {
                        Main.audioSystem.UpdateCommonTrackTowardStopping(i, volume, ref tempFade, Main.musicFade[i] >= 0.5f);
                        Main.musicFade[i] = tempFade;
                    }
                }
                Main.audioSystem.UpdateAudioEngine();

                // Make the music muffle factor naturally dissipate.
                if (Main.instance.IsActive && !Main.gamePaused)
                    MuffleFactor = Clamp(MuffleFactor * 1.03f + 0.03f, 0f, 1f);

                if (MusicIsPaused)
                {
                    Main.audioSystem.ResumeAll();
                    MusicIsPaused = false;
                }
            }

            if (MuffleFactor <= 0.11f)
            {
                Main.audioSystem.PauseAll();
                MusicIsPaused = true;
            }
        }
    }
}
