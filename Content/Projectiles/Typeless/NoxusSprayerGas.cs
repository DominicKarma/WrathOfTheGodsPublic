using System.Reflection;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.PreFightForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Particles.Metaballs;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Typeless
{
    public class NoxusSprayerGas : ModProjectile
    {
        public bool PlayerHasMadeIncalculableMistake
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 9;
            Projectile.timeLeft = Projectile.MaxUpdates * 13;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Create gas.
            float particleScale = InverseLerp(0f, 25f, Time) + (Time - 32f) * 0.008f + Main.rand.NextFloat(0.075f);
            Color particleColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.7f));
            var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.9f, 0.9f), particleColor, 10, particleScale * 0.85f, particleScale * 0.5f, 0f, true, 0f);
            particle.Spawn();

            // Create more gas.
            for (int i = 0; i < 2; i++)
            {
                if (!Main.rand.NextBool(5))
                    continue;

                Color smokeColor = Color.Lerp(Color.MediumPurple, Color.Blue, Main.rand.NextFloat(0.8f));
                smokeColor.A = 0;

                var smoke = new SmallSmokeParticle(Projectile.Center, Projectile.velocity * 0.04f + particleScale * Main.rand.NextVector2Circular(8.5f, 8.5f), smokeColor * 0.9f, smokeColor * 0.4f, particleScale * 2.5f, 50f, Main.rand.NextFloatDirection() * 0.02f)
                {
                    Rotation = Main.rand.NextFloat(TwoPi),
                };
                smoke.Spawn();
            }

            // Get rid of the player if the spray was reflected by Nameless and it touches the player.
            if (PlayerHasMadeIncalculableMistake && Projectile.Hitbox.Intersects(Main.player[Projectile.owner].Hitbox) && Main.netMode == NetmodeID.SinglePlayer && Time >= 20f)
            {
                Player player = Main.player[Projectile.owner];
                for (int j = 0; j < 4; j++)
                {
                    float gasSize = player.width * Main.rand.NextFloat(0.1f, 0.8f);
                    ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(player.Center + Main.rand.NextVector2Circular(40f, 40f), Main.rand.NextVector2Circular(4f, 4f), gasSize);
                }
                typeof(SubworldSystem).GetField("current", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                typeof(SubworldSystem).GetField("cache", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                NoxusSprayPlayerDeletionSystem.PlayerWasDeleted = true;
            }

            if (Main.zenithWorld)
                CreateAnything();
            else
                DeleteEverything();

            Time++;
        }

        public void CreateAnything()
        {
            // The sprayer should wait a small amount of time before spawning things.
            if (Time < 30f)
                return;

            // Check if the surrounding area of the gas is open. If it isn't, don't do anything.
            if (Collision.SolidCollision(Projectile.Center - Vector2.One * 80f, 160, 160, true))
                return;

            // NPC spawns happen randomly.
            if (!Main.rand.NextBool(120))
                return;

            // NPC spawns can only happen serverside.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int npcID = Main.rand.Next(NPCLoader.NPCCount);
            NPC dummyNPC = new();
            dummyNPC.SetDefaults(npcID);

            // Various NPCs could cause problems and should not spawn.
            bool isNoxus = npcID == ModContent.NPCType<NoxusEggCutscene>() || npcID == ModContent.NPCType<NoxusEgg>() || npcID == ModContent.NPCType<EntropicGod>();
            bool isNameless = npcID == ModContent.NPCType<NamelessDeityBoss>();
            bool isWoF = npcID == NPCID.WallofFlesh || npcID == NPCID.WallofFleshEye;
            bool isOOAEntity = npcID == NPCID.DD2EterniaCrystal || npcID == NPCID.DD2LanePortal;
            bool isTownNPC = NPCID.Sets.IsTownSlime[npcID] || NPCID.Sets.IsTownPet[npcID] || dummyNPC.aiStyle == 7;
            bool isRegularDummy = npcID == NPCID.TargetDummy;
            if (isNoxus || isNameless || isWoF || isOOAEntity || isTownNPC || isRegularDummy)
                return;

            // Spawn the NPC.
            NPC.NewNPC(Projectile.GetSource_FromThis(), (int)Projectile.Center.X, (int)Projectile.Center.Y, npcID, 1);
        }

        public void DeleteEverything()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || !n.Hitbox.Intersects(Projectile.Hitbox) || NoxusSprayer.NPCsToNotDelete.Contains(n.type))
                    continue;

                // Reflect the spray if the player has misused it by daring to try and delete Nameless.
                if (NoxusSprayer.NPCsThatReflectSpray.Contains(n.type))
                {
                    if (!PlayerHasMadeIncalculableMistake && n.Opacity >= 0.02f)
                    {
                        PlayerHasMadeIncalculableMistake = true;
                        NoxusSprayPlayerDeletionSystem.PlayerWasDeletedByNamelessDeity = n.type == ModContent.NPCType<NamelessDeityBoss>();
                        NoxusSprayPlayerDeletionSystem.PlayerWasDeletedByLaRuga = n.type == NoxusSprayer.LaRugaID;
                        Projectile.velocity *= -0.6f;
                        Projectile.netUpdate = true;
                    }
                    continue;
                }

                n.active = false;

                for (int j = 0; j < 20; j++)
                {
                    float gasSize = n.width * Main.rand.NextFloat(0.1f, 0.8f);
                    ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(n.Center + Main.rand.NextVector2Circular(40f, 40f), Main.rand.NextVector2Circular(4f, 4f), gasSize);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
