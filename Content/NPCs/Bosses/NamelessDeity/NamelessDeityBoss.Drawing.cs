using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets.Fonts;
using NoxusBoss.Common.Easings;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Common.VerletIntergration;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        /// <summary>
        /// The scale of Nameless' side flowers. This should be randomized slightly when the flowers are switched.
        /// </summary>
        public float SideFlowerScale
        {
            get;
            set;
        } = 1f;

        /// <summary>
        /// The fan animation timer.
        /// </summary>
        public float FanAnimationTimer
        {
            get;
            set;
        }

        /// <summary>
        /// The animation speed of fins that have a fan shape. Defaults to 1.
        /// </summary>
        public float FanAnimationSpeed
        {
            get;
            set;
        } = 1f;

        /// <summary>
        /// Whether Nameless should display his "You have passed the test." dialog over the screen.
        /// </summary>
        public bool DrawCongratulatoryText
        {
            get;
            set;
        }

        /// <summary>
        /// A 0-1 interpolant that determines how strongly a black overlay should be applied to the screen during the death animation.
        /// </summary>
        public float UniversalBlackOverlayInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The current world position of the censor box.
        /// </summary>
        public Vector2 CensorPosition
        {
            get;
            set;
        }

        /// <summary>
        /// The handler for Nameless' wing set.
        /// </summary>
        public NamelessDeityWingSet Wings
        {
            get;
            set;
        }

        /// <summary>
        /// The left vine's underlying rope.
        /// </summary>
        public VerletSimulatedRope LeftVine
        {
            get;
            set;
        }

        /// <summary>
        /// The right vine's underlying rope.
        /// </summary>
        public VerletSimulatedRope RightVine
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether Nameless' hands should be drawn manually, separate from the render target.<br></br>
        /// This is necessary because in certain contexts the hands get cut off by the render target bounds. Exact reasons are explained in this getter's definition.
        /// </summary>
        public bool ShouldDrawHandsManually
        {
            get
            {
                // Hands are detached from Nameless when they're crushing the star.
                if (CurrentState == NamelessAIType.CrushStarIntoQuasar && NPC.dontTakeDamage)
                    return true;

                // Nameless is not visible during the screen opening intro state, and even if he was who knows where he is during that.
                if (CurrentState == NamelessAIType.OpenScreenTear)
                    return true;

                // Punches are independent in position from Nameless.
                if (CurrentState == NamelessAIType.RealityTearPunches)
                    return true;

                // By default hands should be drawn to the render target.
                return false;
            }
        }

        /// <summary>
        /// Safely resets the <see cref="Main.spriteBatch"/> in a way that works for render targets.
        /// </summary>
        private static void ResetSpriteBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        }

        /// <summary>
        /// Safely resets the <see cref="Main.spriteBatch"/> blend state in a way that works for render targets.
        /// </summary>
        private static void SetBlendState(BlendState blendState)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendState, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        }

        /// <summary>
        /// Represents the spacing between Nameless' side flowers.
        /// </summary>
        public static Vector2 FlowerSpacing => Vector2.UnitX * 280f;

        /// <summary>
        /// Calculates the center point for Nameless' side flowers.
        /// </summary>
        /// <param name="screenPos">The position of the screen.</param>
        /// <param name="drawPositionOverride">An optional draw position override.</param>
        public Vector2 FlowerDrawCenter(Vector2 screenPos, Vector2? drawPositionOverride = null) =>
            (drawPositionOverride ?? (NPC.Center - screenPos)) + Vector2.UnitY * 24f;

        /// <summary>
        /// Calculates the world position for Nameless' left side flower.
        /// </summary>
        /// <param name="screenPos">The position of the screen.</param>
        /// <param name="drawPositionOverride">An optional draw position override.</param>
        public Vector2 LeftFlowerDrawPosition(Vector2 screenPos, Vector2? drawPositionOverride = null) => FlowerDrawCenter(screenPos, drawPositionOverride) - FlowerSpacing;

        /// <summary>
        /// Calculates the world position for Nameless' right side flower.
        /// </summary>
        /// <param name="screenPos">The position of the screen.</param>
        /// <param name="drawPositionOverride">An optional draw position override.</param>
        public Vector2 RightFlowerDrawPosition(Vector2 screenPos, Vector2? drawPositionOverride = null) => FlowerDrawCenter(screenPos, drawPositionOverride) + FlowerSpacing;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Ensure that bestiary dummies are visible, have a censor, and various things like wings are update.
            float opacityFactor = 1f;
            if (NPC.IsABestiaryIconDummy)
            {
                NPC.position.Y += NPC.scale * 100f;
                CensorPosition = IdealCensorPosition;
                NPC.Opacity = 1f;
                Wings.Update(WingMotionState.Flap, Main.GlobalTimeWrappedHourly % 1f);
                NPC.scale = 0.175f;

                UpdateSwappableTextures();
                FightLength++;

                if (FightLength % 120 == 37)
                    RerollAllSwappableTextures();

                NamelessDeityTargetManager.BestiaryDummy = NPC;
                NamelessDeityTargetManager.DrawBestiaryDummy = true;
            }

            // Draw all afterimages.
            else
            {
                // Become a bit more transparent as afterimages draw.
                opacityFactor -= AfterimageSpawnChance * AfterimageOpacityFactor * 0.1f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, SubtractiveBlending, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                var afterimages = AllProjectilesByID(ModContent.ProjectileType<NamelessDeityAfterimage>());
                foreach (Projectile afterimage in afterimages)
                    afterimage.As<NamelessDeityAfterimage>().DrawSelf();
                Main.spriteBatch.ResetToDefault();
            }

            // Draw the Nameless Deity target.
            DrawSelfFromTarget(screenPos, opacityFactor);

            // Draw the top hat if the player's name is "Blast" as a "dev preset".
            if (NamelessDeityFormPresetRegistry.UsingBlastPreset && !NPC.IsABestiaryIconDummy)
                DrawTopHat(screenPos);

            // Draw the censor.
            if ((UsedPreset?.UseCensor ?? true) || NPC.IsABestiaryIconDummy)
                DrawProtectiveCensor(screenPos, true);

            // Draw the hands manually without regard for the render target if necessary.
            if (ShouldDrawHandsManually)
                DrawHands(screenPos, Vector2.Zero, false);

            DrawDeathAnimationCutscene(screenPos);

            return false;
        }

        public void PrepareGeneralOverlayShader(Texture2D target)
        {
            // Use a pixelation shader by default.
            if ((UsedPreset?.ShaderOverlayEffect ?? null) is null)
            {
                var pixelationShader = ShaderManager.GetShader("PixelationShader");
                pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 1.75f / target.Size());
                pixelationShader.Apply();
            }

            // If a special shader overlay effect is specified, use that instead.
            else
                UsedPreset.ShaderOverlayEffect(target);
        }

        public void DrawSelfFromTarget(Vector2 screenPos, float opacityFactor)
        {
            // Prepare the overlay shader if this isn't a bestiary dummy.
            var target = NamelessDeityTargetManager.NamelessDeityTarget;
            if (!NPC.IsABestiaryIconDummy)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                PrepareGeneralOverlayShader(target);
            }

            // Calculate the base color.
            Color color = Color.Lerp(Color.White, Color.White with { A = 0 }, Sqrt(1f - opacityFactor));

            // Make the color darker and more transparent as Nameless enters the background, to give the illusion of actually entering it and not just becoming tiny.
            color = Color.Lerp(color, new(141, 96, 92), InverseLerp(2.1f, 6f, ZPosition)) * Remap(ZPosition, 4f, 9f, 1f, 0.6f);

            // Apply relative darkening.
            color = Color.Lerp(color, Color.Black, RelativeDarkening);

            // Draw the target contents.
            Main.spriteBatch.Draw(target, NPC.Center - screenPos, null, NPC.GetAlpha(color) * opacityFactor, NPC.rotation, target.Size() * 0.5f, TeleportVisualsAdjustedScale, 0, 0f);

            // Reset the sprite batch.
            if (!NPC.IsABestiaryIconDummy)
                Main.spriteBatch.ResetToDefault();
        }

        public void DrawSelf(Vector2 screenPos, Vector2? drawPosition = null)
        {
            if (CurrentState == NamelessAIType.OpenScreenTear)
            {
                DrawHands(screenPos, Vector2.Zero);
                return;
            }

            // Check for clocks for later.
            var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());

            // Back-to-front draw priority for Nameless' body parts is as follows:
            // 001. Xeroc eye symbol.

            // 002. Dangling plants. This can be any of the following:
            // 02a. Lavender.
            // 02b. Thick autumn bush vine.
            // 02c. Yellow leaf vine.
            // 02d. Lush green leaf vine.
            // 02e. Pale vine.

            // 003. Wheel. This can be any of the following:
            // 03a. Wooden Wheel.
            // 03b. The dharmachakra.
            // 03c. Exo Prism Wheel.
            // 03d. Ship's Wheel.
            // 03e. Time-recording Watch.
            // 03f. Abstract Sun Symbol.

            // 004. Fins. This can be any of the following:
            // 04a. Orange, scale-y fins.
            // 04b. Green/gold fans. (Yes, fans, not fins).
            // 04c. Reptilian-like wings.
            // 04d. Red/gold fans. (Yes, fans, not fins).
            // 04e. Feather-like blue/red fans. (Yes, fans, not fins).

            // 005. Wings. This can be any of the following:
            // 05a. Owl wings.
            // 05b. Angel wings.
            // 05c. Blue Bird wings.
            // 05d. Raven wings.
            // 05e. Red/gold clay textured wings.

            // 006. Wing side flowers. This can be any of the following:
            // 06a. Lotus flowers.
            // 06b. Marigold flowers.
            // 06c. White Daisy flowers.
            // 06d. Orange Daisy flowers.
            // 06e. Pink Rose flowers.
            // 06f. Clock.

            // 007. Plants on top of side flowers.

            // 008. Silk Scarf. These have 4 variants, each of the same general shape but with minor distortions.

            // 009. "Antlers". This can be any of the following:
            // 09a. Standard antlers 1.
            // 09b. Standard antlers 2.
            // 09c. Standard antlers 3.
            // 09d. Standard antlers 4.
            // 09e. Sakura branches.
            // 09f. Dried antlers.
            // 09g. Standard tree branches.

            // 010. Head Hands.

            // 011. Hands.

            // 012. The main body. This is mostly obscured by the censor.

            // 013. Atlas Moth.

            // 014. Eye Flower.

            // 015. Halo Ring.

            // 016. Censor. This one is actually smaller than the true censor in PreDraw as a hard "You cannot see this" effect in case the main one drifts too much somehow.

            DrawGlowingXerocEyeSymbol(screenPos, drawPosition);

            // Things get omega crusty and bad without this premultiplication step.
            SetBlendState(BlendState.NonPremultiplied);

            Vector2 drawOffset = Vector2.Zero;
            if (drawPosition is not null)
                drawOffset = screenPos + drawPosition.Value - NPC.Center;

            HandleDanglingPlantRotation();
            DrawDanglingVines(drawOffset);
            DrawWheel(screenPos, clocks, drawPosition);
            DrawFins(screenPos, drawPosition);
            DrawWings(screenPos, drawPosition);
            DrawFlowers(screenPos, clocks, drawPosition);
            DrawSidePlantsAboveFlower(screenPos, drawPosition);
            DrawSilkScarf(screenPos, drawPosition);
            DrawAntlers(screenPos, drawPosition);
            DrawHeadHands(screenPos, drawPosition);
            if (UniversalBlackOverlayInterpolant < 1f && !ShouldDrawHandsManually)
                DrawHands(screenPos, drawOffset);
            DrawBody(screenPos, drawPosition);
            DrawAtlasMoth(screenPos, drawPosition);
            DrawEye(screenPos, drawPosition);

            ResetSpriteBatch();

            DrawBrightRing(screenPos, drawPosition);
            DrawProtectiveCensor(screenPos, false, drawPosition);
        }

        public void DrawGlowingXerocEyeSymbol(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the eye statically and with a decent amount of glow.
            float opacity = CurrentState == NamelessAIType.SunBlenderBeams ? 0.35f : (ZPosition * 0.12f + 0.6f);
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 eyeDrawPosition = baseDrawPosition - Vector2.UnitY * 32f;
            Texture2D eye = EyeFullTexture.Value;
            Main.EntitySpriteDraw(eye, eyeDrawPosition, null, (Color.White with { A = 0 }) * opacity, 0f, eye.Size() * 0.5f, 0.8f, 0);
        }

        public void DrawWheel(Vector2 screenPos, IEnumerable<Projectile> clocks, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the wheel as a forever spinning object.
            float wheelRotation = Main.GlobalTimeWrappedHourly * 3f;

            // Use time based on the clock constellation if it's present.
            if (CurrentState == NamelessAIType.ClockConstellation && clocks.Any())
            {
                Projectile clock = clocks.First();
                wheelRotation *= clock.As<ClockConstellation>().TimeIsReversed.ToDirectionInt() * (!ClockConstellation.TimeIsStopped).ToInt();
            }

            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 wheelDrawOffset = -Vector2.UnitY * 310f;
            Vector2 wheelDrawPosition = baseDrawPosition + wheelDrawOffset;
            Texture2D wheel = WheelTexture.UsedTexture;
            Main.EntitySpriteDraw(wheel, wheelDrawPosition, null, Color.White, wheelRotation, wheel.Size() * 0.5f, 1f, 0);
        }

        public void DrawSilkScarf(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the wheel overlay statically.
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 scarfDrawOffset = -Vector2.UnitY * 272f;
            Vector2 scarfDrawPosition = baseDrawPosition + scarfDrawOffset;
            Texture2D scarf = ScarfTexture.UsedTexture;
            Main.EntitySpriteDraw(scarf, scarfDrawPosition, null, Color.White, 0f, scarf.Size() * 0.5f, 1f, 0);
        }

        public void DrawAntlers(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the antlers statically.
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 antlerDrawOffset = -Vector2.UnitY * 254f;
            Vector2 antlerDrawPosition = baseDrawPosition + antlerDrawOffset;
            Texture2D antlers = AntlersTexture.UsedTexture;
            Main.EntitySpriteDraw(antlers, antlerDrawPosition, null, Color.White, 0f, antlers.Size() * 0.5f, 1f, 0);
        }

        public void DrawHeadHands(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the head hands with a cyclic contracting motion.
            float handRotationOffset = Lerp(-0.05f, 0.23f, Cos01(Main.GlobalTimeWrappedHourly * 0.4f));
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 handDrawCenter = baseDrawPosition - Vector2.UnitY * 266f;
            Vector2 handSpacing = Vector2.UnitX * 70f;
            Vector2 leftHandDrawPosition = handDrawCenter - handSpacing;
            Vector2 rightHandDrawPosition = handDrawCenter + handSpacing;
            Texture2D hand = HeadHandsTexture.Value;

            // Draw each hand separately.
            Main.EntitySpriteDraw(hand, leftHandDrawPosition, null, Color.White, -handRotationOffset, hand.Size() * new Vector2(1f, 0.5f), 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(hand, rightHandDrawPosition, null, Color.White, handRotationOffset, hand.Size() * new Vector2(0f, 0.5f), 1f, SpriteEffects.FlipHorizontally);
        }

        public void HandleDanglingPlantRotation()
        {
            Vector2 vineDrawCenter = NPC.Center + Vector2.UnitY * 8f;
            Vector2 vineSpacing = Vector2.UnitX * 90f;
            Vector2 leftVinePosition = vineDrawCenter - vineSpacing;
            Vector2 rightVinePosition = vineDrawCenter + vineSpacing;

            LeftVine ??= new(leftVinePosition, Vector2.Zero, 6, 700f);
            RightVine ??= new(rightVinePosition, Vector2.Zero, 6, 700f);

            // Apply forces to the end of the rope.
            Vector2 shakeForce = (Main.GlobalTimeWrappedHourly * 40f).ToRotationVector2() * Clamp(OverallShakeIntensity * 2f, 0f, 20f);
            LeftVine.Rope[^3].Position.X += Sin(Main.GlobalTimeWrappedHourly * 2.3f) * 12f - 3.2f;
            RightVine.Rope[^3].Position.X += Sin(Main.GlobalTimeWrappedHourly * 2.4f + 1.887f) * 12f - 3.2f;
            LeftVine.Rope[^3].Position += shakeForce;
            RightVine.Rope[^3].Position += shakeForce;

            // Update the vines.
            float vineGravity = Main.LocalPlayer.gravDir * 0.65f;
            LeftVine.Update(leftVinePosition, vineGravity);
            RightVine.Update(rightVinePosition, vineGravity);
        }

        public void DrawDanglingVines(Vector2 drawOffset)
        {
            // Draw the vines swaying as Nameless moves.
            Texture2D vine = VinesTexture.UsedTexture;

            // Draw each dangling part separately.
            LeftVine?.DrawProjection(vine, drawOffset, false, _ => Color.White * NPC.Opacity, NamelessDeityTargetManager.NamelessDeityTarget.Width, NamelessDeityTargetManager.NamelessDeityTarget.Height);
            RightVine?.DrawProjection(vine, drawOffset, true, _ => Color.White * NPC.Opacity, NamelessDeityTargetManager.NamelessDeityTarget.Width, NamelessDeityTargetManager.NamelessDeityTarget.Height);
        }

        public void DrawWings(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Calculate wing textures.
            Texture2D wingsTexture = WingsTexture.UsedTexture;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.84f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.White;

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathF.Min(0.7f, Math.Abs(Wings.RotationDifferenceMovingAverage) * 3.6f);

            // Calculate rotation values.
            float wingRotation = Wings.Rotation;

            // Draw the wings.
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * 70f;
            Vector2 scale = new Vector2(1f, 1f - squishOffset) * new Vector2(1.35f, 1.1f);
            if (WingsTexture.TextureVariant == 2)
                scale.X *= 0.8f;

            Main.spriteBatch.Draw(wingsTexture, drawPosition - Vector2.UnitX * 86f, null, wingsDrawColor, wingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(wingsTexture, drawPosition + Vector2.UnitX * 86f, null, wingsDrawColor, -wingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
        }

        public void DrawFins(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Make the fan animation speed approach normalcy.
            if (!Main.gamePaused)
                FanAnimationSpeed = Lerp(FanAnimationSpeed, 1f, 0.04f);

            // Update the fan animation.
            FanAnimationTimer += TwoPi * FanAnimationSpeed / 120f;
            if (FanAnimationTimer >= TwoPi * 10000f)
                FanAnimationTimer = 0f;

            // Draw the fins with weak swaying motions.
            float scaleFactor = 1.3f;
            bool isFan = false;
            Texture2D fins = FinsTexture.UsedTexture;
            Vector2 finPivotLeft = fins.Size() * 0.5f;
            switch (FinsTexture.TextureVariant)
            {
                case 0:
                    finPivotLeft = fins.Size() * new Vector2(0.7878f, 0.3625f);
                    scaleFactor = 1f;
                    break;
                case 1:
                    finPivotLeft = fins.Size() * new Vector2(0.5637f, 0.4963f);
                    isFan = true;
                    break;
                case 2:
                    finPivotLeft = fins.Size() * new Vector2(0.701f, 0.5263f);
                    scaleFactor = 1.15f;
                    break;
                case 3:
                    finPivotLeft = fins.Size() * new Vector2(0.6434f, 0.238f);
                    scaleFactor = 0.8f;
                    isFan = true;
                    break;
                case 4:
                    finPivotLeft = fins.Size() * new Vector2(0.6532f, 0.4237f);
                    scaleFactor = 1.31f;
                    isFan = true;
                    break;
            }
            Vector2 finPivotRight = new(fins.Width - finPivotLeft.X, finPivotLeft.Y);

            // Draw both fins.
            float finRotation = 0f;
            float squishOffset = Clamp(NPC.velocity.Length() * 0.033f, 0f, 0.1f);
            if (isFan)
            {
                squishOffset = Sin01(FanAnimationTimer) * 0.3f;
                finRotation += Sin(FanAnimationTimer / FanAnimationSpeed + 0.54f) * 0.07f;
            }

            Vector2 scale = new Vector2(1f, 1f - squishOffset) * scaleFactor * 0.875f;
            Main.EntitySpriteDraw(fins, LeftFlowerDrawPosition(screenPos, baseDrawPositionOverride), null, Color.White, -finRotation, finPivotLeft, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(fins, RightFlowerDrawPosition(screenPos, baseDrawPositionOverride), null, Color.White, finRotation, finPivotRight, scale, SpriteEffects.FlipHorizontally);
        }

        public void DrawFlowers(Vector2 screenPos, IEnumerable<Projectile> clocks, Vector2? baseDrawPositionOverride = null)
        {
            Texture2D flower = SideFlowerTexture.UsedTexture;
            bool isClockFlower = SideFlowerTexture.TextureVariant == 5;

            // Calculate individualized flower draw variables for scale and rotation.
            Vector2 baseScale = Vector2.One * SideFlowerScale;
            float leftRotation = Lerp(-0.2f, 0.67f, Cos01(Main.GlobalTimeWrappedHourly * 0.6f)) - Main.GlobalTimeWrappedHourly * 2.84f;
            Vector2 leftScale = baseScale * Lerp(0.95f, 1.05f, Cos01(Main.GlobalTimeWrappedHourly * 0.45f));
            float rightRotation = Lerp(-0.2f, 0.67f, Cos01(Main.GlobalTimeWrappedHourly * 0.59f)) + Main.GlobalTimeWrappedHourly * 2.84f;
            Vector2 rightScale = baseScale * Lerp(0.95f, 1.05f, Cos01(Main.GlobalTimeWrappedHourly * 0.46f));

            void drawFlowerAtPosition(Vector2 drawPosition, float rotation, Vector2 scale)
            {
                // SPECIAL CASE: If this is actually a clock, don't spin the clock in place, spin its hands instead.
                float minuteHandRotation = Main.GlobalTimeWrappedHourly * 9f;
                float hourHandRotation = minuteHandRotation / 12f;
                if (isClockFlower)
                    rotation = 0f;

                // Use time based on the clock constellation if it's present.
                if (CurrentState == NamelessAIType.ClockConstellation && clocks.Any())
                {
                    Projectile clock = clocks.First();
                    minuteHandRotation = clock.As<ClockConstellation>().MinuteHandRotation;
                    hourHandRotation = clock.As<ClockConstellation>().HourHandRotation - 3.91765f + PiOver2;

                    if (!isClockFlower)
                        leftRotation *= clock.As<ClockConstellation>().TimeIsReversed.ToDirectionInt() * (!ClockConstellation.TimeIsStopped).ToInt();
                }

                Main.EntitySpriteDraw(flower, drawPosition, null, Color.White, rotation, flower.Size() * 0.5f, scale, 0);

                if (isClockFlower)
                {
                    Vector2 minuteHandOrigin = new(0f, 12f);
                    Vector2 hourHandOrigin = new(45f, 3f);
                    Main.EntitySpriteDraw(FlowerClockHourHandTexture.Value, drawPosition, null, Color.White, hourHandRotation, hourHandOrigin, scale, 0);
                    Main.EntitySpriteDraw(FlowerClockMinuteHandTexture.Value, drawPosition, null, Color.White, minuteHandRotation, minuteHandOrigin, scale, 0);
                }
            }

            // Perform the draw calls.
            drawFlowerAtPosition(LeftFlowerDrawPosition(screenPos, baseDrawPositionOverride), leftRotation, leftScale);
            drawFlowerAtPosition(RightFlowerDrawPosition(screenPos, baseDrawPositionOverride), rightRotation, rightScale);
        }

        public void DrawSidePlantsAboveFlower(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the side plants almost statically, with a tiny bit of vertical offset over time.
            float verticalOffset = Sin(Main.GlobalTimeWrappedHourly * 0.1f) * 4f + 60f;
            Vector2 baseDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Vector2 plantDrawOffset = -Vector2.UnitY * verticalOffset;
            Vector2 plantDrawPosition = baseDrawPosition + plantDrawOffset;
            Texture2D plant = FlowerTopTexture.Value;
            Main.EntitySpriteDraw(plant, plantDrawPosition, null, Color.White, 0f, plant.Size() * 0.5f, 1f, 0);
        }

        public void DrawBody(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the body statically.
            Vector2 bodyDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Texture2D body = DivineBodyTexture.Value;
            Main.EntitySpriteDraw(body, bodyDrawPosition, null, Color.White, 0f, body.Size() * 0.5f, 1f, 0);
        }

        public void DrawAtlasMoth(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the moth with the flapping via a scaling illusion.
            Vector2 mothDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            mothDrawPosition.Y -= 226f;

            // Create the wing scale easing curve.
            var wingFlapCurve = new PiecewiseCurve().
                Add(PolynomialEasing.Quartic, EasingType.In, 0.03f, 0.3f, 1f).
                Add(PolynomialEasing.Sextic, EasingType.Out, 1.04f, 0.7f).
                Add(PolynomialEasing.Quadratic, EasingType.Out, 1f, 1f);
            float flapScale = wingFlapCurve.Evaluate(Main.GlobalTimeWrappedHourly * 0.9f % 1f);

            // Draw the wings.
            Texture2D wings = AtlasMothWingTexture.Value;
            Vector2 wingSpacing = Vector2.UnitX * 6f;
            Vector2 leftWingDrawPosition = mothDrawPosition - wingSpacing;
            Vector2 rightWingDrawPosition = mothDrawPosition + wingSpacing;
            Vector2 wingScale = 1f * new Vector2(flapScale, 1f - Abs(flapScale - 1f) * 0.12f);
            Main.EntitySpriteDraw(wings, leftWingDrawPosition, null, Color.White, 0f, wings.Size() * new Vector2(0f, 0.5f), wingScale, SpriteEffects.None);
            Main.EntitySpriteDraw(wings, rightWingDrawPosition, null, Color.White, 0f, wings.Size() * new Vector2(1f, 0.5f), wingScale, SpriteEffects.FlipHorizontally);

            // Draw the body.
            Texture2D body = AtlasMothBodyTexture.Value;
            Main.EntitySpriteDraw(body, mothDrawPosition, null, Color.White, 0f, body.Size() * 0.5f, 1f, 0);
        }

        public void DrawEye(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the eye flower with a pulsation motion.
            float eyePulse = Lerp(0.9f, 1.1f, Cos01(Main.GlobalTimeWrappedHourly * 1.95f));
            Vector2 eyeDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            eyeDrawPosition.Y -= 226f;

            Texture2D eye = EyeOfEternityTexture.Value;
            Main.EntitySpriteDraw(eye, eyeDrawPosition, null, Color.White, 0f, eye.Size() * 0.5f, eyePulse, 0);
        }

        public void DrawBrightRing(Vector2 screenPos, Vector2? baseDrawPositionOverride = null)
        {
            float ringOpacity = ZPosition * 0.1f + 0.6f;
            Vector2 ringDrawPosition = baseDrawPositionOverride ?? (NPC.Center - screenPos);
            Texture2D ring = GlowRingTexture.Value;
            Main.EntitySpriteDraw(ring, ringDrawPosition, null, (Color.White with { A = 0 }) * ringOpacity, 0f, ring.Size() * 0.5f, 0.67f, 0);
        }

        public static void DrawDeathAnimationTerminationText()
        {
            var font = FontRegistry.Instance.NamelessDeityText;
            string text = Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityEndScreenSkipText");
            float scale = 0.6f;
            float maxHeight = 200f;
            Vector2 textSize = font.MeasureString(text);
            if (textSize.Y > maxHeight)
                scale = maxHeight / textSize.Y;
            Vector2 textDrawPosition = Main.ScreenSize.ToVector2() * 0.8f;
            textDrawPosition -= textSize * scale * new Vector2(1f, 0.5f);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, textDrawPosition, DialogColorRegistry.NamelessDeityTextColor, 0f, Vector2.Zero, new(scale), -1f, 2f);
        }

        public void DrawHands(Vector2 screenPos, Vector2 drawOffset, bool drawingToTarget = true)
        {
            if (DrawCongratulatoryText)
                return;

            if (CurrentState == NamelessAIType.DeathAnimation && NPC.ai[2] == 1f)
                return;

            bool presetOverridesHands = UsedPreset is not null && UsedPreset.PreferredHandTextures is not null;
            Texture2D handTexture = HandTexture.UsedTexture;
            if ((CurrentState is NamelessAIType.RealityTearPunches or NamelessAIType.SwordConstellation or NamelessAIType.Glock) && !presetOverridesHands)
            {
                handTexture = Fist2Texture.Value;
                HandTexture.TextureVariant = 4;
            }

            if (drawingToTarget)
                SetBlendState(BlendState.NonPremultiplied);
            else
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                PrepareGeneralOverlayShader(handTexture);
            }

            // Draw the hands.
            Texture2D forearmTexture = ForearmTextures[ForearmTexture.TextureVariant].Value;
            Texture2D armTexture = ArmTextures[ArmTexture.TextureVariant].Value;
            float zPositionOpacity = Remap(ZPosition, 0f, 2.1f, 1f, 0.58f);
            foreach (NamelessDeityHand hand in Hands)
            {
                if (hand.HasArms && CurrentState == NamelessAIType.RealityTearPunches && !drawingToTarget)
                    continue;

                float opacity = CurrentState == NamelessAIType.OpenScreenTear ? 1f : NPC.Opacity;
                Vector2 scale = Vector2.One * (CurrentState == NamelessAIType.OpenScreenTear ? 0.5f : 1f);
                hand.Draw(screenPos - drawOffset, NPC.Center, scale, CurrentState, ZPosition, zPositionOpacity, opacity, HandTexture.TextureVariant, handTexture, forearmTexture, armTexture);
            }

            if (drawingToTarget)
                ResetSpriteBatch();
            else
                Main.spriteBatch.ResetToDefault();
        }

        public void DrawProtectiveCensor(Vector2 screenPos, bool bigOverlay, Vector2? baseDrawPositionOverride = null)
        {
            // Draw the censor.
            if ((UsedPreset?.UseCensor ?? true) || NPC.IsABestiaryIconDummy)
            {
                Texture2D censor = UsedPreset?.CensorReplacementTexture?.Value?.Value ?? WhitePixel;
                Color censorColor = (censor == WhitePixel ? Color.Black : Color.White) * Pow(NPC.Opacity, 0.2f);

                if (bigOverlay)
                {
                    Vector2 censorScale = new Vector2(268f, 356f) / censor.Size();
                    Vector2 censorDrawPosition = baseDrawPositionOverride ?? (CensorPosition - screenPos + NPC.velocity);
                    Main.EntitySpriteDraw(censor, censorDrawPosition, null, censorColor, 0f, censor.Size() * 0.5f, censorScale * TeleportVisualsAdjustedScale, 0);
                }
                else
                {
                    Vector2 censorScale = new Vector2(150f, 150f) / censor.Size();
                    Vector2 censorDrawPosition = (baseDrawPositionOverride ?? (NPC.Center - screenPos + NPC.velocity)) - Vector2.UnitY * 166f;
                    Main.EntitySpriteDraw(censor, censorDrawPosition, null, censorColor, 0f, censor.Size() * 0.5f, censorScale, 0);
                }
            }
        }

        public void DrawTopHat(Vector2 screenPos)
        {
            Texture2D topHat = TopHatTexture.Value;
            Vector2 topHatDrawPosition = IdealCensorPosition - screenPos - Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale * 240f;
            Main.EntitySpriteDraw(topHat, topHatDrawPosition, null, NPC.GetAlpha(Color.White), NPC.rotation, topHat.Size() * 0.5f, TeleportVisualsAdjustedScale * 0.7f, 0);
        }

        public void DrawDeathAnimationCutscene(Vector2 screenPos)
        {
            // Draw the universal black overlay if necessary.
            bool screenShattered = NPC.ai[2] == 1f;
            if (UniversalBlackOverlayInterpolant > 0f)
            {
                Vector2 overlayScale = Vector2.One * Lerp(0.1f, 15f, UniversalBlackOverlayInterpolant);
                Color overlayColor = ZPosition <= -0.7f ? Color.Transparent : Color.Black;
                if (screenShattered)
                {
                    float overlayInterpolant = Sin01(Main.GlobalTimeWrappedHourly * 7f) * 0.37f + 0.43f;
                    overlayInterpolant = Lerp(overlayInterpolant, 1f, InverseLerp(240f, 0f, TimeSinceScreenSmash));
                    overlayColor = Color.Lerp(Color.Wheat, Color.White, overlayInterpolant);

                    Main.spriteBatch.PrepareForShaders(BlendState.NonPremultiplied);

                    var staticShader = ShaderManager.GetShader("StaticOverlayShader");
                    staticShader.TrySetParameter("staticInterpolant", Pow(InverseLerp(0f, 240f, TimeSinceScreenSmash), 2f));
                    staticShader.TrySetParameter("staticZoomFactor", InverseLerp(0f, 210f, TimeSinceScreenSmash) * 6f + 3f);
                    staticShader.SetTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/noise"), 1, SamplerState.PointWrap);
                    staticShader.Apply();

                    overlayScale = Vector2.One * Remap(TimeSinceScreenSmash, 0f, 210f, 6f, 10f);
                }

                for (int i = 0; i < 3; i++)
                    Main.spriteBatch.Draw(BloomCircle, Main.ScreenSize.ToVector2() * 0.5f, null, overlayColor * Sqrt(UniversalBlackOverlayInterpolant), 0f, BloomCircle.Size() * 0.5f, overlayScale, 0, 0f);

                if (screenShattered)
                    Main.spriteBatch.ResetToDefault();
            }

            // Draw extra text about terminating the attack if Nameless has been defeated before.
            if (Main.netMode == NetmodeID.SinglePlayer && WorldSaveSystem.HasDefeatedNamelessDeity && CurrentState == NamelessAIType.DeathAnimation && NPC.ai[3] == 1f)
                DrawDeathAnimationTerminationText();

            // Draw congratulatory text if necessary.
            DynamicSpriteFont font = FontRegistry.Instance.NamelessDeityText;
            if (DrawCongratulatoryText)
            {
                string text = Language.GetTextValue("Mods.NoxusBoss.Dialog.NamelessDeityEndScreenText");
                float scale = 0.8f;
                float maxHeight = 225f;
                Vector2 textSize = font.MeasureString(text);
                if (textSize.Y > maxHeight)
                    scale = maxHeight / textSize.Y;
                Vector2 textDrawPosition = Main.ScreenSize.ToVector2() * 0.5f - textSize * scale * 0.5f;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, textDrawPosition, DialogColorRegistry.NamelessDeityTextColor, 0f, Vector2.Zero, new(scale), -1f, 2f);
            }
            else if (UniversalBlackOverlayInterpolant >= 1f && !screenShattered)
            {
                DrawHands(screenPos, Vector2.Zero, false);

                // Draw fire particles manually since they'd be obscured otherwise.
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                foreach (Particle p in ParticleManager.activeParticles)
                {
                    if (p is not HeavySmokeParticle t)
                        continue;

                    t.Draw();
                }

                Main.spriteBatch.ResetToDefault();
            }
        }

        public static void ApplyMoonburnBlueEffect(Texture2D target)
        {
            var blueShader = ShaderManager.GetShader("MoonburnBlueOverlayShader");
            blueShader.TrySetParameter("swapHarshness", 0.85f);
            blueShader.Apply();
        }

        public static void ApplyMyraGoldEffect(Texture2D target)
        {
            var goldShader = ShaderManager.GetShader("MyraGoldOverlayShader");
            goldShader.TrySetParameter("swapHarshness", 0.5f);
            goldShader.Apply();
        }
    }
}
