using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace NoxusBoss.Core.Configuration
{
    [BackgroundColor(96, 30, 53, 216)]
    public class NoxusBossConfig : ModConfig
    {
        public static NoxusBossConfig Instance => ModContent.GetInstance<NoxusBossConfig>();

        public override ConfigScope Mode => ConfigScope.ClientSide;

        private bool photosensitivityMode;

        private float screenShakeIntensity;

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool DisplayConfigMessage { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(false)]
        public bool PhotosensitivityMode
        {
            get => photosensitivityMode;
            set
            {
                photosensitivityMode = value;

                // Reset other config options too if this property was just enabled.
                if (photosensitivityMode)
                {
                    ScreenShatterEffects = false;
                    VisualOverlayIntensity = 0f;
                    ScreenShakeIntensity = 0f;
                }
            }
        }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(1f)]
        [Range(0f, 2f)]
        public float ScreenShakeIntensity
        {
            get
            {
                // Turn off screen shakes completely if Calamity's config indicates that they should be disabled.
                if (!GetFromCalamityConfig("Screenshake", true))
                    screenShakeIntensity = 0f;

                return screenShakeIntensity;
            }
            set => screenShakeIntensity = value;
        }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool ScreenShatterEffects { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(0.5f)]
        [Range(0f, 1f)]
        public float VisualOverlayIntensity { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => false;
    }
}
