using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class LoopedSoundManager : ModSystem
    {
        private static readonly List<LoopedSoundInstance> loopedSounds = new();

        public override void OnModLoad()
        {
            On_SoundEngine.Update += UpdateLoopedSounds;
        }

        private void UpdateLoopedSounds(On_SoundEngine.orig_Update orig)
        {
            if (!SoundEngine.IsAudioSupported)
                return;

            // Go through all looped sounds and perform automatic cleanup.
            loopedSounds.RemoveAll(s =>
            {
                // If the sound was started but is no longer playing, restart it.
                bool shouldBeRemoved = false;
                if (s.HasLoopSoundBeenStarted && !s.IsBeingPlayed)
                    s.Restart();

                // If the sound's termination condition has been activated, remove the sound.
                if (s.AutomaticTerminationCondition())
                    shouldBeRemoved = true;

                // If the sound has been stopped, remove it.
                if (s.HasBeenStopped)
                    shouldBeRemoved = true;

                // If the sound will be removed, mark it as stopped.
                if (shouldBeRemoved)
                    s.Stop();

                return shouldBeRemoved;
            });

            orig();
        }

        public static LoopedSoundInstance CreateNew(SoundStyle loopingSound, Func<bool> automaticTerminationCondition = null)
        {
            LoopedSoundInstance sound = new(loopingSound, automaticTerminationCondition ?? (() => false));
            loopedSounds.Add(sound);

            return sound;
        }

        public static LoopedSoundInstance CreateNew(SoundStyle startingSound, SoundStyle loopingSound, Func<bool> automaticTerminationCondition = null)
        {
            LoopedSoundInstance sound = new(startingSound, loopingSound, automaticTerminationCondition ?? (() => false));
            loopedSounds.Add(sound);

            return sound;
        }
    }
}
