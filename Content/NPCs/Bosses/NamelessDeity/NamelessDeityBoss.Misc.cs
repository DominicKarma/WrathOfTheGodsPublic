using System;
using System.IO;
using System.Linq;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        #region Multiplayer Syncs

        public override void SendExtraAI(BinaryWriter writer)
        {
            BitsByte flags = new()
            {
                [0] = StarShouldBeHeldByLeftHand,
                [1] = WaitingForPhase2Transition,
                [2] = WaitingForDeathAnimation,
                [3] = HasExperiencedFinalAttack,
                [4] = TargetIsUsingRodOfHarmony,
                [5] = ShouldStartTeleportAnimation
            };
            writer.Write(flags);

            writer.Write(AttackTimer);
            writer.Write(CurrentPhase);
            writer.Write(SwordSlashCounter);
            writer.Write(SwordSlashDirection);
            writer.Write(SwordAnimationTimer);
            writer.Write(TeleportInTime);
            writer.Write(TeleportOutTime);

            writer.Write(PunchOffsetAngle);
            writer.Write(FightLength);
            writer.Write(ZPosition);

            writer.WriteVector2(GeneralHoverOffset);
            writer.WriteVector2(LightSlashPosition);
            writer.WriteVector2(CensorPosition);
            writer.WriteVector2(PunchDestination);

            // Write lists.
            writer.Write(Hands.Count);
            for (int i = 0; i < Hands.Count; i++)
                Hands[i].WriteTo(writer);

            writer.Write(StarSpawnOffsets.Count);
            for (int i = 0; i < StarSpawnOffsets.Count; i++)
                writer.WriteVector2(StarSpawnOffsets[i]);

            var stateStack = (StateMachine?.StateStack ?? new()).ToList();
            writer.Write(stateStack.Count);
            for (int i = stateStack.Count - 1; i >= 0; i--)
                writer.Write((byte)stateStack[i].Identifier);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            StarShouldBeHeldByLeftHand = flags[0];
            WaitingForPhase2Transition = flags[1];
            WaitingForDeathAnimation = flags[2];
            HasExperiencedFinalAttack = flags[3];
            TargetIsUsingRodOfHarmony = flags[4];
            ShouldStartTeleportAnimation = flags[5];

            AttackTimer = reader.ReadInt32();
            CurrentPhase = reader.ReadInt32();
            SwordSlashCounter = reader.ReadInt32();
            SwordSlashDirection = reader.ReadInt32();
            SwordAnimationTimer = reader.ReadInt32();
            TeleportInTime = reader.ReadInt32();
            TeleportOutTime = reader.ReadInt32();

            FightLength = reader.ReadInt32();
            PunchOffsetAngle = reader.ReadSingle();
            ZPosition = reader.ReadSingle();

            GeneralHoverOffset = reader.ReadVector2();
            LightSlashPosition = reader.ReadVector2();
            CensorPosition = reader.ReadVector2();
            PunchDestination = reader.ReadVector2();

            // Read lists.
            Hands.Clear();
            StarSpawnOffsets.Clear();
            StateMachine?.StateStack.Clear();

            int handCount = reader.ReadInt32();
            for (int i = 0; i < handCount; i++)
                Hands.Add(NamelessDeityHand.ReadFrom(reader));

            int starOffsetCount = reader.ReadInt32();
            for (int i = 0; i < starOffsetCount; i++)
                StarSpawnOffsets.Add(reader.ReadVector2());

            int stateStackCount = reader.ReadInt32();
            for (int i = 0; i < stateStackCount; i++)
                StateMachine.StateStack.Push(StateMachine.StateRegistry[(NamelessAIType)reader.ReadByte()]);
        }

        #endregion Multiplayer Syncs

        #region Hit Effects

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay >= 1)
                return;

            NPC.soundDelay = 12;
            SoundEngine.PlaySound(HitSound, NPC.Center);
        }

        public override bool CheckDead()
        {
            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill Nameless.
            if (CurrentState == NamelessAIType.DeathAnimation && AttackTimer >= 10f)
                return true;

            // Keep Nameless' HP at its minimum.
            NPC.life = 1;

            if (!WaitingForDeathAnimation)
            {
                WaitingForDeathAnimation = true;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
            }
            return false;
        }

        // Ensure that Nameless' contact damage adheres to the special boss-specific cooldown slot, to prevent things like lava cheese.
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // This is quite scuffed, but since there's no equivalent easy Colliding hook for NPCs, it is necessary to increase Nameless' "effective hitbox" to an extreme
            // size via a detour and then use the CanHitPlayer hook to selectively choose whether the target should be inflicted damage or not (in this case, based on hands that can do damage).
            // This is because NPC collisions are fundamentally based on rectangle intersections. CanHitPlayer does not allow for the negation of that. But by increasing the hitbox by such an
            // extreme amount that that check is always passed, this issue is mitigated. Again, scuffed, but the onus is on TML to make this easier for modders to do.
            if (Hands.Where(h => h.CanDoDamage).Any())
                return Hands.Where(h => h.CanDoDamage).Any(h => Utils.CenteredRectangle(h.Center, TeleportVisualsAdjustedScale * 106f).Intersects(target.Hitbox));

            return CurrentState == NamelessAIType.SwordConstellation && NPC.ai[2] == 1f;
        }

        private void ExpandEffectiveHitboxForHands(On_NPC.orig_GetMeleeCollisionData orig, Rectangle victimHitbox, int enemyIndex, ref int specialHitSetter, ref float damageMultiplier, ref Rectangle npcRect)
        {
            orig(victimHitbox, enemyIndex, ref specialHitSetter, ref damageMultiplier, ref npcRect);

            // See the big comment in CanHitPlayer.
            if (Main.npc[enemyIndex].type == Type && Main.npc[enemyIndex].As<NamelessDeityBoss>().CurrentState == NamelessAIType.RealityTearPunches)
                npcRect.Inflate(4000, 4000);
        }

        // Timed DR but a bit different. I'm typically very, very reluctant towards this mechanic, but given that this boss exists in shadowspec tier, I am willing to make
        // an exception. This will not cause the dumb "lol do 0 damage for 30 seconds" problems that Calamity had in the past.
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // Calculate how far ahead Nameless' HP is relative to how long he's existed so far.
            // This would be one if you somehow got him to death on the first frame of the fight.
            // This naturally tapers off as the fight goes on.
            float fightLengthInterpolant = InverseLerp(0f, IdealFightDuration, FightLength);
            float aheadOfFightLengthInterpolant = MathF.Max(0f, 1f - fightLengthInterpolant - LifeRatio);

            float damageReductionInterpolant = Pow(aheadOfFightLengthInterpolant, 0.64f);
            float damageReductionFactor = Lerp(1f, 1f - MaxTimedDRDamageReduction, damageReductionInterpolant);
            modifiers.FinalDamage *= damageReductionFactor;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (projectile.ModProjectile is not null and IProjOwnedByBoss<NamelessDeityBoss>)
                return false;
            if (projectile.ModProjectile is not null and IProjOwnedByBoss<EntropicGod>)
                return true;

            return null;
        }

        public override bool CanBeHitByNPC(NPC attacker)
        {
            return attacker.type == ModContent.NPCType<EntropicGod>();
        }

        #endregion Hit Effects

        #region Gotta Manually Disable Despawning Lmao

        // Disable natural despawning for Nameless.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
