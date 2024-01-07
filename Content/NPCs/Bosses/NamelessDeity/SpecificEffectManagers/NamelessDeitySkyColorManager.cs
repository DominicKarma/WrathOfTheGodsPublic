using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeitySkyColorManager : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            // Make the background color get darker based on Nameless' star fear effect.
            backgroundColor = Color.Lerp(backgroundColor, new(1, 1, 2), Pow(NamelessDeitySky.StarRecedeInterpolant, 2.5f));
        }
    }
}
