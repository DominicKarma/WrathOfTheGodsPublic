using System;
using System.Collections.Generic;
using MonoMod.Cil;
using NoxusBoss.Content.Particles.Metaballs;
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

                var pitchBlackMetaball = ModContent.GetInstance<PitchBlackMetaball>();
                if (pitchBlackMetaball.ShouldRender)
                {
                    Main.spriteBatch.PrepareForShaders();
                    pitchBlackMetaball.RenderLayerWithShader();
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
