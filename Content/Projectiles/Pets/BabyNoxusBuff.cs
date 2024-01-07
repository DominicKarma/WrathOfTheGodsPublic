using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets
{
    public class BabyNoxusBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            bool _ = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<BabyNoxus>());
        }
    }
}
