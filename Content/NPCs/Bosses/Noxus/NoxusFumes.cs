using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus
{
    public class NoxusFumes : ModBuff
    {
        public static Color IllusionColor => new(120, 24, 116, 0);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) => CreateIllusions(player);

        public static void CreateIllusions(Player player)
        {
            // Spawn illusions around the player.
            if (Main.rand.NextBool(150))
            {
                Vector2 illusionSpawnPosition = player.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(132f, 560f);
                NoxusFumesIllusionParticle illusion = new(illusionSpawnPosition, Main.rand.NextVector2Circular(4f, 10f) - Vector2.UnitY * 11f, IllusionColor, 120, Main.rand.NextFloat(1f, 2.4f));
                illusion.Spawn();
            }
        }
    }
}
