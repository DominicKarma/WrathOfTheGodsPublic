using System;
using System.IO;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        #region Multiplayer Syncs
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(FightLength);
            writer.Write(PhaseCycleIndex);
            writer.Write(PortalChainDashCounter);
            writer.Write(CurrentPhase);
            writer.Write(BrainFogChargeCounter);
            writer.Write(NPC.Opacity);
            writer.Write(LaserSpinDirection);
            writer.WriteVector2(TeleportPosition);
            writer.WriteVector2(TeleportDirection);
            writer.WriteVector2(PortalArcSpawnCenter);
            writer.Write(LaserRotation.X);
            writer.Write(LaserRotation.Y);
            writer.Write(LaserRotation.Z);

            InitializeHandsIfNecessary();
            Hands[0].WriteTo(writer);
            Hands[1].WriteTo(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            FightLength = reader.ReadInt32();
            PhaseCycleIndex = reader.ReadInt32();
            PortalChainDashCounter = reader.ReadInt32();
            CurrentPhase = reader.ReadInt32();
            BrainFogChargeCounter = reader.ReadInt32();
            NPC.Opacity = reader.ReadSingle();
            LaserSpinDirection = reader.ReadSingle();
            TeleportPosition = reader.ReadVector2();
            TeleportDirection = reader.ReadVector2();
            PortalArcSpawnCenter = reader.ReadVector2();
            LaserRotation = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            InitializeHandsIfNecessary();
            Hands[0].ReadFrom(reader);
            Hands[1].ReadFrom(reader);
        }
        #endregion Multiplayer Syncs

        #region Hit Effects

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay >= 1 || CurrentAttack == EntropicGodAttackType.DeathAnimation)
                return;

            NPC.soundDelay = 9;
            SoundEngine.PlaySound(HitSound, NPC.Center);
        }

        public override bool CheckDead()
        {
            AttackTimer = 0f;

            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill Noxus.
            if (CurrentAttack == EntropicGodAttackType.DeathAnimation && AttackTimer >= 10f)
                return true;

            SelectNextAttack();
            ClearAllProjectiles();
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            CurrentAttack = EntropicGodAttackType.DeathAnimation;
            NPC.netUpdate = true;
            return false;
        }

        public override void OnKill()
        {
            if (!WorldSaveSystem.HasDefeatedNoxus)
            {
                WorldSaveSystem.HasDefeatedNoxus = true;
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        // Ensure that Noxus' contact damage adhere to the special boss-specific cooldown slot, to prevent things like lava cheese.
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), DebuffDuration_RegularAttack);
        }

        // Timed DR but a bit different. I'm typically very, very reluctant towards this mechanic, but given that this boss exists in shadowspec tier, I am willing to make
        // an exception. This will not cause the dumb "lol do 0 damage for 30 seconds" problems that Calamity had in the past.
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // Calculate how far ahead Noxus' HP is relative to how long he's existed so far.
            // This would be one if you somehow got him to death on the first frame of the fight.
            // This naturally tapers off as the fight goes on.
            float fightLengthInterpolant = InverseLerp(0f, IdealFightDuration, FightLength);
            float aheadOfFightLengthInterpolant = MathF.Max(0f, 1f - fightLengthInterpolant - LifeRatio);

            float damageReductionInterpolant = Pow(aheadOfFightLengthInterpolant, 0.64f);
            float damageReductionFactor = Lerp(1f, MaxTimedDRDamageReduction, damageReductionInterpolant);
            modifiers.FinalDamage *= damageReductionFactor;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (projectile.ModProjectile is not null and IProjOwnedByBoss<EntropicGod>)
                return false;
            if (projectile.ModProjectile is not null and IProjOwnedByBoss<NamelessDeityBoss>)
                return true;

            return null;
        }

        public override bool CanBeHitByNPC(NPC attacker)
        {
            return attacker.type == ModContent.NPCType<NamelessDeityBoss>();
        }
        #endregion Hit Effects

        #region Gotta Manually Disable Despawning Lmao
        // Disable natural despawning for Noxus.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
