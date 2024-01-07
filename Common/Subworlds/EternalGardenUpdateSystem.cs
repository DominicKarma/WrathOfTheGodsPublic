using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Projectiles.Visuals;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using ReLogic.Utilities;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Common.Subworlds.EternalGarden;

namespace NoxusBoss.Common.Subworlds
{
    public class EternalGardenUpdateSystem : ModSystem
    {
        public static int CrystalCrusherRayID
        {
            get;
            private set;
        } = -10000;

        public static int MortarRoundID
        {
            get;
            private set;
        } = -10000;

        public static int RubberMortarRoundID
        {
            get;
            private set;
        } = -10000;

        public static bool WasInSubworldLastUpdateFrame
        {
            get;
            private set;
        }

        public static int TimeSpentInCenter
        {
            get;
            private set;
        }

        public static bool LifeFruitDroppedFromTree
        {
            get;
            set;
        }

        public static bool AnyoneInCenter
        {
            get;
            private set;
        }

        public static SlotId StandingInCenterSound
        {
            get;
            private set;
        }

        // How long players must wait in the center of the garden in order for Nameless to appear.
        public static readonly int NamelessDeitySummonDelayInCenter = SecondsToFrames(5f);

        public override void OnModLoad()
        {
            // Subscribe to various events.
            NoxusPlayer.PostUpdateEvent += CreatePaleDuckweedInGarden;
            NoxusPlayer.ResetEffectsEvent += DisallowPlantBreakageInGarden;
            NoxusPlayer.PostUpdateEvent += CreateWindInGarden;
            NoxusGlobalItem.CanUseItemEvent += DisableCelestialSigil;
            NoxusGlobalItem.CanUseItemEvent += DisableProblematicItems;
            NoxusGlobalNPC.EditSpawnPoolEvent += OnlyAllowFriendlySpawnsInGarden;
            NoxusGlobalNPC.EditSpawnRateEvent += IncreaseFriendlySpawnsInGarden;
            NoxusGlobalProjectile.PreAIEvent += KillProblematicProjectilesInGarden;
            NoxusGlobalTile.NearbyEffectsEvent += MakeTombsGo1984InGarden;
            NoxusGlobalTile.IsTileUnbreakableEvent += DisallowTileBreakageInGarden;
            NoxusGlobalWall.IsWallUnbreakableEvent += DisallowWallBreakageInGarden;
        }

        public override void PostSetupContent()
        {
            if (ModReferences.BaseCalamity is not null)
            {
                if (ModReferences.BaseCalamity.TryFind("CrystylCrusherRay", out ModProjectile ray))
                    CrystalCrusherRayID = ray.Type;
                if (ModReferences.BaseCalamity.TryFind("MortarRoundProj", out ModProjectile mortar))
                    MortarRoundID = mortar.Type;
                if (ModReferences.BaseCalamity.TryFind("RubberMortarRoundProj", out ModProjectile rubberMortar))
                    RubberMortarRoundID = rubberMortar.Type;
            }
        }

        private void CreatePaleDuckweedInGarden(NoxusPlayer p)
        {
            // Create pale duckweed in the water if the player is in the eternal garden and Nameless is not present.
            int duckweedSpawnChance = 3;
            if (!WasInSubworldLastUpdateFrame || NamelessDeityBoss.Myself is not null || !Main.rand.NextBool(duckweedSpawnChance))
                return;

            // Try to find a suitable location to spawn the duckweed. Once one is found, this loop terminates.
            // If none is found, then the loop simply runs through all its iterations without issue.
            for (int tries = 0; tries < 50; tries++)
            {
                Vector2 potentialSpawnPosition = p.Player.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, 1500f);
                if (Collision.SolidCollision(potentialSpawnPosition, 1, 1) || !Collision.WetCollision(potentialSpawnPosition, 1, 1))
                    continue;

                Vector2 spawnVelocity = -Vector2.UnitY.RotatedByRandom(0.82f) * Main.rand.NextFloat(0.5f, 1.35f);
                Color duckweedColor = Color.Lerp(Color.Wheat, Color.Red, Main.rand.NextFloat(0.52f));
                PaleDuckweedParticle duckweed = new(potentialSpawnPosition, spawnVelocity, duckweedColor, 540);
                duckweed.Spawn();
                break;
            }
        }

