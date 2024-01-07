using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static BlendState subtractiveBlending;

        private static RasterizerState cullClockwiseAndScreen;

        private static RasterizerState cullCounterclockwiseAndScreen;

        private static RasterizerState cullOnlyScreen;

        public static BlendState SubtractiveBlending
        {
            get
            {
                subtractiveBlending ??= new()
                {
                    ColorSourceBlend = Blend.SourceAlpha,
                    ColorDestinationBlend = Blend.One,
                    ColorBlendFunction = BlendFunction.ReverseSubtract,
                    AlphaSourceBlend = Blend.SourceAlpha,
                    AlphaDestinationBlend = Blend.One,
                    AlphaBlendFunction = BlendFunction.ReverseSubtract
                };

                return subtractiveBlending;
            }
        }

        public static RasterizerState CullClockwiseAndScreen
        {
            get
            {
                if (cullClockwiseAndScreen is null)
                {
                    cullClockwiseAndScreen = RasterizerState.CullClockwise;
                    cullClockwiseAndScreen.ScissorTestEnable = true;
                }

                return cullClockwiseAndScreen;
            }
        }

        public static RasterizerState CullCounterclockwiseAndScreen
        {
            get
            {
                if (cullCounterclockwiseAndScreen is null)
                {
                    cullCounterclockwiseAndScreen = RasterizerState.CullCounterClockwise;
                    cullCounterclockwiseAndScreen.ScissorTestEnable = true;
                }

                return cullCounterclockwiseAndScreen;
            }
        }

        public static RasterizerState CullOnlyScreen
        {
            get
            {
                if (cullOnlyScreen is null)
                {
                    cullOnlyScreen = RasterizerState.CullNone;
                    cullOnlyScreen.ScissorTestEnable = true;
                }

                return cullOnlyScreen;
            }
        }

        public static RasterizerState DefaultRasterizerScreenCull => Main.gameMenu || Main.LocalPlayer.gravDir == 1f ? CullCounterclockwiseAndScreen : CullClockwiseAndScreen;

        /// <summary>
        /// Resets a sprite batch with a desired <see cref="BlendState"/>. The <see cref="SpriteSortMode"/> is specified as <see cref="SpriteSortMode.Deferred"/>. If <see cref="SpriteSortMode.Immediate"/> is needed, use <see cref="PrepareForShaders"/> instead.
        /// <br></br>
        /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="newBlendState">The desired blend state.</param>
        public static void UseBlendState(this SpriteBatch spriteBatch, BlendState newBlendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, newBlendState, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Resets the sprite batch with <see cref="SpriteSortMode.Immediate"/> blending, along with an optional <see cref="BlendState"/>. For use when shaders are necessary.
        /// <br></br>
        /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker devices.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="newBlendState">An optional blend state. If none is supplied, <see cref="BlendState.AlphaBlend"/> is used.</param>
        public static void PrepareForShaders(this SpriteBatch spriteBatch, BlendState newBlendState = null, bool ui = false)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, ui ? Main.UIScaleMatrix : Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Resets the sprite batch to its 'default' state relative to most effects in the game, with a default blend state and sort mode. For use after the sprite batch state has been altered and needs to be reset.
        /// <br></br>
        /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="end">Whether to call <see cref="SpriteBatch.End"/> first and flush the contents of the previous draw batch. Defaults to true.</param>
        public static void ResetToDefault(this SpriteBatch spriteBatch, bool end = true)
        {
            if (end)
                spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Resets the sprite batch to its 'default' state relative to the UI, with a default blend state and sort mode. For use after the sprite batch state has been altered and needs to be reset.
        /// <br></br>
        /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="end">Whether to call <see cref="SpriteBatch.End"/> first and flush the contents of the previous draw batch. Defaults to true.</param>
        public static void ResetToDefaultUI(this SpriteBatch spriteBatch, bool end = true)
        {
            if (end)
                spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        /// <summary>
        /// Prepares a specialized <see cref="RasterizerState"/> with enabled screen culling, for efficiency reasons. It also informs the <see cref="GraphicsDevice"/> of this change.
        /// </summary>
        public static RasterizerState PrepareScreenCullRasterizer()
        {
            // Apply the screen culling.
            Main.instance.GraphicsDevice.ScissorRectangle = new(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4);
            return DefaultRasterizerScreenCull;
        }
    }
}
