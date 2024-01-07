using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Swaps the <see cref="GraphicsDevice"/> to a desired render target and clears said render target's contents.
        /// <br></br>
        /// Be careful when using this method. Render targets have been observed to cause significant lag on weaker devices, and as such should be manipulated only as necessary.
        /// </summary>
        /// <param name="renderTarget">The desired render target.</param>
        /// <param name="flushColor">The color to reset the render target's contents to. Defaults to <see cref="Color.Transparent"/>.</param>
        public static void SwapToRenderTarget(this RenderTarget2D renderTarget, Color? flushColor = null)
        {
            // Local variables for convinience.
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            // If on the menu, a server, or any of the render targets are null, terminate this method immediately.
            if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
                return;

            // Otherwise set the render target.
            graphicsDevice.SetRenderTarget(renderTarget);

            // "Flush" the screen, removing any previous things drawn to it.
            graphicsDevice.Clear(flushColor ?? Color.Transparent);
        }
    }
}
