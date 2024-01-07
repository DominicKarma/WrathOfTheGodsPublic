using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Automators
{
    public class RenderTargetManager : ModSystem
    {
        internal static List<ManagedRenderTarget> ManagedTargets = new();

        public delegate void RenderTargetUpdateDelegate();

        public static event RenderTargetUpdateDelegate RenderTargetUpdateLoopEvent;

        public static readonly int TimeUntilUntilUnusedTargetsAreDisposed = SecondsToFrames(5f);

        internal static void ResetTargetSizes(On_Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            foreach (ManagedRenderTarget target in ManagedTargets)
            {
                // Don't attempt to recreate targets that are already initialized or shouldn't be recreated.
                bool incorrectSize = (width != target.Width || height != target.Height) && target.ShouldResetUponScreenResize;
                if (target is null || (!target.IsDisposed && !incorrectSize) || target.WaitingForFirstInitialization)
                    continue;

                Main.QueueMainThreadAction(() =>
                {
                    target.Recreate(width, height);
                });
            }

            orig(width, height, fullscreen);
        }

        internal static void DisposeOfTargets()
        {
            if (ManagedTargets is null)
                return;

            Main.QueueMainThreadAction(() =>
            {
                foreach (ManagedRenderTarget target in ManagedTargets)
                    target?.Dispose();
                ManagedTargets.Clear();
            });
        }

        public static RenderTarget2D CreateScreenSizedTarget(int screenWidth, int screenHeight) =>
            new(Main.instance.GraphicsDevice, screenWidth, screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24, 1, RenderTargetUsage.PreserveContents);

        public override void OnModLoad()
        {
            // Prepare update functionalities.
            Main.OnPreDraw += HandleTargetUpdateLoop;
            On_Main.SetDisplayMode += ResetTargetSizes;
        }

        public override void OnModUnload()
        {
            // Clear any lingering GPU resources.
            DisposeOfTargets();

            // Unsubscribe from the OnPreDraw event.
            Main.OnPreDraw -= HandleTargetUpdateLoop;

            // Reset the update loop event.
            RenderTargetUpdateLoopEvent = null;
        }

        private void HandleTargetUpdateLoop(GameTime obj)
        {
            RenderTargetUpdateLoopEvent?.Invoke();

            // Increment the render target lifetime timers. Once this reaches a certain threshold, the render target is automatically disposed.
            // This timer is reset back to 0 if it's accessed anywhere. The intent of this is to ensure that render targets that are not relevant at a given point in time
            // don't sit around in VRAM forever.
            // The managed wrapper that is the ManagedRenderTarget instance will persist in the central list of this class, but the amount of memory that holds is
            // negligible compared to the unmanaged texture data that the RenderTarget2D itself stores when not disposed.
            foreach (ManagedRenderTarget target in ManagedTargets)
            {
                // Determine whether the target is eligible to be automatically disposed.
                if (!target.SubjectToGarbageCollection || target.IsUninitialized)
                    continue;

                target.TimeSinceLastUsage++;
                if (target.TimeSinceLastUsage >= TimeUntilUntilUnusedTargetsAreDisposed)
                    target.Dispose();
            }
        }
    }
}
