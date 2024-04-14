using System.Threading;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Capture;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class NoxusSprayPlayerDeletionSystem : ModSystem
    {
        public static bool PlayerWasDeleted
        {
            get;
            set;
        }

        public static bool PlayerWasDeletedByNamelessDeity
        {
            get;
            set;
        }

        public static bool PlayerWasDeletedByLaRuga
        {
            get;
            set;
        }

        public static Vector2 ScreenPositionAtPointOfDeletion
        {
            get;
            set;
        }

        public static int DeletionTimer
        {
            get;
            set;
        }

        public static int MainMenuReturnDelay
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_Main.UpdateAudio_DecideOnNewMusic += PlayLaRugaMusicIfNecessary;
        }

        private void PlayLaRugaMusicIfNecessary(On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self)
        {
            orig(self);
            if (PlayerWasDeletedByLaRuga && ModReferences.CalamityRemix is not null)
                Main.newMusic = MusicLoader.GetMusicSlot(ModReferences.CalamityRemix, "Sounds/Music/LaRuga");
        }

        public override void PostUpdateEverything()
        {
            if (PlayerWasDeleted)
            {
                Main.LocalPlayer.immuneAlpha = 255;
                Main.LocalPlayer.dead = true;
                Main.LocalPlayer.respawnTimer = 100;

                // Store the screen position at the time of the screen being deleted and disable player inputs/UI for the duration of the effect.
                if (DeletionTimer == 1)
                {
                    ScreenPositionAtPointOfDeletion = Main.screenPosition;
                    BlockerSystem.Start(true, true, () => PlayerWasDeleted);
                }

                DeletionTimer++;

                // Kick the player to the game menu after being gone for long enough.
                if (DeletionTimer >= 210)
                {
                    Main.menuMode = 10;
                    Main.gameMenu = true;
                    Main.hideUI = false;

                    if (PlayerWasDeletedByNamelessDeity)
                        NamelessDeityTipsOverrideSystem.UseSprayText = true;

                    ThreadPool.QueueUserWorkItem(new WaitCallback(context =>
                    {
                        int netMode = Main.netMode;
                        if (netMode == NetmodeID.SinglePlayer)
                            WorldFile.CacheSaveTime();

                        Main.invasionProgress = -1;
                        Main.invasionProgressDisplayLeft = 0;
                        Main.invasionProgressAlpha = 0f;
                        Main.invasionProgressIcon = 0;
                        Main.menuMode = 10;
                        Main.gameMenu = true;
                        SoundEngine.StopTrackedSounds();
                        if (PlayerWasDeletedByNamelessDeity)
                            SoundEngine.PlaySound(NamelessDeityBoss.DoNotVoiceActedSound);
                        MainMenuReturnDelay = 1;

                        CaptureInterface.ResetFocus();
                        Main.ActivePlayerFileData.StopPlayTimer();
                        Player.SavePlayer(Main.ActivePlayerFileData);
                        Player.ClearPlayerTempInfo();
                        Rain.ClearRain();
                        if (netMode == NetmodeID.SinglePlayer)
                            WorldFile.SaveWorld();
                        else
                        {
                            Netplay.Disconnect = true;
                            Main.netMode = NetmodeID.SinglePlayer;
                        }
                        Main.fastForwardTimeToDawn = false;
                        Main.fastForwardTimeToDusk = false;
                        Main.UpdateTimeRate();
                        PlayerWasDeletedByNamelessDeity = false;
                    }));

                    DeletionTimer = 0;
                    PlayerWasDeleted = false;
                }
            }
        }

        public override void ModifyScreenPosition()
        {
            if (DeletionTimer >= 2)
                Main.screenPosition = ScreenPositionAtPointOfDeletion;
            if (!PlayerWasDeleted)
                PlayerWasDeletedByLaRuga = false;
        }
    }
}
