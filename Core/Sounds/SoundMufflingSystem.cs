using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.Subworlds;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class SoundMufflingSystem : ModSystem
    {
        public static float MuffleFactor
        {
            get;
            set;
        } = 1f;

        public static float EarRingingIntensity
        {
            get;
            set;
        }

        public static List<SoundStyle> ExemptedSoundStyles => new()
        {
            GlitchSound,
            MediumBloodSpillSound,
            EarRingingSound,

            NamelessDeityBoss.ChantSoundLooped,
            NamelessDeityBoss.CosmicLaserStartSound,
            NamelessDeityBoss.CosmicLaserLoopSound,
            NamelessDeityBoss.CosmicLaserObliterationSound,
            NamelessDeityBoss.JermaImKillingYouSound,
            NamelessDeityBoss.Phase3TransitionSound
        };

        public override void OnModLoad()
        {
            On_SoundPlayer.Play_Inner += ReduceVolume;
        }

        public override void PreUpdateEntities()
        {
            MuffleFactor = Lerp(MuffleFactor, 1f, 0.013f);
            if (MuffleFactor >= 0.999f)
                MuffleFactor = 1f;
        }

        private SlotId ReduceVolume(On_SoundPlayer.orig_Play_Inner orig, SoundPlayer self, ref SoundStyle style, Vector2? position, SoundUpdateCallback updateCallback)
        {
            SoundStyle copy = style;

            if (NamelessDeityBoss.Myself is null)
                MuffleFactor = 1f;
            if (MuffleFactor < 0.999f && !ExemptedSoundStyles.Any(s => s.IsTheSameAs(copy)) && EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame && NamelessDeityBoss.Myself is not null)
                style.Volume *= MuffleFactor;

            SlotId result = orig(self, ref style, position, updateCallback);
            style.Volume = copy.Volume;

            return result;
        }
    }
}
