using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.Graphics
{
    public class PrimitiveTrailGroupingSystem : ModSystem
    {
        private static readonly Dictionary<Type, PrimitiveTrailGroup> groups = new();

        public static PrimitiveTrailGroup GetGroup(Type t) => groups[t];

        public static PrimitiveTrailGroup GetGroup<T>() where T : ModType => GetGroup(typeof(T));

        public override void OnModLoad()
        {
            new ManagedILEdit("Draw Non-Pixelated Projectile-Layered Primitives", edit =>
            {
                IL_Main.DrawProjectiles += edit.SubscriptionWrapper;
            }, DrawPrimsWithProjectiles).Apply();

            On_Main.DrawNPCs += DrawPrimsAfterNPCs;
        }

        public override void PostSetupContent()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Load all primitive groups. This is done in PostSetupContent instead of OnModLoad because if the shader system isn't loaded this will fail.
            foreach (Type type in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!type.IsAbstract && type.GetInterfaces().Contains(typeof(IDrawGroupedPrimitives)))
                {
                    IDrawGroupedPrimitives groupHandler = (IDrawGroupedPrimitives)FormatterServices.GetUninitializedObject(type);
                    groups[type] = new(groupHandler.DrawContext, groupHandler.Shader, groupHandler.MaxIndices, groupHandler.MaxVertices, groupHandler.PrepareShaderForPrimitives);
                }
            }
        }

        // This uses IL instead of On so that the the inbuilt spriteBatch.Begin can be used.
        private void DrawPrimsWithProjectiles(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Before projectiles are drawn, after Begin has been called.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStsfld<Main>("CurrentDrawnEntityShader")))
            {
                edit.LogFailure("Could not locate the first Main.CurrentDrawnEntityShader storage.");
                return;
            }

            cursor.EmitDelegate(() =>
            {
                DrawGroupWithDrawContext(PrimitiveGroupDrawContext.BeforeProjectiles, PrimitiveGroupDrawContext.Pixelated);
            });

            // After projectiles are drawn, before End is called.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStsfld<Main>("CurrentDrawnEntityShader")))
            {
                edit.LogFailure("Could not locate the second Main.CurrentDrawnEntityShader storage.");
                return;
            }

            cursor.EmitDelegate(() =>
            {
                DrawGroupWithDrawContext(PrimitiveGroupDrawContext.AfterProjectiles, PrimitiveGroupDrawContext.Pixelated);
            });
        }

        private void DrawPrimsAfterNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            orig(self, behindTiles);

            if (!behindTiles)
                DrawGroupWithDrawContext(PrimitiveGroupDrawContext.AfterNPCs, PrimitiveGroupDrawContext.Pixelated);
        }

        public static bool DrawGroupWithDrawContext(PrimitiveGroupDrawContext drawContext, PrimitiveGroupDrawContext exclude = (PrimitiveGroupDrawContext)int.MaxValue)
        {
            bool anythingWasDrawn = false;
            foreach (var group in groups.Values)
            {
                if (!group.DrawContext.HasFlag(drawContext) || group.DrawContext.HasFlag(exclude))
                    continue;

                anythingWasDrawn = true;
                group.Draw();
            }

            return anythingWasDrawn;
        }

        public override void OnModUnload()
        {
            foreach (var group in groups.Values)
                group.Dispose();
        }
    }
}