        private void DisallowPlantBreakageInGarden(NoxusPlayer p)
        {
            // Prevent players from breaking the plants with swords and projectiles in the subworld.
            if (WasInSubworldLastUpdateFrame)
                p.Player.dontHurtNature = true;
        }

        private void CreateWindInGarden(NoxusPlayer p)
        {
            // Create wind if the player is in the garden.
            if (Main.myPlayer == p.Player.whoAmI && WasInSubworldLastUpdateFrame && NamelessDeityBoss.Myself is null && Main.rand.NextBool(9))
            {
                Vector2 windVelocity = Vector2.UnitX * Main.rand.NextFloat(10f, 14f) * Main.windSpeedTarget;

                // Try to find a suitable location to spawn the wind. Once one is found, this loop terminates.
                // If none is found, then the loop simply runs through all its iterations without issue.
                for (int tries = 0; tries < 50; tries++)
                {
                    Vector2 potentialSpawnPosition = p.Player.Center + new Vector2(Sign(windVelocity.X) * -Main.rand.NextFloat(1050f, 1250f), Main.rand.NextFloatDirection() * 900f);
                    if (Collision.SolidCollision(potentialSpawnPosition, 1, 120) || Collision.WetCollision(potentialSpawnPosition, 1, 120))
                        continue;

                    Projectile.NewProjectile(p.Player.GetSource_FromThis(), potentialSpawnPosition, windVelocity, ModContent.ProjectileType<WindStreakVisual>(), 0, 0f, p.Player.whoAmI);
                    break;
                }
            }
        }

        private bool DisableCelestialSigil(Item item, Player player)
        {
            // Immediately return true if the player isn't even in the subworld.
            if (!WasInSubworldLastUpdateFrame)
                return true;

            return item.type != ItemID.CelestialSigil;
        }

        private bool DisableProblematicItems(Item item, Player player)
        {
            // Immediately return true if the player isn't even in the subworld.
            if (!WasInSubworldLastUpdateFrame)
                return true;

            // Disable liquid placing/removing items.
            int itemID = item.type;
            bool isSponge = itemID == ItemID.SuperAbsorbantSponge || itemID == ItemID.LavaAbsorbantSponge || itemID == ItemID.HoneyAbsorbantSponge || itemID == ItemID.UltraAbsorbantSponge;
            bool isRegularBucket = itemID == ItemID.EmptyBucket || itemID == ItemID.WaterBucket || itemID == ItemID.LavaBucket || itemID == ItemID.HoneyBucket;
            bool isSpecialBucket = itemID == ItemID.BottomlessBucket || itemID == ItemID.BottomlessLavaBucket || itemID == ItemID.BottomlessHoneyBucket || itemID == ItemID.BottomlessShimmerBucket;
            return !isSponge && !isRegularBucket && !isSpecialBucket;
        }

        private void OnlyAllowFriendlySpawnsInGarden(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!WasInSubworldLastUpdateFrame)
                return;

            // Get a collection of all NPC IDs in the spawn pool that are not critters.
            IEnumerable<int> npcsToRemove = pool.Keys.Where(npcID => !NPCID.Sets.CountsAsCritter[npcID]);

