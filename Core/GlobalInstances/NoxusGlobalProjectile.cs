using System;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalProjectile : GlobalProjectile
    {
        public delegate bool ProjectileConditionDelegate(Projectile projectile);

        public static event ProjectileConditionDelegate PreAIEvent;

        public override void Unload()
        {
            // Reset all events on mod unload.
            PreAIEvent = null;
        }

        public override bool PreAI(Projectile projectile)
        {
            // Use default behavior if the event has no subscribers.
            if (PreAIEvent is null)
                return true;

            bool result = true;
            foreach (Delegate d in PreAIEvent.GetInvocationList())
                result &= ((ProjectileConditionDelegate)d).Invoke(projectile);

            return result;
        }
    }
}
