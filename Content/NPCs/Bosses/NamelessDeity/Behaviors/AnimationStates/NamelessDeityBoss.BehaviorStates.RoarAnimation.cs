using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static int RoarAnimation_ScreamTime => 210;

        public void LoadStateTransitions_RoarAnimation()
        {
            // Load the transition from RoarAnimation to the typical cycle.
            StateMachine.RegisterTransition(NamelessAIType.RoarAnimation, NamelessAIType.ResetCycle, false, () =>
            {
                return AttackTimer >= RoarAnimation_ScreamTime;
            });
        }

        public void DoBehavior_RoarAnimation()
        {
            // Appear on the foreground.
            if (AttackTimer == 1f)
            {
                NPC.Center = Target.Center - Vector2.UnitY * 300f;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                SoundEngine.PlaySound(GenericBurstSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Bring the music.
            if (Music == 0 && AttackTimer >= RoarAnimation_ScreamTime - 10)
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/NamelessDeity");

            SceneEffectPriority = SceneEffectPriority.BossHigh;

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Jitter in place and scream.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(ScreamSoundLong with { Pitch = -0.075f });
            if (AttackTimer % 10f == 0f && AttackTimer <= RoarAnimation_ScreamTime - 75f)
            {
                Color burstColor = Main.rand.NextBool() ? Color.LightGoldenrodYellow : Color.Lerp(Color.White, Color.IndianRed, 0.7f);

                // Create blur and burst particle effects.
                ExpandingChromaticBurstParticle burst = new(EyePosition, Vector2.Zero, burstColor, 16, 0.1f);
                burst.Spawn();
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.3f;

                if (OverallShakeIntensity <= 11f)
                    StartShakeAtPoint(NPC.Center, 5f);
            }
            NPC.Center += Main.rand.NextVector2Circular(12.5f, 12.5f);

            // Become completely opaque.
            NPC.Opacity = 1f;

            // Disable incoming damage, to prevent the player taking away 10% of Nameless' health while he's not moving.
            NPC.dontTakeDamage = true;

            // Update universal hands.
            DefaultUniversalHandMotion();
        }
    }
}
