using System;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria.Audio;

namespace NoxusBoss.Core
{
    public class LoopedSoundInstance
    {
        private readonly SoundStyle? startSoundStyle;

        private readonly SoundStyle loopSoundStyle;

        // Useful for cases where a sound is emitted by an entity but should cease when that entity is dead.
        // I've had far too many headache inducing cases where looped sounds refuse to go away after their producer stopped existing.
        // This condition is checked in a central manager instead of in the Update method because if an entity is responsible for updating then naturally
        // it'll be too late for Update to cleanly dispose of this sound instance.
        public Func<bool> AutomaticTerminationCondition
        {
            get;
            private set;
        }

        public SlotId StartingSoundSlot
        {
            get;
            private set;
        }

        public SlotId LoopingSoundSlot
        {
            get;
            private set;
        }

        public bool UsesStartingSound => startSoundStyle is not null;

        public bool HasStartingSoundBeenStarted
        {
            get;
            private set;
        }

        public bool HasLoopSoundBeenStarted
        {
            get;
            private set;
        }

        public bool HasBeenStopped
        {
            get;
            internal set;
        }

        public bool IsBeingPlayed => SoundEngine.TryGetActiveSound(LoopingSoundSlot, out _);

        // This constructor should not be used manually. Rather, sound instances should be created via the LoopedSoundManager's utilities, since that ensures that the sound is
        // properly logged by the manager.
        internal LoopedSoundInstance(SoundStyle loopingSound, Func<bool> automaticTerminationCondition)
        {
            loopSoundStyle = loopingSound;
            AutomaticTerminationCondition = automaticTerminationCondition;
            LoopingSoundSlot = SlotId.Invalid;
            StartingSoundSlot = SlotId.Invalid;
        }

        internal LoopedSoundInstance(SoundStyle startingSound, SoundStyle loopingSound, Func<bool> automaticTerminationCondition) : this(loopingSound, automaticTerminationCondition)
        {
            startSoundStyle = startingSound;
        }

        public void Update(Vector2 soundPosition, Action<ActiveSound> soundUpdateStep = null)
        {
            // Start the sound if it hasn't been activated yet.
            // If a starting sound should be used, play that first, and wait for it to end before playing the looping sound.
            if (!HasLoopSoundBeenStarted && !IsBeingPlayed)
            {
                bool waitingForStartingSoundToEnd = !HasStartingSoundBeenStarted || (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s) && s.IsPlaying);
                if (!UsesStartingSound)
                    waitingForStartingSoundToEnd = false;

                if (!waitingForStartingSoundToEnd)
                {
                    LoopingSoundSlot = SoundEngine.PlaySound(loopSoundStyle with { MaxInstances = 0, IsLooped = true }, soundPosition);
                    HasLoopSoundBeenStarted = true;
                    HasStartingSoundBeenStarted = true;
                }
                else if (UsesStartingSound && !HasStartingSoundBeenStarted)
                {
                    StartingSoundSlot = SoundEngine.PlaySound(startSoundStyle.Value with { MaxInstances = 0 }, soundPosition);
                    HasStartingSoundBeenStarted = true;
                }
            }

            // Keep the sound(s) updated.
            if (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s1))
            {
                s1.Position = soundPosition;
                soundUpdateStep?.Invoke(s1);
            }
            if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound s2))
            {
                s2.Position = soundPosition;
                soundUpdateStep?.Invoke(s2);
            }
            else if (!HasBeenStopped)
                HasLoopSoundBeenStarted = false;
        }

        public void Restart() => HasLoopSoundBeenStarted = false;

        public void Stop()
        {
            if (HasBeenStopped)
                return;

            if (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s1))
                s1?.Stop();
            if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound s2))
                s2?.Stop();

            HasBeenStopped = true;
        }
    }
}
