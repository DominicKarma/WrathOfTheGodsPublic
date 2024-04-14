using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Netcode;
using NoxusBoss.Core.Netcode.Packets;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public partial class NamelessDeityInfiniteStairwayManager : ModSystem
    {
        public const string WitnessedGardenVisionFieldName = "HasWitnessedGardenVision";

        public static bool CanStart
        {
            get
            {
                // This effect can only happen at night when the player is on the surface.
                if (Main.dayTime || !Main.LocalPlayer.ZoneOverworldHeight)
                    return false;

                // This effect can only happen once Noxus is able to fall from space.
                if (!NoxusEggCutsceneSystem.NoxusCanCommitSkydivingFromSpace)
                    return false;

                // This effect cannot happen more than once.
                if (Main.LocalPlayer.GetValueRef<bool>(WitnessedGardenVisionFieldName))
                    return false;

                // This effect cannot happen if the player has already fought Noxus.
                if (WorldSaveSystem.HasDefeatedNoxusEgg || WorldSaveSystem.HasDefeatedNoxus)
                    return false;

                // This effect cannot happen after Nameless has already been defeated, as it wouldn't make much logical sense.
                // This should never happen naturally, and the player should witness the cutscene long before they can begin Nameless' fight, but still.
                if (WorldSaveSystem.HasDefeatedNamelessDeity)
                    return false;

                // This effect cannot happen in subworlds, as it would break completely.
                if (SubworldSystem.AnyActive())
                    return false;

                // This effect cannot happen if any bosses or events are present.
                if (AnyBosses() || AnyInvasionsOrEvents())
                    return false;

                // This effect cannot happen if the player is concerningly close to the world edges or top, because the player needs space to
                // go up the stairs.
                float x = Main.LocalPlayer.Center.X;
                float y = Main.LocalPlayer.Center.Y;
                if (x <= 5000f || x >= Main.maxTilesX * 16f - 5000f || y <= 3600f)
                    return false;

                // Lastly, this effect cannot happen unless the player is in an extremely open area.
                return HasEnteredEmptySpace;
            }
        }

        private static void SaveDefeatStateForPlayer(NoxusPlayer p, TagCompound tag)
        {
            tag[WitnessedGardenVisionFieldName] = p.GetValueRef<bool>(WitnessedGardenVisionFieldName).Value;
        }

        private static void LoadDefeatStateForPlayer(NoxusPlayer p, TagCompound tag)
        {
            p.GetValueRef<bool>(WitnessedGardenVisionFieldName).Value = tag.TryGet(WitnessedGardenVisionFieldName, out bool result) && result;
        }

        private void DisableSpawnsWhenStairIsActive(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (StairwayIsVisible)
            {
                spawnRate = 0;
                maxSpawns = 0;
            }
        }

        private void UpdateEffect(NoxusPlayer p)
        {
            // Randomly start the event if possible.
            if (Main.rand.NextBool(3200) && CanStart && Main.myPlayer == p.Player.whoAmI)
                Start(Main.myPlayer);

            // Reset things if the effect is not active.
            if (!StairwayIsVisible)
            {
                GardenStartX = 0f;
                DistanceMoved = 0f;
                PlayerPositionBeforeAnimationBegan = Vector2.Zero;
            }

            // Get rid of all falling stars. Their noises completely ruin the ambience.
            else
                Star.starfallBoost = 0f;

            // Disallow this effect in subworlds.
            if (SubworldSystem.AnyActive())
            {
                Opacity = 0f;
                return;
            }

            // Make the step sound go down.
            StepSoundCountdown = Utils.Clamp(StepSoundCountdown - 1, 0, 120);

            // Keep track of how much the player is moving if the effect is active.
            bool pastStartingPoint = (StairsDirection == -1f && Main.LocalPlayer.position.X < StairwayStart.X - 3600f) || (StairsDirection == 1f && Main.LocalPlayer.position.X > StairwayStart.X + 3600f);
            if (StairwayIsVisible && Sign(Main.LocalPlayer.velocity.X) == StairsDirection && pastStartingPoint)
                DistanceMoved += Abs(Main.LocalPlayer.velocity.X);

            // Make the stairs fade in if they're already present.
            if (Opacity >= 0.01f)
            {
                Opacity = Clamp(Opacity + 0.05f, 0f, 1f);
                if (Opacity < 0.9f)
                    TotalScreenOverlaySystem.OverlayInterpolant = 1f;
            }

            // Disable map overlay bugs.
            if (StairwayIsVisible)
                Main.mapStyle = 0;

            // Perform an infinite looping effect.
            PerformInfiniteLoopCheck();

            // Kill pets and hooks. They are not necessary for the overall aesthetic.
            KillPlayerPetsAndHooks();

            // Define the start of the garden after the player has moved quite a bit.
            float desiredGardenStartX = StairwayStart.X + StairsDirection * 11100f;
            if (GardenStartX == 0f && Distance(Main.LocalPlayer.Center.X, desiredGardenStartX) >= 1200f && DistanceMoved >= DistanceNeededForMaxBrightess * 1.2f)
                GardenStartX = desiredGardenStartX;

            // Stop the player from moving if they're past the point of the garden, and start the special animation.
            bool pastGarden = (StairsDirection == -1f && Main.LocalPlayer.position.X < GardenStartX - 96f) || (StairsDirection == 1f && Main.LocalPlayer.position.X > GardenStartX + 96f);
            if (GardenStartX != 0f && pastGarden && !NamelessDeityInfiniteStairwayTopAnimationManager.AnimationActive)
            {
                NamelessDeityInfiniteStairwayTopAnimationManager.AnimationTimer = 1;
                BlockerSystem.Start(true, ModReferences.CalamityRemix is null, () => NamelessDeityInfiniteStairwayTopAnimationManager.AnimationActive);
                Main.LocalPlayer.velocity.X = 0f;
            }
        }

        public static void Start(int playerIndex)
        {
            // Disallow "starting" the effect when it's already active.
            if (StairwayIsVisible)
                return;

            Opacity = 0.01f;
            StairwayStart = Main.LocalPlayer.Center + Vector2.UnitX * StairsDirection * 1100f;
            if (StairwayStart.Y < 6000f)
                StairwayStart = new(StairwayStart.X, 6000f);

            PlayerPositionBeforeAnimationBegan = Main.LocalPlayer.Center;

            // Mark the player as having witnessed the event.
            Main.LocalPlayer.GetValueRef<bool>(WitnessedGardenVisionFieldName).Value = true;

            // Inform everyone else that this player has been to experience the vision if this is multiplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                PacketManager.SendPacket<PlayerStairwayVisionStartPacket>(playerIndex, PlayerPositionBeforeAnimationBegan);

            SoundEngine.PlaySound(GlitchSound);
        }

        public static void Stop()
        {
            // Disallow "stopping" the effect when it's not on.
            if (!StairwayIsVisible)
                return;

            // Reset the opacity.
            Opacity = 0f;

            // Bring the player back to where they were before.
            if (PlayerPositionBeforeAnimationBegan != Vector2.Zero)
            {
                Main.LocalPlayer.Center = PlayerPositionBeforeAnimationBegan;
                while (!Collision.SolidCollision(Main.LocalPlayer.BottomLeft - Vector2.UnitY * 2f, Main.LocalPlayer.width, 4))
                    Main.LocalPlayer.position.Y += 2f;

                PlayerPositionBeforeAnimationBegan = Vector2.Zero;
            }

            // Ensure the teleport doesn't murder the player instantly due to fall damage.
            Main.LocalPlayer.fallStart = (int)(Main.LocalPlayer.Top.Y / 16f);

            // Make the player blink when waking up.
            Main.LocalPlayer.eyeHelper.BlinkBecausePlayerGotHurt();

            // Reset star fall rates.
            Star.starfallBoost = 1f;

            // Inform everyone else that this player has stopped experiencing the vision if this is multiplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                PacketManager.SendPacket<PlayerStairwayVisionEndPacket>(Main.myPlayer);
        }
    }
}
