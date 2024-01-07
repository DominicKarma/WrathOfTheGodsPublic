using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Gores
{
    public class DismalSeekerTatter : ModGore
    {
        public override void SetStaticDefaults()
        {
            // Ensure that the tatters appear regardless of the Blood and Gore setting.
            ChildSafety.SafeGore[Type] = true;

            // Falls to the ground like a piece of paper.
            GoreID.Sets.SpecialAI[Type] = 1;
        }

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            // Use a random frame.
            gore.numFrames = 5;
            gore.frame = (byte)(Main.rand?.Next(gore.numFrames) ?? 0);
            gore.velocity.X = Main.rand.NextFloatDirection() * Abs(gore.velocity.X);
            gore.velocity.Y *= 0.2f;
            gore.drawOffset = Vector2.UnitY * 6f;
        }
    }
}
