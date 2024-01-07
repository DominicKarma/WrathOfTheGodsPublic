using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class LocalScreenSplitScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => LocalScreenSplitSystem.SplitWidths.Any(w => w >= 0.01f);

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:LocalScreenSplit", isActive);
        }
    }

    public class LocalScreenSplitSystem : ModSystem
    {
        public static Vector2[] SplitCenters
        {
            get;
            set;
        } = new Vector2[MaxSplitCount];

        public static float[] SplitAngles
        {
            get;
            set;
        } = new float[MaxSplitCount];

        public static int[] SplitTimers
        {
            get;
            set;
        } = new int[MaxSplitCount];

        public static int[] SplitLifetimes
        {
            get;
            set;
        } = new int[MaxSplitCount];

        public static float[] SplitWidths
        {
            get;
            set;
        } = new float[MaxSplitCount];

        public static float[] MaxSplitWidths
        {
            get;
            set;
        } = new float[MaxSplitCount];

        public static float[] SplitSlopes => SplitAngles.Select(Tan).ToArray();

        public static float[] SplitCompletionRatios
        {
            get
            {
                float[] ratios = new float[MaxSplitCount];
                for (int i = 0; i < MaxSplitCount; i++)
                {
                    ratios[i] = SplitTimers[i] / (float)SplitLifetimes[i];
                    if (!float.IsNormal(ratios[i]))
                        ratios[i] = 0f;
                }

                return ratios;
            }
        }

        public static bool UseCosmicEffect
        {
            get;
            set;
        }

        public const int MaxSplitCount = 10;

        public override void PostUpdateProjectiles()
        {
            for (int i = 0; i < MaxSplitCount; i++)
            {
                // Increment the Split timer if it's active. Once its reaches its natural maximum the effect ceases.
                if (SplitTimers[i] >= 1)
                {
                    SplitTimers[i]++;

                    if (SplitTimers[i] >= SplitLifetimes[i])
                        SplitTimers[i] = 0;
                }

                SplitWidths[i] = Convert01To010(SplitCompletionRatios[i]) * MaxSplitWidths[i];
            }

            UseCosmicEffect = AnyProjectiles(ModContent.ProjectileType<SuperCosmicBeam>());
        }

        public static void Start(Vector2 splitCenter, int splitTime, float splitAngle, float splitWidth)
        {
            for (int i = 0; i < MaxSplitCount; i++)
            {
                if (SplitTimers[i] > 0)
                    continue;

                SplitCenters[i] = splitCenter;
                SplitTimers[i] = 1;
                SplitLifetimes[i] = splitTime;
                SplitAngles[i] = splitAngle;
                SplitWidths[i] = MaxSplitWidths[i] = splitWidth;
                break;
            }
        }
    }
}
