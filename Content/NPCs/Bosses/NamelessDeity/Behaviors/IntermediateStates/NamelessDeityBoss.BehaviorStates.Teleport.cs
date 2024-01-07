using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public bool ShouldStartTeleportAnimation
        {
            get;
            set;
        }

        public int TeleportInTime
        {
            get;
            set;
        }

        public int TeleportOutTime
        {
            get;
            set;
        }

        public Func<Vector2> TeleportDestination
        {
            get;
            set;
        }

        public const int DefaultTeleportTime = 20;

        public void LoadStateTransitions_Teleport()
        {
            // Slow down.
            NPC.velocity *= 0.82f;

            // All any attack to at any time raise the ShouldStartTeleportAnimation flag to start a teleport.
            // Once the teleport concludes, the previous attack is returned to where it was before.
            ApplyToAllStatesWithCondition(state =>
            {
                StateMachine.RegisterTransition(state, NamelessAIType.Teleport, true, () => ShouldStartTeleportAnimation, () =>
                {
                    ShouldStartTeleportAnimation = false;
                    AttackTimer = 0;
                });
            }, _ => true);

            // Return to the original attack after the teleport.
            StateMachine.RegisterTransition(NamelessAIType.Teleport, null, false, () =>
            {
                return AttackTimer >= TeleportInTime + TeleportOutTime;
            }, () =>
            {
                TeleportInTime = DefaultTeleportTime / 2;
                TeleportOutTime = DefaultTeleportTime / 2;
                TeleportVisualsInterpolant = 0f;
            });
        }

        public void DoBehavior_Teleport()
        {
            // Manipulate the teleport visuals.
            TeleportVisualsInterpolant = InverseLerp(0f, TeleportInTime, AttackTimer) * 0.5f + InverseLerp(0f, TeleportOutTime, AttackTimer - TeleportInTime) * 0.5f;

            // Play the teleport in sound.
            if (AttackTimer == 1)
                SoundEngine.PlaySound(TeleportInSound);

            // Update the position and create teleport visuals once the teleport is at the midpoint of the animation.
            if (AttackTimer == TeleportInTime)
                ImmediateTeleportTo(TeleportDestination());

            // Disable damage.
            NPC.damage = 0;
        }
    }
}
