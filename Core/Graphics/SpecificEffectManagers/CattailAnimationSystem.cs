using Microsoft.Xna.Framework;
using NoxusBoss.Assets.Fonts;
using ReLogic.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class CattailAnimationSystem : ModSystem
    {
        public static int AnimationTimer
        {
            get;
            private set;
        }

        public static readonly int AnimationDuration = SecondsToFrames(16f);

        public static readonly int DelayUntilAnimationBegins = SecondsToFrames(12f);

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawAnimationWrapper;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawAnimationWrapper;
        }

        private void DrawAnimationWrapper(GameTime obj)
        {
            // Make the animation stop if on the game menu.
            if (Main.gameMenu)
                AnimationTimer = 0;

            if (AnimationTimer <= 0)
                return;

            Main.spriteBatch.Begin();
            DrawAnimation();
            Main.spriteBatch.End();

            // Make the animation go on. Once it concludes it goes away.
            AnimationTimer++;
            if (AnimationTimer >= DelayUntilAnimationBegins + AnimationDuration)
                AnimationTimer = 0;
        }

        private static void DrawAnimation()
        {
            float animationCompletion = InverseLerp(0f, AnimationDuration, AnimationTimer - DelayUntilAnimationBegins);
            float opacity = InverseLerpBump(0f, 0.1f, 0.9f, 1f, animationCompletion);

            // Don't bother if nothing would draw due to zero opacity.
            if (opacity <= 0f)
                return;

            // Draw the line overlay over the screen.
            Vector2 drawCenter = Main.ScreenSize.ToVector2() * 0.5f - Vector2.UnitY * 80f;
            Vector2 lineScale = new(Main.screenWidth / FadedLine.Width * 1.8f, 1.4f);
            Main.spriteBatch.Draw(FadedLine, drawCenter, null, Color.Black * opacity * 0.75f, 0f, FadedLine.Size() * 0.5f, lineScale, 0, 0f);

            // Draw the special text.
            string text = Language.GetTextValue("Mods.NoxusBoss.Dialog.CattailText");
            DynamicSpriteFont font = FontRegistry.Instance.NamelessDeityText;
            float scale = 1.1f;
            float maxHeight = 300f;
            Vector2 textSize = font.MeasureString(text);
            if (textSize.Y > maxHeight)
                scale = maxHeight / textSize.Y;
            Vector2 textDrawPosition = drawCenter - textSize * scale * 0.5f;
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, textDrawPosition, DialogColorRegistry.CattailAnimationTextColor * Pow(opacity, 1.6f), 0f, Vector2.Zero, new(scale), -1f, 2f);
        }

        public static void StartAnimation()
        {
            AnimationTimer = 1;
        }
    }
}
