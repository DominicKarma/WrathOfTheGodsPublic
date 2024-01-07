using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.Metaballs;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class SpecialLayeringSystem : ModSystem
    {
        public static List<int> DrawCacheBeforeBlack
        {
            get;
            private set;
        } = new(Main.maxNPCs);

        public static List<int> DrawCacheAfterNoxusFog
        {
            get;
            private set;
        } = new(Main.maxNPCs);

        public static List<int> DrawCacheBeforeBlack_Proj
        {
            get;
            private set;
        } = new(Main.maxProjectiles);

        internal static void DrawOverBlackNPCCache(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<ScreenDarkness>("DrawBack")))
                return;

            cursor.EmitDelegate(() =>
            {
                if (Main.gameMenu)
                    return;

                // Draw BeforeBlack metaballs.
                if (MetaballManager.AnyActiveMetaballsAtLayer(MetaballDrawLayer.BeforeBlack))
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    MetaballManager.DrawMetaballs(MetaballDrawLayer.BeforeBlack);
                    Main.spriteBatch.ResetToDefault();
                }

                EmptyDrawCache_Projectile(DrawCacheBeforeBlack_Proj);
                EmptyDrawCache_NPC(DrawCacheBeforeBlack);
            });
        }

        public static void EmptyDrawCache_Projectile(List<int> cache)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                try
                {
                    Main.instance.DrawProj(cache[i]);
                }
                catch (Exception e)
                {
                    TimeLogger.DrawException(e);
                    Main.npc[cache[i]].active = false;
                }
            }
            cache.Clear();
        }

        public static void EmptyDrawCache_NPC(List<int> cache)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                try
                {
                    Main.instance.DrawNPC(cache[i], false);
                }
                catch (Exception e)
                {
                    TimeLogger.DrawException(e);
                    Main.npc[cache[i]].active = false;
                }
            }
            cache.Clear();
        }

        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                IL_Main.DoDraw += DrawOverBlackNPCCache;
            });
        }

        public override void OnModUnload()
        {
            Main.QueueMainThreadAction(() =>
            {
                IL_Main.DoDraw -= DrawOverBlackNPCCache;
            });
        }
    }
}
