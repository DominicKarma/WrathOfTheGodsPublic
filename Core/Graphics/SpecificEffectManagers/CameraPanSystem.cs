using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class CameraPanSystem : ModSystem
    {
        public static Vector2 CameraFocusPoint
        {
            get;
            set;
        }

        /// <summary>
        /// Where the <see cref="Main.screenPosition"/> would be without modifications.
        /// </summary>
        public static Vector2 UnmodifiedCameraPosition =>
            Main.LocalPlayer.TopLeft + new Vector2(Main.LocalPlayer.width * 0.5f, Main.LocalPlayer.height - 21f) - Main.ScreenSize.ToVector2() * 0.5f + Vector2.UnitY * Main.LocalPlayer.gfxOffY;

        public static float CameraPanInterpolant
        {
            get;
            set;
        }

        public static float Zoom
        {
            get;
            set;
        }

        public override void ModifyScreenPosition()
        {
            if (Main.LocalPlayer.dead && !Main.gamePaused)
            {
                Zoom = Lerp(Zoom, 0f, 0.13f);
                CameraPanInterpolant = 0f;
                return;
            }

            // Handle camera focus effects.
            if (CameraPanInterpolant > 0f)
            {
                Vector2 idealScreenPosition = CameraFocusPoint - Main.ScreenSize.ToVector2() * 0.5f;
                Main.screenPosition = Vector2.Lerp(Main.screenPosition, idealScreenPosition, CameraPanInterpolant);
            }

            // Make interpolants gradually return to their original values.
            if (!Main.gamePaused)
            {
                CameraPanInterpolant = Clamp(CameraPanInterpolant - 0.06f, 0f, 1f);
                Zoom = Lerp(Zoom, 0f, 0.09f);
            }
        }

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
        {
            Transform.Zoom *= 1f + Zoom;
        }
    }
}
