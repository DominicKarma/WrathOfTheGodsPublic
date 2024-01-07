using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    /* CONTEXT: 
     * This system is a duplicate of the variant I made for Infernum a while back. It is safely programmed and will not break if players use it alongside Infernum.
     * The reason it's necessary is because of multiplayer.
     * By the time you get back the (index of the) projectile that was spawned it's too late to set any extra data, because the projectile sync packet
     * was already sent. This system addresses this problem by inserting a delegate inside of Projectile.NewProjectile before that sync happens, thus allowing
     * extra data to be included without issue.
     */
    public class ProjectileSpawnManagementSystem : ModSystem
    {
        private static Action<Projectile> preSyncAction;

        public static void PrepareProjectileForSpawning(Action<Projectile> a) => preSyncAction = a;

        public override void OnModLoad()
        {
            new ManagedILEdit("Inherent Custom Projectile Data Spawn Syncing", edit =>
            {
                IL_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += edit.SubscriptionWrapper;
            }, PreSyncProjectileStuff).Apply();
        }

        private void PreSyncProjectileStuff(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Go after the projectile instantiation phase and find the local index of the spawned projectile.
            int projectileILIndex = 0;
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStfld<Projectile>("stepSpeed")))
            {
                edit.LogFailure("The projectile.stepSpeed storage could not be found.");
                return;
            }

            int placeToSetAction = cursor.Index;
            if (!cursor.TryGotoPrev(i => i.MatchLdloc(out projectileILIndex)))
            {
                edit.LogFailure("The projectile's local IL index could not be found.");
                return;
            }

            cursor.Goto(placeToSetAction);
            cursor.Emit(OpCodes.Ldloc, projectileILIndex);
            cursor.EmitDelegate<Action<Projectile>>(projectile =>
            {
                // Invoke the pre-sync action and then destroy it, to ensure that the action doesn't bleed into successive, unrelated projectile spawn calls.
                preSyncAction?.Invoke(projectile);
                preSyncAction = null;
            });
        }
    }
}
