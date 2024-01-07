using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class LocalPlayerDrawManager : ModSystem
    {
        public static Vector2 CacheDrawOffset
        {
            get;
            private set;
        }

        public static bool UseTargetDrawer
        {
            get;
            private set;
        }

        public static ManagedRenderTarget PlayerTarget
        {
            get;
            private set;
        }

        public static Action ShaderDrawAction
        {
            get;
            set;
        }

        public static Func<bool> StopCondition
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_LegacyPlayerRenderer.DrawPlayerFull += DrawWithTargetIfNecessary;
            On_PlayerDrawLayers.DrawPlayer_TransformDrawData += DrawCachesWithTargetOffset;
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareDrawTarget;
            Main.QueueMainThreadAction(() => PlayerTarget = new(false, (width, height) => new(Main.instance.GraphicsDevice, 512, 512)));
        }

        private void PrepareDrawTarget()
        {
            if (Main.gameMenu)
                UseTargetDrawer = false;

            if (ShaderDrawAction is not null || UseTargetDrawer)
            {
                // Ensure that the DrawPlayerFull method doesn't attempt to access the render target when trying to draw to the render target.
                UseTargetDrawer = false;

                // Prepare the render target.
                var gd = Main.instance.GraphicsDevice;
                gd.SetRenderTarget(PlayerTarget);
                gd.Clear(Color.Transparent);

                // Draw the player.
                Vector2 oldPosition = Main.LocalPlayer.Center;
                Main.LocalPlayer.Center = Main.screenPosition + PlayerTarget.Size() * 0.5f;
                CacheDrawOffset = Main.LocalPlayer.Center - oldPosition;
                Main.PlayerRenderer.DrawPlayers(Main.Camera, new Player[] { Main.LocalPlayer });

                // Reset the player's position.
                Main.LocalPlayer.Center = oldPosition;

                // Return to the backbuffer.
                gd.SetRenderTarget(null);

                UseTargetDrawer = true;
            }
        }

        private static void PrepareSpritebatchForPlayers(Camera camera, Player drawPlayer)
        {
            SamplerState samplerState = camera.Sampler;
            if (drawPlayer.mount.Active && drawPlayer.fullRotation != 0f)
                samplerState = LegacyPlayerRenderer.MountedSamplerState;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, samplerState, DepthStencilState.None, camera.Rasterizer, null, Matrix.Identity);
        }

        private void DrawWithTargetIfNecessary(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer)
        {
            // Use the player render target instead of manual drawing if a draw action is necessary.
            bool stopEffect = StopCondition?.Invoke() ?? true;
            if (stopEffect && drawPlayer.whoAmI == Main.myPlayer)
            {
                UseTargetDrawer = false;
                ShaderDrawAction = null;
            }

            if (UseTargetDrawer && ShaderDrawAction is not null && drawPlayer.whoAmI == Main.myPlayer && NamelessDeityBoss.Myself is not null && !stopEffect)
            {
                PrepareSpritebatchForPlayers(camera, drawPlayer);

                // Prepare the shader draw action and reset it.
                ShaderDrawAction.Invoke();
                ShaderDrawAction = null;

                Main.spriteBatch.Draw(PlayerTarget, drawPlayer.Center - Main.screenPosition, null, Color.White, 0f, PlayerTarget.Size() * 0.5f, 1f, 0, 0f);
                Main.spriteBatch.End();
                return;
            }

            orig(self, camera, drawPlayer);
        }

        private static void DrawCachesWithTargetOffset(On_PlayerDrawLayers.orig_DrawPlayer_TransformDrawData orig, ref PlayerDrawSet drawinfo)
        {
            orig(ref drawinfo);
            if (Main.gameMenu || drawinfo.drawPlayer.whoAmI != Main.myPlayer || !UseTargetDrawer)
                return;

            for (int i = 0; i < drawinfo.DustCache.Count; i++)
                Main.dust[drawinfo.DustCache[i]].position -= CacheDrawOffset;

            for (int i = 0; i < drawinfo.GoreCache.Count; i++)
                Main.gore[drawinfo.GoreCache[i]].position -= CacheDrawOffset;
        }
    }
}
