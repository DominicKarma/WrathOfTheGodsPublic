using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    /* CONTEXT :
     * The Nameless Deity stairway animation is supposed to be completely individualized. In Multiplayer, only the person experiencing the effect should be able to witness it.
     * Everyone else should simply see the player in place with their eyes closed.
     */
    public class FixUnconsciousPlayersBeingVisibleInMP : ModSystem
    {
        public const string IsExperiencingStairwayVisionField = "ExperiencingStairwayVision";

        public const string PositionBeforeVisionField = "PositionBeforeVision";

        public override void PostUpdateEverything()
        {
            // Make players that are experiencing the stairway vision appear to be stationary and unconscious to everyone else.
            // Importantly, this code does not execute for the player experiencing the vision themselves, since that would teleport them around unexpectedly.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || i == Main.myPlayer || !p.GetValueRef<bool>(IsExperiencingStairwayVisionField))
                    continue;

                // Force the player in place.
                p.TopLeft = p.GetValueRef<Vector2>(PositionBeforeVisionField);
                while (!Collision.SolidCollision(p.BottomLeft - Vector2.UnitY * 2f, p.width, 4))
                    p.position.Y += 2f;

                // Disable the player's motion.
                p.velocity = Vector2.Zero;

                // Make the player's eyes close.
                p.eyeHelper.BlinkBecausePlayerGotHurt();
            }
        }
    }
}
