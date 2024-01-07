using Microsoft.Xna.Framework;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class RadialScreenShoveScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.01f && GetFromCalamityConfig("Screenshake", true) && RadialScreenShoveSystem.DistortionPower >= 0.01f;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:LightWaveScreenShove", isActive);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public class RadialScreenShoveSystem : ModSystem
    {
        public static Vector2 DistortionCenter
        {
            get;
            set;
        }

        public static float DistortionPower
        {
            get;
            set;
        }

        public static int DistortionTimer
        {
            get;
            set;
        }

        public static int DistortionLifetime
        {
            get;
            set;
        }

        public static float DistortionCompletionRatio => DistortionTimer / (float)DistortionLifetime;

        public override void PostUpdateProjectiles()
        {
            // Increment the distortion timer if it's active. Once its reaches its natural maximum the effect ceases.
            if (DistortionTimer >= 1)
            {
                DistortionTimer++;
                if (DistortionTimer >= DistortionLifetime)
                    DistortionTimer = 0;
            }

            DistortionPower = Convert01To010(DistortionCompletionRatio);
        }

        public static void Start(Vector2 distortionCenter, int distortionTime)
        {
            if (NoxusBossConfig.Instance.PhotosensitivityMode)
                return;

            DistortionCenter = distortionCenter;
            DistortionTimer = 1;
            DistortionLifetime = distortionTime;
        }
    }
}
