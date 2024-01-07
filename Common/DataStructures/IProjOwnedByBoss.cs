using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Common.DataStructures
{
    public interface IProjOwnedByBoss<T> where T : ModNPC
    {
        public bool SetActiveFalseInsteadOfKill => false;

        public static void KillAll()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is IProjOwnedByBoss<T> ownedBy)
                {
                    if (!ownedBy.SetActiveFalseInsteadOfKill)
                        p.Kill();
                    else
                        p.active = false;
                }
            }
        }
    }
}
