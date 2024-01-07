using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Backgrounds
{
    public class EternalGardenBGStyle : ModUndergroundBackgroundStyle
    {
        // The player will never naturally see this.
        public override void FillTextureArray(int[] textureSlots)
        {
            for (int i = 0; i <= 3; i++)
                textureSlots[i] = 1;
        }
    }

    public class EternalGardenSurfaceBGStyle : ModSurfaceBackgroundStyle
    {
        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            // Go away this background is going to be drawn manually.
            b += 99999f;

            int frameOffset = (int)(Main.GameUpdateCount / 10U) % 4;
            int backgroundFrameIndex = 251 + frameOffset;

            return BackgroundTextureLoader.GetBackgroundSlot($"Terraria/Images/Background_{backgroundFrameIndex}");
        }

        public override void Load()
        {
            // Load the vanilla background with the large lake animation frames.
            for (int i = 251; i <= 254; i++)
                BackgroundTextureLoader.AddBackgroundTexture(Mod, $"Terraria/Images/Background_{i}");
        }

        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            // This just fades in the background and fades out other backgrounds.
            for (int i = 0; i < fades.Length; i++)
            {
                bool shouldFadeIn = i == Slot;
                fades[i] = Clamp(fades[i] + transitionSpeed * shouldFadeIn.ToDirectionInt(), 0f, 1f);
            }
        }
    }
}
