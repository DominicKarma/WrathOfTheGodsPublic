using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.NamelessDeityBoss;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets
{
    public class NamelessDeityFormPresetRegistry : ModSystem
    {
        private static readonly List<NamelessDeityFormPreset> formPresets = new();

        // This preset makes Nameless horizontally stretched at all times.
        public static bool UsingAmmyanPreset => Main.LocalPlayer.name.Equals("Ammyan", StringComparison.OrdinalIgnoreCase);

        // This preset makes Nameless wear a cute hat at all times.
        public static bool UsingBlastPreset => Main.LocalPlayer.name.Equals("Blast", StringComparison.OrdinalIgnoreCase);

        // This present uses a couple of special preferences and makes Nameless go on a diatribe from Jerma.
        public static bool UsingLynelPreset => Main.LocalPlayer.name.Equals("Lynel", StringComparison.OrdinalIgnoreCase);

        // This preset makes Nameless spin at a comical rate at all times.
        public static bool UsingSmhPreset => Main.LocalPlayer.name.Equals("smh", StringComparison.OrdinalIgnoreCase);

        // This preset makes all of Nameless' forms cycle once per frame, as long as photosensitivity mode is disabled.
        public static bool UsingYuHPreset => Main.LocalPlayer.name.Equals("GinYuH", StringComparison.OrdinalIgnoreCase);

        // Load form presets as special effects for those involved in the development of the mod who wanted one.
        // A couple other examples exist sparsely across Nameless' code, such as him spinning if the player's name is "smh".
        public static readonly NamelessDeityFormPreset AstersFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Aster", StringComparison.OrdinalIgnoreCase)).
            WithAntlerPreference(4). // Sakura branch.
            WithFinPreference(4). // Intricate red/blue fan.
            WithFlowerPreference(4). // Pink Rose.
            WithHandPreference(4). // Thumb/pointer combined meditative pose.
            WithVinePreference(0). // Lavender vine.
            WithWheelPreference(2). // Exo wheel.
            WithWingPreference(2); // Blue bird wings.

        public static readonly NamelessDeityFormPreset DominicsFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Dominic", StringComparison.OrdinalIgnoreCase)).
            WithDisabledCensor(). // Disabled censor.
            WithAntlerPreference(4). // Sakura branch.
            WithFinPreference(4). // Intricate red/blue fan.
            WithFlowerPreference(4). // Pink Rose.
            WithHandPreference(4). // Thumb/pointer combined meditative pose.
            WithArmPreference(1). // Orange-white arm.
            WithForearmPreference(4). // Pale white arm.
            WithVinePreference(4). // Pale vine.
            WithWheelPreference(1). // The dharmachakra.
            WithWingPreference(1); // Angel wings.

        public static readonly NamelessDeityFormPreset LGLsFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("LGL", StringComparison.OrdinalIgnoreCase)).
            WithCensorReplacement(new(() => FishGivingMiddleFingerTexture)); // Replace the censor with a fish giving a middle finger.

        public static readonly NamelessDeityFormPreset LynelsFavorite = RegisterFormPreset(() => UsingLynelPreset).
            WithAntlerPreference(1). // Antler 2.
            WithFinPreference(3). // Intricate fan.
            WithFlowerPreference(1). // Marigold.
            WithHandPreference(1). // Pointer finger.
            WithArmPreference(3). // Red velvet arm.
            WithForearmPreference(1). // Muscle-y forearm.
            WithVinePreference(4). // Pale vine.
            WithWheelPreference(1). // The dharmachakra.
            WithWingPreference(3); // Raven wings.

        public static readonly NamelessDeityFormPreset MoonburnsFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Moonburn", StringComparison.OrdinalIgnoreCase)).
            WithCustomShader(ApplyMoonburnBlueEffect); // Special effect where red colors become blue.

        public static readonly NamelessDeityFormPreset MyrasFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Myra", StringComparison.OrdinalIgnoreCase)).
            WithCustomShader(ApplyMyraGoldEffect). // Special effect where Nameless is tinted golden.
            WithAntlerPreference(0). // Antler 1.
            WithFinPreference(0). // Orange fish fins.
            WithFlowerPreference(2). // Daisy.
            WithHandPreference(4). // Thumb/pointer combined meditative pose.
            WithArmPreference(4). // Dark cream arm.
            WithForearmPreference(0). // Robed forearm.
            WithVinePreference(4). // Pale vine.
            WithWheelPreference(5). // Sun fan thing.
            WithWingPreference(1); // Angel wings.

        public static readonly NamelessDeityFormPreset SnakesFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Snake", StringComparison.OrdinalIgnoreCase)).
            WithAntlerPreference(6). // Tree.
            WithFinPreference(1). // Intricate Green fan.
            WithFlowerPreference(5). // Clock.
            WithHandPreference(1). // Pointer finger.
            WithArmPreference(4). // Dark cream arm.
            WithForearmPreference(0). // Robed forearm.
            WithVinePreference(1). // Dense vines.
            WithWheelPreference(5). // Sun fan thing.
            WithWingPreference(0). // Owl wings.
            WithCensorReplacement(new(() => XRayCensorSketchTexture)); // Replace the censor with an X-ray kind of aesthetic.

        public static readonly NamelessDeityFormPreset ToastysFavorite = RegisterFormPreset(() => Main.LocalPlayer.name.Equals("Toasty", StringComparison.OrdinalIgnoreCase)).
            WithAntlerPreference(5). // Antler 5.
            WithFinPreference(3). // Gold fan fins.
            WithFlowerPreference(2). // Daisy.
            WithHandPreference(4). // Thumb/pointer combined meditative pose.
            WithArmPreference(1). // Orange-white arm.
            WithForearmPreference(4). // Pale white arm.
            WithVinePreference(4). // Pale vine.
            WithWheelPreference(5). // Sun fan thing.
            WithWingPreference(1); // Angel wings.

        public static NamelessDeityFormPreset SelectFirstAvailablePreset() => formPresets.FirstOrDefault(p => p.UsageCondition());

        public static NamelessDeityFormPreset RegisterFormPreset(Func<bool> usageCondition)
        {
            NamelessDeityFormPreset preset = new(usageCondition);
            formPresets.Add(preset);

            return preset;
        }
    }
}
