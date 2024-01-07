using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace NoxusBoss.Core.Fixes
{
    /* CONTEXT:
     * For reasons that remain elusive, the Eternal Garden intro drawcode that happens when going to the subworld is inconsistent.
     * Sometimes, a one-frame disparity can occur where said drawcode doesn't execute at all, revealing the main menu behind everything in a short, awkward
     * blink. This system addresses this by manually drawing white over the main menu at all times if a special bool (which is triggered before the subworld enter code) is
     * enabled.
     * 
     * Furthermore, the menu drawing code appears to mess with the mouse depending on what the player was last doing.
     */
    public class EternalGardenIntroBackgroundFix : ModSystem
    {
        public static bool ShouldDrawWhite
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_Main.DrawMenu += DrawWhite;
            On_UILinkPointNavigator.Update += StopMouseStuff;

            // Ensure that the MagicPixel texture is loaded when the mod is.
            if (Main.netMode != NetmodeID.Server)
                _ = TextureAssets.MagicPixel.Value;
        }

        // The reason this is necessary is because sometimes the custom drawing for the subworld entering has a one frame hiccup, and awkwardly draws the
        // regular background for some reason.
        private void DrawWhite(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            // Do nothing more if white should not be drawn.
            if (!ShouldDrawWhite)
                return;

            Main.spriteBatch.Begin();

            // Draw the white over everything.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.DisplayMode.Width, Main.instance.GraphicsDevice.DisplayMode.Width);
            Vector2 scale = screenArea / pixel.Size();
            Main.spriteBatch.Draw(pixel, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, 0, 0f);

            // Ensure that the player's cursor draws over the white backgorund.
            Main.DrawCursor(Main.DrawThickCursor());

            Main.spriteBatch.End();
        }

        private void StopMouseStuff(On_UILinkPointNavigator.orig_Update orig)
        {
            if (!ShouldDrawWhite)
                orig();
        }
    }
}