            // Use the above collection as a blacklist, removing all NPCs that are included in it, effectively ensuring only critters may spawn in the garden.
            foreach (int npcIDToRemove in npcsToRemove)
                pool.Remove(npcIDToRemove);
        }

        private void IncreaseFriendlySpawnsInGarden(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (WasInSubworldLastUpdateFrame)
            {
                spawnRate = 60;
                maxSpawns = NamelessDeityBoss.Myself is null ? 50 : 0;
            }
        }

        private bool KillProblematicProjectilesInGarden(Projectile projectile)
        {
            // Don't do anything if this event is called outside of the garden.
            if (!WasInSubworldLastUpdateFrame)
                return true;

            // This apparently causes shader issues in the garden.
            // This projectile is notably used by the Shattered Community when leveling up.
            if (projectile.type == ProjectileID.DD2ElderWins)
            {
                projectile.active = false;
                return false;
            }

            // Prevent tombs from cluttering things up in the garden.
            bool isTomb = projectile.type is ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1 or ProjectileID.RichGravestone2 or
                ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4 or ProjectileID.RichGravestone4 or ProjectileID.Headstone or ProjectileID.Obelisk or
                ProjectileID.GraveMarker or ProjectileID.CrossGraveMarker or ProjectileID.Headstone;
            if (isTomb)
                projectile.active = false;

            // Prevent crystyl crusher's beam and other tile-manipulating items like the sandgun from working in the garden and messing up tiles.
            if (projectile.type == CrystalCrusherRayID)
                projectile.active = false;
            if (projectile.type == ProjectileID.DirtBomb || projectile.type == ProjectileID.DirtStickyBomb)
                projectile.active = false;
            if (projectile.type == ProjectileID.SandBallGun || projectile.type == ProjectileID.SandBallGun)
                projectile.active = false;
            if (projectile.type == ProjectileID.SandBallFalling || projectile.type == ProjectileID.PearlSandBallFalling)
                projectile.active = false;
            if (projectile.type == ProjectileID.EbonsandBallFalling || projectile.type == ProjectileID.EbonsandBallGun)
                projectile.active = false;
            if (projectile.type == ProjectileID.CrimsandBallFalling || projectile.type == ProjectileID.CrimsandBallGun)
                projectile.active = false;

            // From the Dirt Rod. Kill is used instead of active = false to ensure that the dirt doesn't just vanish and gets placed down again in its original location.
            if (projectile.type == ProjectileID.DirtBall)
                projectile.Kill();

            // No explosives.
            // MAN rocket code is evil!
            bool dryRocket = projectile.type == ProjectileID.DryRocket || projectile.type == ProjectileID.DrySnowmanRocket;
            bool wetRocket = projectile.type == ProjectileID.WetRocket || projectile.type == ProjectileID.WetSnowmanRocket;
            bool honeyRocket = projectile.type == ProjectileID.HoneyRocket || projectile.type == ProjectileID.HoneySnowmanRocket;
            bool lavaRocket = projectile.type == ProjectileID.LavaRocket || projectile.type == ProjectileID.LavaSnowmanRocket;
            bool rocket = dryRocket || wetRocket || honeyRocket || lavaRocket || projectile.type == MortarRoundID || projectile.type == RubberMortarRoundID;

            bool dryMisc = projectile.type == ProjectileID.DryGrenade || projectile.type == ProjectileID.DryMine;
            bool wetMisc = projectile.type == ProjectileID.WetGrenade || projectile.type == ProjectileID.WetMine;
            bool honeyMisc = projectile.type == ProjectileID.HoneyGrenade || projectile.type == ProjectileID.HoneyMine;
            bool lavaMisc = projectile.type == ProjectileID.LavaGrenade || projectile.type == ProjectileID.LavaMine;
            bool miscExplosive = dryMisc || wetMisc || honeyMisc || lavaMisc;

            if (rocket || miscExplosive)
                projectile.active = false;

            return true;
        }

        private void MakeTombsGo1984InGarden(int x, int y, int type, bool closer)
        {
            if (!WasInSubworldLastUpdateFrame)
                return;

            // Erase tombstones in the garden.
            if (type == TileID.Tombstones)
                Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
        }

        private bool DisallowTileBreakageInGarden(int x, int y, int type)
        {
            // True = Tiles are unbreakable, False = Tiles are breakable.
            return WasInSubworldLastUpdateFrame;
        }

        private bool DisallowWallBreakageInGarden(int x, int y, int type)
        {
            // True = Walls are unbreakable, False = Walls are breakable.
            return WasInSubworldLastUpdateFrame;
        }

        public override void PreUpdateEntities()
        {
            // Reset the text opacity when the game is being played. It will increase up to full opacity during subworld transition drawing.
            TextOpacity = 0f;

            // Verify whether things are in the subworld. This hook runs on both clients and the server. If for some reason this stuff needs to be determined in a different
            // hook it is necessary to ensure that property is preserved wherever you put it.
            bool inGarden = SubworldSystem.IsActive<EternalGarden>();
            if (WasInSubworldLastUpdateFrame != inGarden)
            {
                // A major flaw with respect to subworld data transfer is the fact that Calamity's regular OnWorldLoad hooks clear everything.
                // This works well and good for Calamity's purposes, but it causes serious issues when going between subworlds. The result of this is
                // ordered as follows:

                // 1. Exit world. Store necessary data for subworld transfer.
                // 2. Load necessary stuff for subworld and wait.
                // 3. Enter subworld. Load data from step 1.
                // 4. Call OnWorldLoad, resetting everything from step 3.

                // In order to address this, a final step is introduced:
                // 5. Load data from step 3 again on the first frame of entity updating.
                if (inGarden)
                {
                    if (Main.netMode != NetmodeID.Server)
                        LoadWorldDataFromTag("Client", ClientWorldDataTag);

                    // Create light flash teleport visuals on the player.
                    CreatePlayerLightFlashEffects();
                }

                WasInSubworldLastUpdateFrame = inGarden;
            }

            // Good apples can only drop from the tree the first time Nameless is fought in the subworld.
            // This resets if people leave the subworld and return again later, however.
            if (!WasInSubworldLastUpdateFrame)
                LifeFruitDroppedFromTree = false;

            // Everything beyond this point applies solely to the subworld.
            if (!WasInSubworldLastUpdateFrame)
            {
                TimeSpentInCenter = 0;
                return;
            }

            // Apply subworld specific behaviors.
            SubworldSpecificUpdateBehaviors();
        }

        private static void SubworldSpecificUpdateBehaviors()
        {
            // Keep it perpetually night time if Nameless is not present.
            if (NamelessDeityBoss.Myself is null)
            {
                Main.dayTime = false;
                Main.time = 16200f;
            }

            // Keep the wind strong, so that the plants sway around.
            // This swiftly ceases if Nameless is present, as though nature is fearful of him.
            if (NamelessDeityBoss.Myself is null)
                Main.windSpeedTarget = Lerp(0.88f, 1.32f, AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f);
            else
                Main.windSpeedTarget = 0f;
            Main.windSpeedCurrent = Lerp(Main.windSpeedCurrent, Main.windSpeedTarget, 0.03f);

            // Create a god ray at the center of the garden if Nameless isn't present.
            int godRayID = ModContent.ProjectileType<GodRayVisual>();
            if (Main.netMode != NetmodeID.MultiplayerClient && NamelessDeityBoss.Myself is null && !AnyProjectiles(godRayID))
            {
                Vector2 centerOfWorld = new Point(Main.maxTilesX / 2, EternalGardenWorldGen.SurfaceTilePoint).ToWorldCoordinates() + Vector2.UnitY * 320f;
                NewProjectileBetter(centerOfWorld, Vector2.Zero, godRayID, 0, 0f);
            }

            // Check if anyone is in the center of the garden for the purpose of determining if the time-in-center timer should increment.
            // This does not apply if Nameless is present.
            AnyoneInCenter = false;
            if (NamelessDeityBoss.Myself is null)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (!p.active || p.dead)
                        continue;

                    if (Distance(p.Center.X, Main.maxTilesX * 8f) <= (EternalGardenWorldGen.TotalFlatTilesAtCenter + 8f) * 16f)
                    {
                        AnyoneInCenter = true;
                        break;
                    }
                }
            }

            // Play a special sound if the player enters the center.
            if (TimeSpentInCenter == 2 && AnyoneInCenter)
            {
                StandingInCenterSound = SoundEngine.PlaySound(NamelessDeityBoss.StandingInLightSound, null, sound =>
                {
                    if (!AnyoneInCenter)
                        sound.Volume = Clamp(sound.Volume - 0.05f, 0f, 1f);

                    return WasInSubworldLastUpdateFrame;
                });
            }

            if (SoundEngine.TryGetActiveSound(StandingInCenterSound, out var sound))
                sound.Callback?.Invoke(sound);

            // Disallow player movement shortly after the sound plays to force the feeling of suspense.
            if (TimeSpentInCenter == 150 && AnyoneInCenter)
                InputAndUIBlockerSystem.Start(true, false, () => TimeSpentInCenter >= 150);

            // Spawn Nameless if a player has spent a sufficient quantity of time in the center of the garden.
            TimeSpentInCenter = Utils.Clamp(TimeSpentInCenter + AnyoneInCenter.ToDirectionInt(), 0, 600);
            if (Main.netMode != NetmodeID.MultiplayerClient && TimeSpentInCenter >= NamelessDeitySummonDelayInCenter && NamelessDeityBoss.Myself is null)
            {
                NPC.NewNPC(new EntitySource_WorldEvent(), Main.maxTilesX * 8, EternalGardenWorldGen.SurfaceTilePoint * 16 - 800, ModContent.NPCType<NamelessDeityBoss>(), 1);
                TimeSpentInCenter = 0;
            }

            // Make the music dissipate in accordance with how long the player has been in the center.
            MusicVolumeManipulationSystem.MuffleFactor = 1f - InverseLerp(30f, NamelessDeitySummonDelayInCenter * 0.5f, TimeSpentInCenter);

            // Disable typical weather things.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Sandstorm.Happening)
                    Sandstorm.StopSandstorm();
                Main.StopRain();
                Main.StopSlimeRain();
            }
        }

        public static void CreatePlayerLightFlashEffects()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                if (Main.myPlayer == i)
                    SoundEngine.PlaySound(TeleportOutSound);

                // Create the particle effects.
                ExpandingGreyscaleCircleParticle circle = new(p.Center, Vector2.Zero, new(219, 194, 229), 10, 0.28f);
                VerticalLightStreakParticle bigLightStreak = new(p.Center, Vector2.Zero, new(228, 215, 239), 10, new(2.4f, 3f));
                MagicBurstParticle magicBurst = new(p.Center, Vector2.Zero, new(150, 109, 219), 12, 0.1f);

                circle.Spawn();
                bigLightStreak.Spawn();
                magicBurst.Spawn();

                // Shake the screen a little bit.
                StartShakeAtPoint(p.Center, 7f);
            }
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (!WasInSubworldLastUpdateFrame && NamelessDeitySky.SkyIntensityOverride <= 0f)
                return;

            tileColor = new(81, 119, 135);

            if (WasInSubworldLastUpdateFrame)
                backgroundColor = new(4, 6, 14);

            // Make the background brighter the closer the camera is to the center of the world.
            float centerOfWorld = Main.maxTilesX * 8f;
            float distanceToCenterOfWorld = Distance(Main.screenPosition.X + Main.screenWidth * 0.5f, centerOfWorld);
            float brightnessInterpolant = InverseLerp(3200f, 1400f, distanceToCenterOfWorld);
            if (WasInSubworldLastUpdateFrame)
                backgroundColor = Color.Lerp(backgroundColor, Color.LightCoral, brightnessInterpolant * 0.27f);
            tileColor = Color.Lerp(tileColor, Color.LightPink, brightnessInterpolant * 0.4f);

            // Make everything bright if Nameless is present.
            tileColor = Color.Lerp(tileColor, Color.White, MathF.Max(NamelessDeitySky.HeavenlyBackgroundIntensity, NamelessDeitySky.SkyIntensityOverride));
        }
    }
}
