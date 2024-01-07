using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Fixes;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class TotalScreenOverlaySystem : ModSystem
    {
        public static Color OverlayColor
        {
            get;
            set;
        }

        public static event Action EffectInactiveEvent;

        public static event Action DrawAfterWhiteEvent;

        public static float OverlayInterpolant
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawWhite;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawWhite;
            EffectInactiveEvent = null;
            DrawAfterWhiteEvent = null;
        }

        public override void PreUpdateEntities()
        {
            OverlayInterpolant = Clamp(OverlayInterpolant - 0.04f, 0f, 2f);

            // Reset the overlay color if the effect is not currently active.
            if (OverlayInterpolant <= 0f)
                OverlayColor = Color.White;
        }

        private void DrawWhite(GameTime obj)
        {
            if (OverlayInterpolant <= 0f || (Main.gameMenu && !EternalGardenIntroBackgroundFix.ShouldDrawWhite))
            {
                EffectInactiveEvent?.Invoke();
                return;
            }

            // Prepare for drawing.
            Main.spriteBatch.Begin();

            // Draw a pure-white background.
            Vector2 pixelScale = Main.ScreenSize.ToVector2() * 2f / WhitePixel.Size();
            Main.spriteBatch.Draw(WhitePixel, Main.ScreenSize.ToVector2() * 0.5f, null, OverlayColor * Clamp(OverlayInterpolant, 0f, 1f), 0f, WhitePixel.Size() * 0.5f, pixelScale, 0, 0f);

            // Handle post-white draw effects.
            DrawAfterWhiteEvent?.Invoke();

            Main.spriteBatch.End();
        }
    }
}
