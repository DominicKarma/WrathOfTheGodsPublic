using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class Rok : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostDraedonAndCal;

        public static ModItem RockItem
        {
            get
            {
                if (ModReferences.BaseCalamity?.TryFind("Rock", out ModItem theRock) ?? false)
                    return theRock;

                return null;
            }
        }

        public static int RockID => RockItem?.Type ?? ItemID.None;

        public override void Load()
        {
            // Don't bother attempting to load the IL edit if Calamity is not enabled.
            if (!ModLoader.TryGetMod("CalamityMod", out Mod cal))
                return;

            // Store Calamity if it hasn't been defined yet, just in case.
            ModReferences.BaseCalamity ??= cal;

            Type bossRushProjType = cal.Code.GetType("CalamityMod.Projectiles.Typeless.BossRushEndEffectThing");
            if (bossRushProjType is null)
            {
                Mod.Logger.Warn("Calamity's 'BossRushEndEffectThing' projectile could not be found! The rock could not be replaced!");
                return;
            }

            MethodInfo bossRushEnderKillMethod = bossRushProjType.GetMethod("OnKill", BindingFlags.Public | BindingFlags.Instance);
            if (bossRushEnderKillMethod is not null)
                MonoModHooks.Modify(bossRushEnderKillMethod, ReplaceRockInBossRush);
            else
                Mod.Logger.Warn("Calamity's 'BossRushEndEffectThing' projectile's Kill method could not be found! The rock could not be replaced!");
        }

        private void ReplaceRockInBossRush(ILContext context)
        {
            // Define methods. These will be used for findings and replacements.
            MethodInfo itemTypeMethod = typeof(ModContent).GetMethod("ItemType", BindingFlags.Public | BindingFlags.Static);
            MethodInfo itemTypeMethod_Rock = itemTypeMethod.MakeGenericMethod(RockItem.GetType());

            // Define the method cursor.
            ILCursor cursor = new(context);

            // Search for the location of the ModContent.ItemType<Rock> call and move after it.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt(itemTypeMethod_Rock)))
            {
                Mod.Logger.Error("Could not find the call for the ModContent.ItemType<Rock> method in the Boss Rush item replacement edit!");
                return;
            }

            // Replace the real rock with the fake rock.
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(ModContent.ItemType<Rok>);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = 0;
        }
    }
}
