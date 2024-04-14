using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeityTargetManager : ModSystem
    {
        public static bool DrawBestiaryDummy
        {
            get;
            set;
        }

        public static NPC BestiaryDummy
        {
            get;
            set;
        }

        public static ManagedRenderTarget NamelessDeityTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareTarget;
            Main.QueueMainThreadAction(() =>
            {
                NamelessDeityTarget = new(false, (_, _2) => new(Main.instance.GraphicsDevice, 2500, 2800));
            });
        }

        private void PrepareTarget()
        {
            // Don't waste resources if Nameless is not present.
            if (NamelessDeityBoss.Myself is null && !DrawBestiaryDummy)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Prepare the render target for drawing.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
            gd.SetRenderTarget(NamelessDeityTarget);
            gd.Clear(Color.Transparent);

            // Store the screen width and height. In order for the robes to draw properly they have to be changed temporarily to adjust the matrices.
            int oldScreenWidth = Main.screenWidth;
            int oldScreenHeight = Main.screenHeight;

            // Set the screen width and height manually.
            Main.screenWidth = NamelessDeityTarget.Width;
            Main.screenHeight = NamelessDeityTarget.Height;

            // Draw the nameless deity.
            NPC nameless = NamelessDeityBoss.Myself;
            if (BestiaryDummy is not null && DrawBestiaryDummy)
                nameless = BestiaryDummy;
            nameless.As<NamelessDeityBoss>().DrawSelf(Vector2.Zero, NamelessDeityTarget.Size() * 0.5f);

            // Return screen values to normal again.
            Main.screenWidth = oldScreenWidth;
            Main.screenHeight = oldScreenHeight;

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);

            // Reset the bestiary dummy for next frame now that it has been performed.
            DrawBestiaryDummy = false;
        }
    }
}
