using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        private void RemoveBestiaryStarLimits(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Perform reflection for IL.
            FieldInfo starCountField = typeof(NPCPortraitInfoElement).GetField("_filledStarsCount", BindingFlags.Instance | BindingFlags.NonPublic);

            // The first iteration of this loop sets the star limit, the second iteration sets the wrap number.
            // If the second iteration is removed, the wrap number sticks to 5 and stars will go down a bit every 5 instances.
            for (int j = 0; j < 2; j++)
            {
                if (!cursor.TryGotoNext(i => i.MatchLdcI4(5)))
                {
                    edit.LogFailure($"The {(j == 0 ? "first" : "second")} load of the integer literal of 5 could not be found.");
                    return;
                }

                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStloc(out _)))
                {
                    edit.LogFailure($"The {(j == 0 ? "first" : "second")} storage of the local variable right after the literal of 5 could not be found.");
                    return;
                }

                // Turn the 5 into Max(5, _filledStarsCount.Value);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, starCountField);
                cursor.EmitDelegate<Func<int, int?, int>>((originalLimit, starCount) =>
                {
                    return Math.Max(starCount ?? 1, originalLimit);
                });
            }
        }

        public override void DrawBehind(int index)
        {
            bool canDraw = CurrentState == NamelessAIType.OpenScreenTear || CurrentState == NamelessAIType.Awaken || CurrentState == NamelessAIType.DeathAnimation || NPC.Opacity >= 0.02f;
            if (NPC.hide && canDraw)
            {
                // Special rule: If the scary background needs Nameless to do the jumpscare effect, draw in the background.
                if (NamelessDeityScarySkyManager.IsActive && NamelessDeityScarySkyManager.Variant == NamelessDeityScarySkyManager.SkyVariant.NamelessDeityJumpscare)
                {
                    SpecialLayeringSystem.DrawCacheBeforeBlack.Add(index);
                    return;
                }

                if ((DrawCongratulatoryText || UniversalBlackOverlayInterpolant >= 0.02f) && ZPosition >= -0.5f)
                    SpecialLayeringSystem.DrawCacheAfterNoxusFog.Add(index);
                else if (ZPosition < -0.1f)
                    SpecialLayeringSystem.DrawCacheAfterNoxusFog.Add(index);
                else if (ShouldDrawBehindTiles)
                    SpecialLayeringSystem.DrawCacheBeforeBlack.Add(index);
                else
                    Main.instance.DrawCacheNPCsMoonMoon.Add(index);
            }
        }

        public override void ModifyTypeName(ref string typeName)
        {
            // Don't change the name if not actually present. Nycro's nohit mod gets fucked otherwise.
            if (!NPC.active)
                return;

            if (CurrentState == NamelessAIType.DeathAnimation || NPC.IsABestiaryIconDummy)
            {
                typeName = string.Empty;
                return;
            }

            typeName = string.Empty;
            for (int i = 0; i < 8; i++)
                typeName += (char)Main.rand.Next(700);

            // Add a cheeky suffix if the player has died many, many times.
            if (WorldSaveSystem.NamelessDeityDeathCount >= 2000)
                typeName += Language.GetTextValue($"Mods.{Mod.Name}.NPCs.{Name}.SillyDeathCountSuffix");
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale *= TeleportVisualsAdjustedScale.Length() * 0.707f;
            return null;
        }

        public override void BossHeadSlot(ref int index)
        {
            // Make the head icon disappear if Nameless is invisible.
            if (NPC.Opacity <= 0.45f)
                index = -1;
        }
    }
}
