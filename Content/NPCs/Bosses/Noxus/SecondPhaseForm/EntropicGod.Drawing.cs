using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC, IBossChecklistSupport, IToastyQoLChecklistBossSupport
    {
        private static Asset<Texture2D> backTexture;

        private static Asset<Texture2D> bossChecklistTexture;

        private static Asset<Texture2D> eyeTexture;

        private static Asset<Texture2D> handTexture;

        private static Asset<Texture2D> headTexture;

        private static Asset<Texture2D>[] ribTextures;

        private static Asset<Texture2D> telegraphBorderTexture;

        private void LoadTextures()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            backTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/EntropicGodBack");
            bossChecklistTexture = ModContent.Request<Texture2D>($"{Texture}_BossChecklist");
            eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/NoxusEye");
            handTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/EntropicGodHand");
            headTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/EntropicGodHead");

            ribTextures = new Asset<Texture2D>[3];
            for (int i = 0; i < ribTextures.Length; i++)
                ribTextures[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/EntropicGodRibs{i + 1}");

            telegraphBorderTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SecondPhaseForm/LaserTelegraphBorder");
        }

        public override void DrawBehind(int index)
        {
            if (NPC.hide && NPC.Opacity >= 0.02f)
            {
                if (ZPosition < -0.1f)
                    SpecialLayeringSystem.DrawCacheAfterNoxusFog.Add(index);
                else if (ShouldDrawBehindTiles)
                    SpecialLayeringSystem.DrawCacheBeforeBlack.Add(index);
                else
                    Main.instance.DrawCacheNPCProjectiles.Add(index);
            }
        }

        public override void BossHeadSlot(ref int index)
        {
            // Make the head icon disappear if Noxus is invisible.
            if (TeleportVisualsAdjustedScale.Length() <= 0.1f || NPC.Opacity <= 0.45f)
                index = -1;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Initialize hands if necessary.
            InitializeHandsIfNecessary();

            // Draw the back and use preset hand offests if in the bestiary.
            if (NPC.IsABestiaryIconDummy)
            {
                DrawBack(NPC.Center - screenPos, Color.Lerp(Color.Purple, Color.Black, 0.885f), NPC.rotation);
                Hands[0].Center = NPC.Center + new Vector2(136f, 54f) * NPC.scale;
                Hands[1].Center = NPC.Center + new Vector2(-136f, 54f) * NPC.scale;
                Hands[0].Frame = 2;
                Hands[1].Frame = 2;
            }

            // Draw the main texture and bright afterimages.
            float horizontalRibOffsetTime = Main.GlobalTimeWrappedHourly * 2f;
            if (ChargeAfterimageInterpolant > 0f)
            {
                float universalOpacity = 0.27f;
                float minClosenessInterpolant = 0.86f;

                if (CurrentPhase >= 1)
                {
                    universalOpacity = 0.43f;
                    minClosenessInterpolant = 0.76f;
                }
                if (CurrentAttack == EntropicGodAttackType.PortalChainCharges2)
                {
                    universalOpacity = 0.95f;
                    minClosenessInterpolant = 0.64f;
                }

                // Make afterimages less tight during spin charges, so that the circular motion can be better appreciated.
                if (CurrentAttack == EntropicGodAttackType.RealityWarpSpinCharge)
                {
                    universalOpacity = 0.34f;
                    minClosenessInterpolant = 0.26f;
                }
                float afterimageClosenessInterpolant = Lerp(1f, minClosenessInterpolant, ChargeAfterimageInterpolant);
                for (int i = 60; i >= 0; i--)
                {
                    // Make afterimages sharply taper off in opacity as they get longer.
                    float afterimageOpacity = ChargeAfterimageInterpolant * Pow(1f - i / 61f, 5.9f) * universalOpacity;

                    Vector2 afterimageDrawPosition = Vector2.Lerp(NPC.oldPos[i] + NPC.Size * 0.5f, NPC.Center, afterimageClosenessInterpolant) - screenPos;
                    Color afterimageColor = Color.Lerp(new(209, 155, 218, 0), Color.Black, Abs(ZPosition) * 0.35f) * afterimageOpacity;

                    // The subtraction of i / 60 acts as a correction offset, since ribs from previous frames had different time values.
                    float ribAnimationSpeed = horizontalRibOffsetTime / Main.GlobalTimeWrappedHourly;
                    DrawBody(afterimageDrawPosition, NPC.GetAlpha(afterimageColor), NPC.rotation, horizontalRibOffsetTime - i * ribAnimationSpeed / 60f);
                }
            }
            DrawBody(NPC.Center - screenPos, NPC.GetAlpha(GeneralColor), NPC.rotation, horizontalRibOffsetTime);
            DrawHead(NPC.Center - screenPos, NPC.GetAlpha(GeneralColor), NPC.rotation + HeadRotation);

            // Draw the hands.
            Texture2D handTex = handTexture.Value;
            foreach (EntropicGodHand hand in Hands)
            {
                float handRotation = NPC.AngleTo(hand.Center);

                // Make the hands aim towards the hand if close.
                Vector2 headPosition = NPC.Center + HeadOffset;
                float angleToHead = (headPosition - hand.Center).ToRotation() - Sign(hand.DefaultOffset.X) * PiOver2;
                handRotation = handRotation.AngleLerp(angleToHead, InverseLerp(148f, 60f, hand.Center.Distance(headPosition)));

                // Use the hand rotation override instead if it's defined.
                handRotation = hand.RotationOverride ?? handRotation;

                Vector2 handDrawPosition = hand.Center - screenPos;
                Rectangle frame = handTex.Frame(1, 3, 0, hand.Frame);
                SpriteEffects handDirection = hand.Center.X >= NPC.Center.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (handDirection == SpriteEffects.FlipHorizontally)
                    handRotation += Pi;

                Main.EntitySpriteDraw(handTex, handDrawPosition, frame, NPC.GetAlpha(GeneralColor), handRotation, frame.Size() * 0.5f, NPC.scale * 0.75f, handDirection, 0);
            }

            // Draw the laser telegraph area once ready.
            if (LaserTelegraphOpacity > 0f)
                DrawLaserTelegraphZone();

            return false;
        }

        public void DrawBody(Vector2 drawPosition, Color color, float rotation, float horizontalRibOffsetTime)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;

            // Draw the tendril parts.
            Main.EntitySpriteDraw(texture, drawPosition - Vector2.UnitY.RotatedBy(rotation) * NPC.scale * 48f, NPC.frame, color, rotation, NPC.frame.Size() * 0.5f, TeleportVisualsAdjustedScale, 0, 0);

            // Draw ribs.
            for (int i = 3; i >= 1; i--)
            {
                float horizontalRibOffset = Pow(Sin(horizontalRibOffsetTime + i), 3f) * 6f;

                Vector2 ribDrawOffset = new Vector2(horizontalRibOffset + 36f, i * 38f + 55f).RotatedBy(rotation) * NPC.scale;
                Texture2D ribsTexture = ribTextures[i - 1].Value;
                Main.EntitySpriteDraw(ribsTexture, drawPosition + ribDrawOffset, null, color, rotation, ribsTexture.Size() * 0.5f, TeleportVisualsAdjustedScale, SpriteEffects.FlipHorizontally, 0);

                ribDrawOffset = new Vector2(-horizontalRibOffset - 36f, i * 38f + 55f).RotatedBy(rotation) * NPC.scale;
                Main.EntitySpriteDraw(ribsTexture, drawPosition + ribDrawOffset, null, color, rotation, ribsTexture.Size() * 0.5f, TeleportVisualsAdjustedScale, 0, 0);
            }
        }

        public void DrawBack(Vector2 drawPosition, Color color, float rotation)
        {
            Texture2D back = backTexture.Value;

            drawPosition += Vector2.UnitY.RotatedBy(rotation) * NPC.scale * 6f;
            Main.EntitySpriteDraw(back, drawPosition, null, color, rotation, back.Size() * 0.5f, TeleportVisualsAdjustedScale, 0, 0);
        }

        public void DrawYankHand(Vector2 drawPosition, Color color, float rotation)
        {
            Texture2D namelessHand = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/Hand1").Value;
            Rectangle handFrame = namelessHand.Frame();

            Main.EntitySpriteDraw(namelessHand, drawPosition + HeadOffset + new Vector2(-60f, -10f), handFrame, color, rotation - PiOver4 - 0.4f, handFrame.Size() * 0.5f, Vector2.One * 0.6f, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
            Main.EntitySpriteDraw(namelessHand, drawPosition + HeadOffset + new Vector2(60f, -10f), handFrame, color, rotation + PiOver4 + 0.4f, handFrame.Size() * 0.5f, Vector2.One * 0.6f, SpriteEffects.FlipVertically, 0);
        }

        public void DrawHead(Vector2 drawPosition, Color color, float rotation)
        {
            // Draw a Nameless Deity hand pulling Noxus out of the portal if necessary.
            if (BeingYankedOutOfPortal && NoxusDeathCutsceneSystem.AnimationTimer <= 0)
            {
                float opacity = 1f - NoxusDeathCutsceneSystem.OverlayInterpolant;
                DrawYankHand(drawPosition, Color.DarkGray * opacity, rotation);
            }

            // Calculate the head scale factor. This includes a bit of squishiness if desired.
            Vector2 headScaleFactor = Vector2.One;
            headScaleFactor.Y += Cos(Main.GlobalTimeWrappedHourly * -12f) * HeadSquishiness - HeadRotation * 0.25f;

            // Draw the head.
            Texture2D head = headTexture.Value;
            Main.EntitySpriteDraw(head, drawPosition + HeadOffset, null, color, rotation, head.Size() * 0.5f, headScaleFactor * TeleportVisualsAdjustedScale, 0, 0);

            // Draw an eye gleam over the head, if said gleam is in effect.
            if (EyeGleamInterpolant > 0f)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);

                float eyePulse = Main.GlobalTimeWrappedHourly * 2.1f % 1f;
                Vector2 eyePosition = drawPosition + HeadOffset + new Vector2(-20f, 12f).RotatedBy(HeadRotation) * headScaleFactor * TeleportVisualsAdjustedScale;
                Vector2 baseEyeScale = headScaleFactor * TeleportVisualsAdjustedScale * EyeGleamInterpolant * new Vector2(0.67f, 0.59f) * 2f;
                Main.EntitySpriteDraw(SparkTexture, eyePosition, null, Color.Fuchsia * EyeGleamInterpolant, rotation, SparkTexture.Size() * 0.5f, baseEyeScale, 0, 0);
                Main.EntitySpriteDraw(SparkTexture, eyePosition, null, Color.Cyan * EyeGleamInterpolant, rotation, SparkTexture.Size() * 0.5f, baseEyeScale * new Vector2(0.7f, 1f), 0, 0);
                Main.EntitySpriteDraw(SparkTexture, eyePosition, null, Color.Violet * EyeGleamInterpolant * (1f - eyePulse), rotation, SparkTexture.Size() * 0.5f, baseEyeScale * new Vector2(eyePulse * 2f + 1f, eyePulse + 1f), 0, 0);

                Main.spriteBatch.ResetToDefault();
            }

            // Draw the big eye over the head if necessary.
            Texture2D eye = eyeTexture.Value;
            if (BigEyeOpacity > 0f)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);

                float eyePulse = Main.GlobalTimeWrappedHourly * 1.3f % 1f;
                Vector2 baseEyeScale = headScaleFactor * TeleportVisualsAdjustedScale * BigEyeOpacity * 0.15f;
                Vector2 eyePosition = drawPosition + HeadOffset + new Vector2(19f, -12f).RotatedBy(HeadRotation) * baseEyeScale;
                Main.EntitySpriteDraw(eye, eyePosition, null, Color.BlueViolet.MultiplyRGBA(color) * BigEyeOpacity, rotation, eye.Size() * 0.5f, baseEyeScale, 0, 0);
                Main.EntitySpriteDraw(eye, eyePosition, null, Color.MidnightBlue.MultiplyRGBA(color) * BigEyeOpacity * (1f - eyePulse), rotation, eye.Size() * 0.5f, baseEyeScale * (eyePulse * 0.39f + 1f), 0, 0);

                Main.spriteBatch.ResetToDefault();
            }
        }

        public void DrawLaserTelegraphZone()
        {
            Texture2D telegraphBorder = telegraphBorderTexture.Value;
            Main.spriteBatch.UseBlendState(BlendState.Additive);

            Vector2 telegraphBorderDrawPosition = new(NPC.Center.X - Main.screenPosition.X, Main.screenHeight * 0.5f);
            Vector2 scale = Main.ScreenSize.ToVector2() / telegraphBorderTexture.Size();
            Vector2 origin = new(0f, 0.5f);
            SpriteEffects direction = SpriteEffects.FlipHorizontally;
            if (LaserSpinDirection > 0f)
            {
                origin.X = 1f - origin.X;
                direction = SpriteEffects.None;
            }
            Main.spriteBatch.Draw(telegraphBorder, telegraphBorderDrawPosition, null, Color.MediumPurple * LaserTelegraphOpacity * 0.45f, 0f, telegraphBorder.Size() * origin, scale, direction, 0f);
            Main.spriteBatch.Draw(telegraphBorder, telegraphBorderDrawPosition, null, Color.Fuchsia * LaserTelegraphOpacity * 0.2f, 0f, telegraphBorder.Size() * origin, scale, direction, 0f);
            Main.spriteBatch.DrawBloomLine(NPC.Center - Vector2.UnitY * 4000f, NPC.Center + Vector2.UnitY * 4000f, Color.Purple * LaserTelegraphOpacity, LaserTelegraphOpacity * 30f);

            Main.spriteBatch.ResetToDefault();
        }

        public void DrawDecal(Vector2 drawPosition, Color decalColor, float rotation)
        {
            Color baseColor = decalColor;
            DrawBack(drawPosition, baseColor * 0.3f, rotation);
            DrawBody(drawPosition, baseColor, rotation, 0f);
            DrawHead(drawPosition, baseColor, rotation);
        }
    }
}
