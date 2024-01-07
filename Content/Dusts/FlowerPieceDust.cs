using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Dusts
{
    public class FlowerPieceDust : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
            dust.alpha = 50;
            dust.customData = 0;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.customData = (int)dust.customData + 2;
            if ((int)dust.customData >= 35)
                dust.color *= 0.95f;
            if ((int)dust.customData >= 90)
                dust.active = false;

            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(255, 255, 255, dust.alpha).MultiplyRGBA(dust.color) * InverseLerp(0f, 15f, (int)dust.customData);
    }
}
