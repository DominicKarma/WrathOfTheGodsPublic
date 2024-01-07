using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Assets
{
    public class MiscTexturesRegistry : ModSystem
    {
        #region Texture Path Constants

        public const string InvisiblePixelPath = $"{ExtraTexturesPath}/InvisiblePixel";

        public const string ExtraTexturesPath = "NoxusBoss/Assets/ExtraTextures";

        public const string GreyscaleTexturesPath = "NoxusBoss/Assets/ExtraTextures/GreyscaleTextures";

        public const string LineTexturesPath = "NoxusBoss/Assets/ExtraTextures/Lines";

        public const string TrailStreakTexturesPath = "NoxusBoss/Assets/ExtraTextures/TrailStreaks";

        public const string SparkTexturePath = "Terraria/Images/Extra_89";

        public const string ChromaticBurstPath = $"{GreyscaleTexturesPath}/ChromaticBurst";

        #endregion Texture Path Constants

        #region Greyscale Textures

        // Typically used for backglow visuals.
        public static readonly Texture2D BloomCircle = LoadDeferred($"{GreyscaleTexturesPath}/BloomCircle");

        public static readonly Texture2D BloomCircleSmall = LoadDeferred($"{GreyscaleTexturesPath}/BloomCircleSmall");

        // General purpose greyscale bloom flare. Very useful for semi-weak overlays.
        public static readonly Texture2D BloomFlare = LoadDeferred($"{GreyscaleTexturesPath}/BloomFlare");

        public static readonly Texture2D BloomLineTexture = LoadDeferred($"{GreyscaleTexturesPath}/BloomLine");

        public static readonly Texture2D BrightSpiresTexture = LoadDeferred($"{GreyscaleTexturesPath}/BrightSpires");

        public static readonly Texture2D BurnNoise = LoadDeferred($"{GreyscaleTexturesPath}/BurnNoise");

        public static readonly Texture2D ChromaticSpires = LoadDeferred($"{GreyscaleTexturesPath}/ChromaticSpires");

        public static readonly Texture2D ChromaticBurst = LoadDeferred(ChromaticBurstPath);

        public static readonly Texture2D CoronaTexture = LoadDeferred($"{GreyscaleTexturesPath}/Corona");

        public static readonly Texture2D CrackedNoise = LoadDeferred($"{GreyscaleTexturesPath}/CrackedNoise");

        public static readonly Texture2D DendriticNoise = LoadDeferred($"{GreyscaleTexturesPath}/DendriticNoise");

        public static readonly Texture2D DendriticNoiseZoomedOut = LoadDeferred($"{GreyscaleTexturesPath}/DendriticNoiseZoomedOut");

        public static readonly Texture2D FireNoise = LoadDeferred($"{GreyscaleTexturesPath}/FireNoise");

        public static readonly Texture2D FogTexture = LoadDeferred($"{GreyscaleTexturesPath}/Fog");

        public static readonly Texture2D FourPointedStarTexture = LoadDeferred($"{GreyscaleTexturesPath}/FourPointedStar");

        public static readonly Texture2D HollowCircleSoftEdge = LoadDeferred($"{GreyscaleTexturesPath}/HollowCircleSoftEdge");

        public static readonly Texture2D[] GalaxyTextures = LoadDeferred($"{ExtraTexturesPath}/Galaxies/Galaxy", 9);

        // Intended to be used via portals when (dis)appearing so that they fade out in ways that are more interesting than a mere typical scale factor.
        public static readonly Texture2D LemniscateDistanceLookup = LoadDeferred($"{GreyscaleTexturesPath}/LemniscateDistanceLookup");

        public static readonly Texture2D MoltenNoise = LoadDeferred($"{GreyscaleTexturesPath}/MoltenNoise");

        public static readonly Texture2D PerlinNoise = LoadDeferred("Terraria/Images/Misc/Perlin");

        public static readonly Texture2D SharpNoise = LoadDeferred($"{GreyscaleTexturesPath}/SharpNoise");

        public static readonly Texture2D SmudgeNoise = LoadDeferred($"{GreyscaleTexturesPath}/SmudgeNoise");

        public static readonly Texture2D SparkTexture = LoadDeferred(SparkTexturePath);

        public static readonly Texture2D SpikesTexture = LoadDeferred($"{GreyscaleTexturesPath}/Spikes");

        // Same as LemniscateDistanceLookup.
        public static readonly Texture2D StarDistanceLookup = LoadDeferred($"{GreyscaleTexturesPath}/StarDistanceLookup");

        public static readonly Texture2D SwirlNoise = LoadDeferred($"{GreyscaleTexturesPath}/SwirlNoise");

        // BASED noise!
        public static readonly Texture2D TurbulentNoise = LoadDeferred($"{GreyscaleTexturesPath}/TurbulentNoise");

        public static readonly Texture2D ViscousNoise = LoadDeferred($"{GreyscaleTexturesPath}/ViscousNoise");

        // MORE based noise!
        public static readonly Texture2D WavyBlotchNoise = LoadDeferred($"{GreyscaleTexturesPath}/WavyBlotchNoise");

        // Simple, 1x1 white pixel texture. Can be used for a variety of functions in conjunction with upscaling.
        public static readonly Texture2D WhitePixel = LoadDeferred($"{GreyscaleTexturesPath}/Pixel");

        #endregion Greyscale Textures

        #region Backgrounds and Color Textures

        // Notably used by Nameless' portal textures to ensure that the inside of the portal contains a mostly dark aesthetic but with some bright stardust clouds.
        public static readonly Texture2D CosmosTexture = LoadDeferred($"{ExtraTexturesPath}/Cosmos");

        // Used by various effects from Nameless, such as the "revealed" portion of his reality tears.
        public static readonly Texture2D DivineLightTexture = LoadDeferred($"{ExtraTexturesPath}/DivineLight");

        // Contains information about how much pixels should be offset, with the Red and Green channels corresponding the X and Y coordinates of such offsets.
        // The way it was created was by sampling TurbulentNoise, multiplying its pixels by 16, and treating said result as an angle that was then converted into a 
        // direction based on the incredible <cos(t), sin(t)> equation. This used to be done in the NamelessDeityPsychedelicWingShader but was extracted in this separate texture
        // for performance reasons.
        public static readonly Texture2D PsychedelicWingTextureOffsetMap = LoadDeferred($"{ExtraTexturesPath}/PsychedelicWingTextureOffsetMap");

        // Used by a bunch of Noxus' effects, such as his second phase background and portals.
        public static readonly Texture2D VoidTexture = LoadDeferred($"{ExtraTexturesPath}/Void");

        #endregion Backgrounds and Color Textures

        #region Invisible Pixel

        // Self-explanatory. Sometimes shaders need a "blank slate" in the form of an invisible texture to draw their true contents onto, which this can be beneficial for.
        public static readonly Texture2D InvisiblePixel = LoadDeferred(InvisiblePixelPath);

        #endregion Invisible Pixel

        #region Trail Streak Textures

        public static readonly Texture2D FadedLine = LoadDeferred($"{TrailStreakTexturesPath}/FadedLine");

        public static readonly Texture2D StreakBloomLine = LoadDeferred($"{TrailStreakTexturesPath}/StreakBloomLine");

        public static readonly Texture2D StreakFlamelash = LoadDeferred($"Terraria/Images/Extra_{ExtrasID.FlameLashTrailShape}");

        public static readonly Texture2D StreakLightning = LoadDeferred($"{TrailStreakTexturesPath}/StreakLightning");

        public static readonly Texture2D StreakMagma = LoadDeferred($"{TrailStreakTexturesPath}/StreakMagma");

        public static readonly Texture2D StreakNightmareDeathray = LoadDeferred($"{TrailStreakTexturesPath}/StreakNightmareDeathray");

        public static readonly Texture2D StreakNightmareDeathrayOverlay = LoadDeferred($"{TrailStreakTexturesPath}/StreakNightmareDeathrayOverlay");

        #endregion Trail Streak Textures

        #region Loader Utility
        private static Texture2D LoadDeferred(string path)
        {
            // Don't attempt to load anything serverside.
            if (Main.netMode == NetmodeID.Server)
                return default;

            return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        }

        private static Texture2D[] LoadDeferred(string path, int textureCount)
        {
            // Don't attempt to load anything serverside.
            if (Main.netMode == NetmodeID.Server)
                return default;

            Texture2D[] textures = new Texture2D[textureCount];
            for (int i = 0; i < textureCount; i++)
                textures[i] = LoadDeferred($"{path}{i + 1}");

            return textures;
        }
        #endregion Loader Utility
    }
}
