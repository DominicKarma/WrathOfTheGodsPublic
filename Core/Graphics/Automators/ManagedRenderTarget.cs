using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Core.Graphics.Automators
{
    [DebuggerDisplay("Width: {target?.Width ?? 0}, Height: {target?.Height ?? 0}, Uninitialized: {IsUninitialized}, Time since last usage: {TimeSinceLastUsage} frame(s)")]
    public class ManagedRenderTarget : IDisposable
    {
        private RenderTarget2D target;

        public int TimeSinceLastUsage
        {
            get;
            internal set;
        }

        internal bool WaitingForFirstInitialization
        {
            get;
            private set;
        } = true;

        internal RenderTargetInitializationAction InitializationAction
        {
            get;
            private set;
        }

        public bool IsUninitialized => target is null || target.IsDisposed;

        public bool IsDisposed
        {
            get;
            private set;
        }

        public bool ShouldResetUponScreenResize
        {
            get;
            private set;
        }

        public bool SubjectToGarbageCollection
        {
            get;
            private set;
        }

        public RenderTarget2D Target
        {
            get
            {
                TimeSinceLastUsage = 0;
                if (IsUninitialized)
                {
                    target = InitializationAction(Main.screenWidth, Main.screenHeight);
                    WaitingForFirstInitialization = false;
                }

                return target;
            }
            private set => target = value;
        }

        public int Width => Target.Width;

        public int Height => Target.Height;

        public delegate RenderTarget2D RenderTargetInitializationAction(int screenWidth, int screenHeight);

        public ManagedRenderTarget(bool shouldResetUponScreenResize, RenderTargetInitializationAction creationCondition, bool subjectToGarbageCollection = true)
        {
            ShouldResetUponScreenResize = shouldResetUponScreenResize;
            InitializationAction = creationCondition;
            SubjectToGarbageCollection = subjectToGarbageCollection;
            RenderTargetManager.ManagedTargets.Add(this);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            target?.Dispose();
            TimeSinceLastUsage = 0;
            GC.SuppressFinalize(this);
        }

        public void Recreate(int screenWidth, int screenHeight)
        {
            Dispose();
            IsDisposed = false;
            TimeSinceLastUsage = 0;

            target = InitializationAction(screenWidth, screenHeight);
        }

        // These extension methods don't apply to ManagedRenderTarget instances, even with the implicit conversion operator. As such, it is implemented manually.
        public Vector2 Size() => Target.Size();

        public void SwapToRenderTarget(Color? flushColor = null) => Target.SwapToRenderTarget(flushColor);

        public void CopyContentsFrom(RenderTarget2D from)
        {
            Main.instance.GraphicsDevice.SetRenderTarget(Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            Main.spriteBatch.Draw(from, Vector2.Zero, null, Color.White);
            Main.spriteBatch.End();

            Main.instance.GraphicsDevice.SetRenderTarget(from);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.instance.GraphicsDevice.SetRenderTarget(null);
        }

        // This allows for easy shorthand conversions from ManagedRenderTarget to RenderTarget2D without having to manually type out ManagedTarget.Target all the time.
        // This is functionally equivalent to accessing the getter manually and will activate all of the relevant checks within said getter.
        public static implicit operator RenderTarget2D(ManagedRenderTarget targetWrapper) => targetWrapper.Target;
    }
}
