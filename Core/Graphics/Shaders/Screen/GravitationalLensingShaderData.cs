using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class GravitationalLensingShaderData(bool checkForPet, Ref<Effect> shader, string passName) : ScreenShaderData(shader, passName)
    {
        protected bool checkForPet = checkForPet;

        public static Projectile Quasar
        {
            get;
            internal set;
        }

        public static Projectile QuasarPet
        {
            get;
            internal set;
        }

        public static bool QuasarIsPresent
        {
            get
            {
                if (Quasar is null)
                    return false;

                Projectile quasarAtIndex = Main.projectile[Quasar.whoAmI];
                return quasarAtIndex.active && quasarAtIndex.type == ModContent.ProjectileType<Quasar>();
            }
        }

        public static bool QuasarPetIsPresent
        {
            get
            {
                if (QuasarPet is null)
                    return false;

                Projectile quasarAtIndex = Main.projectile[QuasarPet.whoAmI];
                return quasarAtIndex.active && quasarAtIndex.type == ModContent.ProjectileType<BlackHolePet>();
            }
        }

        public const string ShaderKey = "NoxusBoss:GravitationalLensing";

        public const string ShaderKeyPet = ShaderKey + "Pet";

        internal static void Load()
        {
            NoxusPlayer.GetAlphaEvent += ApplyRedshift;
        }

        private static void ApplyRedshift(NoxusPlayer p, ref Color drawColor)
        {
            if (Quasar is not null && Quasar.type == ModContent.ProjectileType<Quasar>())
            {
                // Make players appear redshifted as they get closer to the source quasar.
                float distanceToCenter = p.Player.Distance(Quasar.Center) / Quasar.scale * 10f;
                float redshiftDistanceInterpolant = InverseLerp(400f, 300f, distanceToCenter);
                float invisibilityDistanceInterpolant = InverseLerp(220f, 150f, distanceToCenter);
                Color redshift = Color.Lerp(Color.White, Color.Red with { A = 196 }, InverseLerp(0.7f, 0.9f, Quasar.Opacity) * redshiftDistanceInterpolant);
                drawColor = drawColor.MultiplyRGBA(redshift);

                // Make the players become invisible as they become very, very close to the source quasar. This is consistent with how black holes work in the real world.
                drawColor *= 1f - invisibilityDistanceInterpolant;
            }
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = QuasarPetIsPresent;
            if (shouldBeActive && !Filters.Scene[ShaderKeyPet].IsActive())
                Filters.Scene.Activate(ShaderKeyPet);
            if (!shouldBeActive && Filters.Scene[ShaderKeyPet].IsActive())
                Filters.Scene.Deactivate(ShaderKeyPet);

            shouldBeActive = QuasarIsPresent;
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public static void DoGenericShaderPreparations(Effect shader)
        {
            float width = Main.instance.GraphicsDevice.Viewport.Width;
            float height = Main.instance.GraphicsDevice.Viewport.Height;
            Vector2 aspectRatioCorrectionFactor = new(width / (float)height, 1f);

            shader.Parameters["globalTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["aspectRatioCorrectionFactor"]?.SetValue(aspectRatioCorrectionFactor);
            shader.Parameters["maxLensingAngle"]?.SetValue(28.2f);
            shader.Parameters["accretionDiskFadeColor"]?.SetValue(Color.Lerp(Color.Coral, Color.Orange, Sin01(Main.GlobalTimeWrappedHourly * 16f) * 0.25f).ToVector3());
            Main.instance.GraphicsDevice.Textures[1] = PsychedelicWingTextureOffsetMap;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.AnisotropicWrap;
        }

        public override void Apply()
        {
            Vector2 aspectRatioCorrectionFactor = new(Main.screenWidth / (float)Main.screenHeight, 1f);

            bool inUse = checkForPet ? QuasarPetIsPresent : QuasarIsPresent;
            if (inUse)
            {
                Projectile sourceProjectile = checkForPet ? QuasarPet : Quasar;
                float blackRadius = sourceProjectile.width * sourceProjectile.scale / 2560f * 0.048f;
                if (checkForPet)
                {
                    float scale = EasingCurves.Elastic.Evaluate(EasingType.In, sourceProjectile.scale);
                    if (scale < 0.01f)
                        scale = 0.01f;
                    blackRadius = scale * 0.03f;
                }
                else
                    blackRadius *= 1.62f;

                Vector2 sourcePosition = (WorldSpaceToScreenUV(sourceProjectile.Center) - Vector2.One * 0.5f) * aspectRatioCorrectionFactor + Vector2.One * 0.5f;
                Shader.Parameters["sourcePosition"]?.SetValue(sourcePosition);
                Shader.Parameters["blackRadius"]?.SetValue(blackRadius);
                Shader.Parameters["distortionStrength"]?.SetValue(sourceProjectile.Opacity);
            }
            else
                Shader.Parameters["distortionStrength"]?.SetValue(0f);
            UseColor(Color.Wheat);
            UseSecondaryColor(Color.LightGoldenrodYellow);
            DoGenericShaderPreparations(Shader);

            base.Apply();
        }
    }
}
