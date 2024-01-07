using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Summons a projectile of a specific type while also adjusting damage for vanilla spaghetti regarding hostile projectiles.
        /// </summary>
        /// <param name="spawnX">The x spawn position of the projectile.</param>
        /// <param name="spawnY">The y spawn position of the projectile.</param>
        /// <param name="velocityX">The x velocity of the projectile.</param>
        /// <param name="velocityY">The y velocity of the projectile</param>
        /// <param name="type">The id of the projectile type that should be spawned.</param>
        /// <param name="damage">The damage of the projectile.</param>
        /// <param name="knockback">The knockback of the projectile.</param>
        /// <param name="owner">The owner index of the projectile.</param>
        /// <param name="ai0">An optional <see cref="NPC.ai"/>[0] fill value. Defaults to 0.</param>
        /// <param name="ai1">An optional <see cref="NPC.ai"/>[1] fill value. Defaults to 0.</param>
        /// <param name="ai2">An optional <see cref="NPC.ai"/>[2] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(float spawnX, float spawnY, float velocityX, float velocityY, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
        {
            if (owner == -1)
                owner = Main.myPlayer;
            damage = (int)(damage * 0.5);
            if (Main.expertMode)
                damage = (int)(damage * 0.5);
            int index = Projectile.NewProjectile(new EntitySource_WorldEvent(), spawnX, spawnY, velocityX, velocityY, type, damage, knockback, owner, ai0, ai1, ai2);
            if (index >= 0 && index < Main.maxProjectiles)
                Main.projectile[index].netUpdate = true;

            return index;
        }

        /// <summary>
        /// Summons a projectile of a specific type while also adjusting damage for vanilla spaghetti regarding hostile projectiles.
        /// </summary>
        /// <param name="center">The spawn position of the projectile.</param>
        /// <param name="velocity">The velocity of the projectile</param>
        /// <param name="type">The id of the projectile type that should be spawned.</param>
        /// <param name="damage">The damage of the projectile.</param>
        /// <param name="knockback">The knockback of the projectile.</param>
        /// <param name="owner">The owner index of the projectile.</param>
        /// <param name="ai0">An optional <see cref="NPC.ai"/>[0] fill value. Defaults to 0.</param>
        /// <param name="ai1">An optional <see cref="NPC.ai"/>[1] fill value. Defaults to 0.</param>
        /// <param name="ai2">An optional <see cref="NPC.ai"/>[2] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
        {
            return NewProjectileBetter(center.X, center.Y, velocity.X, velocity.Y, type, damage, knockback, owner, ai0, ai1, ai2);
        }

        /// <summary>
        /// Returns all projectiles present of a specific type.
        /// </summary>
        /// <param name="desiredTypes">The projectile type to check for.</param>
        public static IEnumerable<Projectile> AllProjectilesByID(params int[] desiredTypes)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && desiredTypes.Contains(Main.projectile[i].type))
                    yield return Main.projectile[i];
            }
        }

        /// <summary>
        /// Determines if a <see cref="Projectile"/> is on its final extra update. Useful for things like timer increments, which may only want to be performed once every frame.
        /// </summary>
        /// <param name="p">The projectile to check.</param>
        public static bool IsFinalExtraUpdate(this Projectile p) => p.numUpdates == -1;

        /// <summary>
        /// Checks if a given projectile ID is present anywhere.
        /// </summary>
        /// <param name="projectileID">The projectile ID to check for.</param>
        public static bool AnyProjectiles(int projectileID)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == projectileID)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Counts the amount of projectiles of any given IDs.
        /// </summary>
        /// <param name="desiredTypes">The projectile IDs to check for.</param>
        public static int CountProjectiles(params int[] desiredTypes)
        {
            int projectileCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && desiredTypes.Contains(Main.projectile[i].type))
                    projectileCount++;
            }

            return projectileCount;
        }

        public static bool IsOffscreen(this Projectile p)
        {
            // Check whether the projectile's hitbox intersects the screen, accounting for the screen fluff setting.
            int fluff = ProjectileID.Sets.DrawScreenCheckFluff[p.type];
            Rectangle screenArea = new((int)Main.Camera.ScaledPosition.X - fluff, (int)Main.Camera.ScaledPosition.Y - fluff, (int)Main.Camera.ScaledSize.X + fluff * 2, (int)Main.Camera.ScaledSize.Y + fluff * 2);
            return !screenArea.Intersects(p.Hitbox);
        }

        /// <summary>
        /// A simple utility that gets an <see cref="Projectile"/>'s <see cref="Projectile.ModProjectile"/> instance as a specific type without having to do clunky casting.
        /// </summary>
        /// <typeparam name="T">The ModProjectile type to convert to.</typeparam>
        /// <param name="p">The Projectile to access the ModProjectile from.</param>
        public static T As<T>(this Projectile p) where T : ModProjectile
        {
            return p.ModProjectile as T;
        }
    }
}
