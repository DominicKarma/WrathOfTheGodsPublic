using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public partial class NamelessDeityInfiniteStairwayManager : ModSystem
    {
        public static float WidthPerStep => 15f;

        public static float HeightPerStep => 6f;

        public static float StepHeight => 28f;

        public static int StepsPerSprite => 2;

        public static float StairwaySlope => HeightPerStep / WidthPerStep;

        public static int StepSoundCountdown
        {
            get;
            set;
        }

        public static int DisableBlackCountdown
        {
            get;
            set;
        }

        public static bool HasEnteredEmptySpace
        {
            get;
            set;
        }

        public static float DistanceMoved
        {
            get;
            set;
        }

        public static float GardenStartX
        {
            get;
            set;
        }

        public static Vector2 StairwayStart
        {
            get;
            set;
        }

        public static Vector2 PlayerPositionBeforeAnimationBegan
        {
            get;
            set;
        }

        public static float RunCompletion => InverseLerp(300f, DistanceNeededForMaxBrightess, DistanceMoved);

        // Make the distance longer so Fanny can ramble if Cal Remix is enabled.
        public static float DistanceNeededForMaxBrightess => ModReferences.CalamityRemix is not null ? 14000f : 6000f;

        public static readonly SoundStyle StairStepSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Environment/DivineStairwayStep", 7) with { PitchVariance = 0.15f, Volume = 0.4f, MaxInstances = 15 };

        public override void OnModLoad()
        {
            // Apply IL edits and detours.
            On_Player.DryCollision += AddStairwayCollision;
            On_Collision.WetCollision += DisableLiquidCollision;
            On_Collision.WaterCollision += DisableLiquidCollision2;
            On_Collision.DrownCollision += DisableLiquidCollision3;
            On_Collision.StickyTiles += DisableCobwebInteractions;
            On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += DisableIdleParticlesFromTiles;
            On_LightingEngine.GetColor += TemporarilyDisableBlackDrawingForLight;
            IL_Main.DrawMap += ObfuscateMap;
            On_Player.ApplyTouchDamage += DisableSuffocation;
            On_Player.FloorVisuals += DisableFloorVisuals;
            On_Main.DrawInterface_40_InteractItemIcon += DisableHoverItems;

            new ManagedILEdit("Obfuscate Stairway Position Info", edit =>
            {
                IL_Main.DrawInfoAccs += edit.SubscriptionWrapper;
            }, ObfuscatePositionInfo).Apply(false);

            // Load plant textures.
            LoadPlantTextures();

            // Prepare event subscriptions.
            NoxusPlayer.SaveDataEvent += SaveDefeatStateForPlayer;
            NoxusPlayer.LoadDataEvent += LoadDefeatStateForPlayer;
            NoxusPlayer.PostUpdateEvent += UpdateEffect;
            NoxusGlobalNPC.EditSpawnRateEvent += DisableSpawnsWhenStairIsActive;
        }

        public override void PostSetupContent()
        {
            // Load Fanny dialog.
            LoadFannyRamblings();
        }

        private void AddStairwayCollision(On_Player.orig_DryCollision orig, Player player, bool fallThrough, bool ignorePlats)
        {
            orig(player, fallThrough, ignorePlats);

            // Go no further if the stair does not exist at all.
            if (!StairwayIsVisible)
                return;

            // Undo the velocity step from before.
            player.position -= player.velocity;

            // Store important player information in separate variables for ease of use.
            Vector2 topLeft = player.TopLeft;
            int width = player.width;
            int height = player.height;

            // Calculate the Y position of the garden's ground
            float gardenGroundHeight = 0f;
            if (GardenStartX != 0f)
                gardenGroundHeight = StairwayPositionFromXPosition(GardenStartX).Y - 11f;

            // Calculate stair hitboxes.
            List<Rectangle> stairHitboxes = new();
            for (float dx = -150f; dx < 150f; dx += WidthPerStep * StepsPerSprite)
            {
                Vector2 stairPosition = StairwayPositionFromXPosition(topLeft.X + dx);
                if (stairPosition.Y < gardenGroundHeight)
                    stairPosition.Y = gardenGroundHeight;

                for (int i = 0; i < StepsPerSprite; i++)
                {
                    Rectangle stepHitbox = new((int)(stairPosition.X + WidthPerStep * (i + 0.8f)), (int)(stairPosition.Y + HeightPerStep * (i - 1.5f)), (int)WidthPerStep, (int)StepHeight);
                    stairHitboxes.Add(stepHitbox);
                }
            }

            // Perform collision handling on the Y axis.
            bool onGround = false;
            Vector2 aheadPosition = (topLeft + player.velocity).Floor();
            Rectangle hitbox = new((int)aheadPosition.X, (int)aheadPosition.Y, width, height);
            foreach (Rectangle stairHitbox in stairHitboxes)
            {
                if (hitbox.Intersects(stairHitbox))
                {
                    player.velocity.Y = 0f;
                    onGround = true;
                }
            }

            // Perform collision handling on the X axis.
            aheadPosition = (topLeft + player.velocity).Floor();
            hitbox = new((int)aheadPosition.X, (int)aheadPosition.Y, width, height);
            Vector2 fakeVelocity = player.velocity;
            PerformAABBCollisionResponse(false, stairHitboxes, hitbox, ref fakeVelocity, out Vector2 depth);

            // All for slope-like movement up the stairs by lifting the player up to the next step if a horizontal collision happened.
            if (depth.X != 0f && player.velocity.Y >= 0f && player.velocity.Y < 0.4f)
            {
                // Apply collision corrections.
                foreach (Rectangle stairHitbox in stairHitboxes)
                {
                    Rectangle stairHitbox2 = stairHitbox;
                    while (player.position.Y >= 0 && player.Hitbox.Intersects(stairHitbox2))
                    {
                        player.position.Y--;
                        player.velocity.Y = 0f;
                    }
                }
            }

            // Handle walk interactions.
            float horizontalSpeed = Abs(player.velocity.X);
            if (onGround && horizontalSpeed >= 2f)
                ApplyWalkInteractions(player);

            // Perform the velocity step again.
            player.position += player.velocity;
        }


        private bool DisableLiquidCollision(On_Collision.orig_WetCollision orig, Vector2 Position, int Width, int Height)
        {
            return !TilesAreUninteractable && orig(Position, Width, Height);
        }


        private Vector2 DisableLiquidCollision2(On_Collision.orig_WaterCollision orig, Vector2 Position, Vector2 Velocity, int Width, int Height, bool fallThrough, bool fall2, bool lavaWalk)
        {
            if (StairwayIsVisible)
                return Velocity;

            return orig(Position, Velocity, Width, Height, fallThrough, fall2, lavaWalk);
        }

        private bool DisableLiquidCollision3(On_Collision.orig_DrownCollision orig, Vector2 Position, int Width, int Height, float gravDir, bool includeSlopes)
        {
            return !TilesAreUninteractable && orig(Position, Width, Height, gravDir, includeSlopes);
        }

        private Vector2 DisableCobwebInteractions(On_Collision.orig_StickyTiles orig, Vector2 Position, Vector2 Velocity, int Width, int Height)
        {
            if (StairwayIsVisible)
                return -Vector2.One;

            return orig(Position, Velocity, Width, Height);
        }

        private void DisableSuffocation(On_Player.orig_ApplyTouchDamage orig, Player self, int tileId, int x, int y)
        {
            if (!StairwayIsVisible)
                orig(self, tileId, x, y);
        }


        private void DisableFloorVisuals(On_Player.orig_FloorVisuals orig, Player self, bool Falling)
        {
            if (!StairwayIsVisible)
                orig(self, Falling);
        }

        private void DisableHoverItems(On_Main.orig_DrawInterface_40_InteractItemIcon orig, Main self)
        {
            if (!StairwayIsVisible)
                orig(self);
        }

        public static void KillPlayerPetsAndHooks()
        {
            if (!StairwayIsVisible)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (!p.active || p.owner != Main.myPlayer)
                    continue;

                if (!ProjectileID.Sets.LightPet[p.type] && !Main.projPet[p.type] && p.aiStyle != 7)
                    continue;

                p.active = false;
            }
        }

        public static void PerformInfiniteLoopCheck()
        {
            bool pastForwardInvisibleBarrier = (StairsDirection == -1f && Main.LocalPlayer.position.X < StairwayStart.X - 11100f) || (StairsDirection == 1f && Main.LocalPlayer.position.X > StairwayStart.X + 11100f);
            bool pastBackwardInvisibleBarrier = (StairsDirection == -1f && Main.LocalPlayer.position.X > StairwayStart.X + 1600f) || (StairsDirection == 1f && Main.LocalPlayer.position.X < StairwayStart.X - 1600f);
            bool pastInvisibleBarrier = pastBackwardInvisibleBarrier || pastForwardInvisibleBarrier;
            if (StairwayIsVisible && pastInvisibleBarrier && GardenStartX == 0f)
            {
                // Teleport the player back. The player will be vertically offset such that they are the same vertical distance from the stairs, keeping the effect seamless.
                float verticalDistanceFromStairwayBottom = Main.LocalPlayer.position.Y - StairwayPositionFromXPosition(Main.LocalPlayer.position.X).Y;
                float horizontalOffset = pastForwardInvisibleBarrier ? 11100f : -1600f;
                Vector2 oldPlayerTopLeft = Main.LocalPlayer.TopLeft;
                Main.LocalPlayer.position = StairwayPositionFromXPosition(StairwayStart.X + StairsDirection * horizontalOffset - StairsDirection * WidthPerStep * StepsPerSprite * pastForwardInvisibleBarrier.ToDirectionInt() * 50f) + Vector2.UnitY * verticalDistanceFromStairwayBottom;

                // Calculate how much the player moved from the teleport to determine how much things like dust and pets need to be moved.
                Vector2 teleportOffset = Main.LocalPlayer.TopLeft - oldPlayerTopLeft;

                // Handle weird, short-lived black screen overlays that seem to happen when the player suddenlys moves a great distance.
                Main.SetCameraLerp(1f, 15);
                DisableBlackCountdown = 20;

                // Teleport dust.
                for (int i = 0; i < Main.maxDust; i++)
                {
                    Dust d = Main.dust[i];
                    if (d.active)
                        d.position += teleportOffset;
                }

                // Teleport particles.
                foreach (Particle particle in ParticleManager.activeParticles)
                    particle.Position += teleportOffset;
            }
        }

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            // Check if there are any tiles nearby. This is done so that the tiles stop being visible and interactable only after they are all offscreen.
            int nearbyTileCount = 0;
            foreach (int tileCount in tileCounts)
                nearbyTileCount += tileCount;
            HasEnteredEmptySpace = nearbyTileCount <= 6000;
        }

        public static Vector2 StairwayPositionFromXPosition(float x)
        {
            return new(x, StairwaySlope * (x - StairwayStart.X) + StairwayStart.Y);
        }
    }
}
