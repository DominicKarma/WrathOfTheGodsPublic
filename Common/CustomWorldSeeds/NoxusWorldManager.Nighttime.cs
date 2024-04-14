using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.CustomWorldSeeds
{
    public partial class NoxusWorldManager : ModSystem
    {
        private void LoadNightScreenShader()
        {
            Ref<Effect> s = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/SkyAndZoneEffects/NoxusMoonlightShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[EerieNoxusNightShaderData.ShaderKey] = new Filter(new EerieNoxusNightShaderData(s, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);
        }
    }
}
