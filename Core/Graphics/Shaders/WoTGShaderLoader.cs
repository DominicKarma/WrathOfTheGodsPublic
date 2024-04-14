using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.InfiniteStairways;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class WoTGShaderLoader : ModSystem
    {
        public override void OnModLoad()
        {
            // Don't attempt to load shaders on servers.
            if (Main.netMode == NetmodeID.Server)
                return;

            // This is kind of hideous but I'm not sure how to best handle these screen shaders. Perhaps some marker in the file name or a dedicated folder?
            Ref<Effect> s = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/LocalizedDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:NoxusEggSky"] = new Filter(new NoxusEggScreenShaderData(s, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Filters.Scene["NoxusBoss:NoxusSky"] = new Filter(new GenericScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["NoxusBoss:NoxusSky"] = new NoxusSky();

            Ref<Effect> s2 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/NamelessDeityScreenTearShader", AssetRequestMode.ImmediateLoad).Value);
            SkyManager.Instance["NoxusBoss:NamelessDeitySky"] = new NamelessDeitySky();
            Filters.Scene["NoxusBoss:NamelessDeitySky"] = new Filter(new NamelessDeityScreenShaderData(s2, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s3 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/RadialScreenShoveShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:LightWaveScreenShove"] = new Filter(new RadialScreenShoveShaderData(s3, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s4 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/ScreenSplitShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:LocalScreenSplit"] = new Filter(new LocalScreenSplitShaderData(s4, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s5 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/OverlayModifiers/NamelessDeity/NamelessDeityClockDeathZoneShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:NamelessDeityClockDeathZoneSky"] = new Filter(new NamelessDeityClockDeathZoneScreenShaderData(s5, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s7 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/OverlayModifiers/NamelessDeity/TerminusScreenOverlayShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:Terminus"] = new Filter(new TerminusScreenShaderData(s7, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Filters.Scene["NoxusBoss:TerminusVortex"] = new Filter(new ScreenShaderData("FilterCrystalDestructionVortex").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);

            Ref<Effect> s8 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/OverlayModifiers/MainMenuScreenShakeShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[MainMenuScreenShakeShaderData.ShaderKey] = new Filter(new MainMenuScreenShakeShaderData(s8, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s9 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/OverlayModifiers/HighContrastScreenShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[HighContrastScreenShakeShaderData.ShaderKey] = new Filter(new HighContrastScreenShakeShaderData(s9, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s10 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/GravitationalLensingShader", AssetRequestMode.ImmediateLoad).Value);
            Ref<Effect> s11 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/GravitationalLensingShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[GravitationalLensingShaderData.ShaderKeyPet] = new Filter(new GravitationalLensingShaderData(true, s11, ManagedShader.DefaultPassName), EffectPriority.High);
            Filters.Scene[GravitationalLensingShaderData.ShaderKey] = new Filter(new GravitationalLensingShaderData(false, s10, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);
            GravitationalLensingShaderData.Load();

            Ref<Effect> s12 = new(Mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/ScreenDistortions/LightSlashesOverlayShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[LightSlashesOverlayShaderData.ShaderKey] = new Filter(new LightSlashesOverlayShaderData(s12, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Filters.Scene["NoxusBoss:InfiniteStairway"] = new Filter(new GenericScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["NoxusBoss:InfiniteStairway"] = new NamelessDeityInfiniteStairwaySky();
        }

        public override void PostUpdateEverything()
        {
            HighContrastScreenShakeShaderData.ToggleActivityIfNecessary();
            GravitationalLensingShaderData.ToggleActivityIfNecessary();
            LightSlashesOverlayShaderData.ToggleActivityIfNecessary();
            EerieNoxusNightShaderData.ToggleActivityIfNecessary();
        }
    }
}
