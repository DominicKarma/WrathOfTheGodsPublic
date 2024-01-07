using Microsoft.Xna.Framework;
using NoxusBoss.Assets.Fonts;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.Projectiles.Visuals;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.Graphics.InfiniteStairways.NamelessDeityInfiniteStairwayManager;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public class NamelessDeityInfiniteStairwayTopAnimationManager : ModSystem
    {
        public static int AnimationTimer
        {
            get;
            set;
        }

        public static bool AnimationActive => AnimationTimer >= 1;

        public static int AnimationStartDelay => 120;

        public static int DialogLineFadeInTime => ModReferences.CalamityRemix is null ? 45 : 90;

        public static int DialogLineLingerTime => ModReferences.CalamityRemix is null ? 270 : 180;

        public static int DialogLineFadeOutTime => ModReferences.CalamityRemix is null ? 45 : 90;

        public static int PauseUntilNextDialogLine => ModReferences.CalamityRemix is null ? 1 : 45;

        public static int DialogLineExistTime => DialogLineFadeInTime + DialogLineLingerTime + DialogLineFadeOutTime + PauseUntilNextDialogLine;

        public static int AnimationEndDelay => 300;

        public static int TotalDialogLines => 6;

        public static int AnimationDuration => AnimationStartDelay + DialogLineExistTime * (TotalDialogLines - 1) + AnimationEndDelay;

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawAnimation;
            NoxusPlayer.PostUpdateEvent += CreateWindDuringAnimation;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawAnimation;
        }

        private void DrawAnimation(GameTime obj)
        {
            // Make the animation stop if on the game menu.
            if (Main.gameMenu)
            {
                AnimationTimer = 0;
                Opacity = 0f;
            }

            // Disable the animation if it should not be played.
            if (!StairwayIsVisible)
            {
                AnimationTimer = 0;
                return;
            }

            // Zoom in.
            CameraPanSystem.Zoom = Pow(InverseLerp(30f, AnimationStartDelay - 10f, AnimationTimer), 0.4f) * 0.4f;

            // Make the animation go on.
            if (AnimationTimer >= 1 && Main.instance.IsActive)
                AnimationTimer++;

            // Prepare for drawing.
            Main.spriteBatch.Begin();

            // Calculate general draw variables.
            float fadeOutInterpolant = InverseLerp(AnimationDuration - DialogLineFadeOutTime, AnimationDuration, AnimationTimer);

            // Draw dialog.
            if (AnimationTimer >= AnimationStartDelay && fadeOutInterpolant <= 0f)
                DrawDialog();

            // Make the player close their eyes once dialog has started being drawn.
            if (AnimationTimer >= AnimationStartDelay)
                Main.LocalPlayer.eyeHelper.BlinkBecausePlayerGotHurt();

            // Draw a white overlay over everything.
            if (fadeOutInterpolant > 0f)
            {
                DrawWhite(fadeOutInterpolant);
                CameraPanSystem.Zoom -= Pow(fadeOutInterpolant, 2.2f) * 1.35f;

                // Ensure that the white fades out when this effect terminates.
                TotalScreenOverlaySystem.OverlayInterpolant = fadeOutInterpolant;
            }

            // Play a cool sound before the player is taken out of the vision.
            if (AnimationTimer == AnimationDuration - 120f)
                SoundEngine.PlaySound(NamelessDeityBoss.StarConvergenceFastSound);

            // Make the effect end once everything has gone completely white.
            if (AnimationTimer >= AnimationDuration)
            {
                // Stop the animation and bring the player back from the stairway vision.
                Stop();
                CameraPanSystem.Zoom = 0f;
                AnimationTimer = 0;
            }

            Main.spriteBatch.End();
        }

        private void CreateWindDuringAnimation(NoxusPlayer p)
        {
            if (AnimationTimer <= 60)
                return;

            // Create wind if the infinite stairway animation is ongoing.
            if (Main.myPlayer == p.Player.whoAmI && Main.rand.NextBool(4))
            {
                Vector2 windVelocity = Vector2.UnitX * Main.rand.NextFloat(9f, 14.5f) * -StairsDirection;
                Vector2 potentialSpawnPosition = p.Player.Center + new Vector2(Sign(windVelocity.X) * -Main.rand.NextFloat(1100f, 1225f), Main.rand.NextFloat(-900f, -95f));
                Projectile.NewProjectile(p.Player.GetSource_FromThis(), potentialSpawnPosition, windVelocity, ModContent.ProjectileType<WindStreakVisual>(), 0, 0f, p.Player.whoAmI);
            }
        }

        private static void DrawDialog()
        {
            // Rearrange timers to be relative to the dialog.
            int dialogTimer = (AnimationTimer - AnimationStartDelay) % DialogLineExistTime;
            int dialogLine = Utils.Clamp((AnimationTimer - AnimationStartDelay) / DialogLineExistTime + 1, 1, TotalDialogLines);

            // Calculate dialog draw information.
            float dialogScale = Main.screenWidth / 2560f * 0.96f;
            float dialogOpacity = InverseLerp(0f, DialogLineFadeInTime, dialogTimer) * InverseLerp(0f, -DialogLineFadeOutTime, dialogTimer - DialogLineExistTime);
            string dialog = Language.GetTextValue($"Mods.NoxusBoss.Dialog.NamelessDeityStairwayText{(ModReferences.CalamityRemix is null ? string.Empty : "Fanny")}{dialogLine}");
            DynamicSpriteFont font = FontRegistry.Instance.NamelessDeityText;

            // Draw the dialog.
            int k = 0;
            foreach (string line in Utils.WordwrapString(dialog, font, (int)(dialogScale * 1400f), 20, out _))
            {
                if (string.IsNullOrEmpty(line))
                    break;

                Vector2 dialogSize = font.MeasureString(line);
                Vector2 dialogDrawPosition = Main.ScreenSize.ToVector2() * 0.5f + new Vector2(StairsDirection * 500f, -200f) - dialogSize * dialogScale * new Vector2(0f, 0.5f);
                dialogDrawPosition.Y += k * dialogScale * 42f;
                Vector2 dialogOrigin = dialogSize * 0.5f;

                for (int i = 0; i < 4; i++)
                    Main.spriteBatch.DrawString(font, line, dialogDrawPosition + (TwoPi * i / 4f).ToRotationVector2(), Color.Black * Pow(dialogOpacity, 2.4f), 0f, dialogOrigin, dialogScale, 0, 0f);
                Main.spriteBatch.DrawString(font, line, dialogDrawPosition, DialogColorRegistry.NamelessDeityTextColor * dialogOpacity, 0f, dialogOrigin, dialogScale, 0, 0f);
                k++;
            }
        }

        private static void DrawWhite(float fadeOutInterpolant)
        {
            Vector2 pixelScale = Main.ScreenSize.ToVector2() * 2f / WhitePixel.Size();
            Main.spriteBatch.Draw(WhitePixel, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White * fadeOutInterpolant, 0f, WhitePixel.Size() * 0.5f, pixelScale, 0, 0f);
        }
    }
}
