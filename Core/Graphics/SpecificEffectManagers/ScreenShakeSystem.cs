using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class ScreenShakeSystem : ModSystem
    {
        public class ShakeInfo
        {
            // This indicates the maximum amount BaseDirection can be rotated by when a shake occurs. At sufficiently high values there is effectively no shake direction and it's equivalent to NextVector2Circular.
            public float AngularVariance;

            // This indicate how much, in pixels, the screen should shake.
            public float ShakeStrength;

            // This indicates the general direction the shake should occur in.
            public Vector2 BaseDirection;

            // This indicates how much the shake should dissipate every frame.
            public float ShakeStrengthDissipationIncrement;

            public void Apply()
            {
                float shakeOffset = ShakeStrength * NoxusBossConfig.Instance.ScreenShakeIntensity;
                Main.screenPosition += BaseDirection.RotatedByRandom(AngularVariance) * shakeOffset;
            }
        }

        private static ShakeInfo universalRumble;

        private static readonly List<ShakeInfo> shakes = new();

        public static float OverallShakeIntensity => shakes.Sum(s => s.ShakeStrength);

        public static ShakeInfo StartShake(float strength, float angularVariance = TwoPi, Vector2? shakeDirection = null, float shakeStrengthDissipationIncrement = 0.2f)
        {
            ShakeInfo shake = new()
            {
                ShakeStrength = strength,
                AngularVariance = angularVariance,
                BaseDirection = (shakeDirection ?? Vector2.Zero).SafeNormalize(Vector2.UnitX),
                ShakeStrengthDissipationIncrement = shakeStrengthDissipationIncrement
            };

            shakes.Add(shake);
            return shake;
        }

        public static ShakeInfo StartShakeAtPoint(Vector2 shakeCenter, float strength, float angularVariance = TwoPi, Vector2? shakeDirection = null, float shakeStrengthDissipationIncrement = 0.2f, float intensityTaperEndDistance = 2300f, float intensityTaperStartDistance = 1476f)
        {
            // Calculate the shake strength based on how far away the player is from the shake center.
            float distanceToShakeCenter = Main.LocalPlayer.Distance(shakeCenter);
            float desiredScreenShakeStrength = InverseLerp(intensityTaperEndDistance, intensityTaperStartDistance, distanceToShakeCenter) * strength;

            // Start the shake with the distance taper in place.
            return StartShake(desiredScreenShakeStrength, angularVariance, shakeDirection, shakeStrengthDissipationIncrement);
        }

        public static void SetUniversalRumble(float strength, float angularVariance = TwoPi, Vector2? shakeDirection = null)
        {
            universalRumble = new()
            {
                ShakeStrength = strength,
                AngularVariance = angularVariance,
                BaseDirection = (shakeDirection ?? Vector2.Zero).SafeNormalize(Vector2.UnitX)
            };
        }

        public override void ModifyScreenPosition()
        {
            // Clear all shakes that are no longer in use.
            shakes.RemoveAll(s => s.ShakeStrength <= 0f);

            // Update the screen position based on shake intensities.
            foreach (ShakeInfo shake in shakes)
            {
                shake.Apply();

                // Make the shake dissipate in intensity.
                shake.ShakeStrength = Clamp(shake.ShakeStrength - shake.ShakeStrengthDissipationIncrement, 0f, 50f);
            }

            // Apply the univeral rumble if necessary.
            if (universalRumble is not null && OverallShakeIntensity < universalRumble.ShakeStrength)
            {
                universalRumble.Apply();
                shakes.Clear();
            }

            // Clear the universal rumble once it has dissipated.
            universalRumble = null;
        }
    }
}
