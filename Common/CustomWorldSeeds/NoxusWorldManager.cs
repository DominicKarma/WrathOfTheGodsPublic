using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using NoxusBoss.Common.CustomWorldSeeds;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.CustomWorldSeeds
{
    public partial class NoxusWorldManager : ModSystem
    {
        private static bool wasOriginallyDaytime;

        private static int profanedGuardianID = -1;

        private static int providenceID = -1;

        public const string SeedName = "Noxus' Realm";

        public static bool Enabled => CustomWorldSeedManager.IsSeedActive(SeedName);

        public override void OnModLoad()
        {
            // Register this seed in the central manager.
            CustomWorldSeedManager.RegisterSeed(SeedName, "darknessfalls", "darkness falls");

            // Load custom NPC IDs.
            if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            {
                if (cal.TryFind("ProfanedGuardianCommander", out ModNPC guardian))
                    profanedGuardianID = guardian.Type;
                if (cal.TryFind("Providence", out ModNPC provi))
                    providenceID = provi.Type;
            }

            // Load IL edits and detours.
            On_Main.DrawMenu += DrawNoxusSkyDuringWorldGen;
            On_NPC.SpawnNPC += MakeNightSpawnsPermanentForNoxusWorld;

            new ManagedILEdit("NPC Night AI Application", edit =>
            {
                IL_Main.DoUpdateInWorld += edit.SubscriptionWrapper;
            }, MakeNPCsUseNightAIInNoxusWorld).Apply();

            // Apply event subscriptions.
            NoxusGlobalNPC.EditSpawnRateEvent += IncreaseSpawnRatesInWorld;

            // Load the night screen shader.
            if (Main.netMode != NetmodeID.Server)
                LoadNightScreenShader();
        }

        private void DrawNoxusSkyDuringWorldGen(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            if (Main.gameMenu && WorldGen.generatingWorld && Enabled)
            {
                // Keep the music silent.
                float newMusicFade = Main.musicFade[Main.curMusic];
                if (newMusicFade > 0f)
                {
                    Main.audioSystem?.UpdateAmbientCueTowardStopping(Main.curMusic, 1f, ref newMusicFade, 0f);
                    Main.musicFade[Main.curMusic] = newMusicFade;
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

                // Draw the Noxus sky.
                var sky = (NoxusSky)SkyManager.Instance["NoxusBoss:NoxusSky"];
                sky.Update(gameTime);
                sky.Draw(Main.spriteBatch, 0f, 5f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }

            orig(self, gameTime);
        }

        private void MakeNightSpawnsPermanentForNoxusWorld(On_NPC.orig_SpawnNPC orig)
        {
            bool wasDaytime = Main.dayTime;
            if (Enabled && !Main.eclipse)
                Main.dayTime = false;

            try
            {
                orig();
            }
            finally
            {
                Main.dayTime = wasDaytime;
            }
        }

        private void MakeNPCsUseNightAIInNoxusWorld(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Go a bit before NPC updating code.
            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(FixExploitManEaters).GetMethod("Update"))))
            {
                edit.LogFailure("Could not locate the FixExploitManEaters.Update call.");
                return;
            }

            cursor.EmitDelegate(() =>
            {
                // Temporarily change the time to night. This will be reset back to what it was originally after all NPCs update.
                // The fews exceptions to this are the Profaned Guardians and Providence fight, since those two specifically enrage/despawn during the day, and the eclipse, since the enemies that event spawns
                // go away if it's night time.
                wasOriginallyDaytime = Main.dayTime;
                bool anyProfanedBosses = (profanedGuardianID != -1 && NPC.AnyNPCs(profanedGuardianID)) || (providenceID != -1 && NPC.AnyNPCs(providenceID));
                if (Enabled && !anyProfanedBosses && !Main.eclipse)
                    Main.dayTime = false;
            });

            // Go a bit after NPC updating code.
            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(LockOnHelper).GetMethod("SetUP"))))
            {
                edit.LogFailure("Could not locate the LockOnHelper.SetUP call.");
                return;
            }

            cursor.EmitDelegate(() =>
            {
                // Reset the time to its original state.
                Main.dayTime = wasOriginallyDaytime;
            });
        }

        private void IncreaseSpawnRatesInWorld(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (!Enabled)
                return;

            // Determine if the player is on the surface (but not in space).
            bool playerAtSurface = player.Center.Y / 16f <= Main.worldSurface && player.Center.Y / 16f >= Main.worldSurface * 0.35f;

            // Increase spawns even further if on the surface in hardmode. This does not apply during eclipses, because the spawn rates are already very extreme there.
            bool aLotMoreSpawns = Main.hardMode && playerAtSurface && !Main.eclipse;

            // Change spawn rates.
            if (spawnRate >= 10 && spawnRate <= 1000000)
                spawnRate = (int)(spawnRate * (aLotMoreSpawns ? 0.33333f : 0.6f));
            if (maxSpawns >= 1)
                maxSpawns += aLotMoreSpawns ? 8 : 3;
        }
    }
}
