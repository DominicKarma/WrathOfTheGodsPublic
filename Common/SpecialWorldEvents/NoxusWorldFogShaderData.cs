using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Enemies.NoxusWorld.DismalSeekers;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Common.SpecialWorldEvents
{
    public class NoxusWorldFogShaderScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NoxusFogEventManager.FogDrawIntensity >= 0.01f;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NoxusWorldFog", isActive);
        }
    }

    public class NoxusWorldFogShaderData : ScreenShaderData
    {
        private static readonly float[] smoothBrightnesses = new float[2];

        private static readonly Vector2[] sourcePositions = new Vector2[2];

        public NoxusWorldFogShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            UseOpacity(0.001f);
        }

        public override void Apply()
        {
            float belowSurfaceFadeout = InverseLerp((float)Main.worldSurface + 50f, (float)Main.worldSurface - 10f, Main.LocalPlayer.Center.Y / 16f);

            Main.instance.GraphicsDevice.Textures[1] = SmudgeNoise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            UseColor(Color.Lerp(Color.DarkMagenta, Color.DarkGray, 0.7f) * 0.4f);
            UseIntensity(NoxusFogEventManager.FogDrawIntensity * belowSurfaceFadeout);

            int seekerIndex = NPC.FindFirstNPC(ModContent.NPCType<DismalSeeker>());
            var lanterns = AllProjectilesByID(ModContent.ProjectileType<DismalSeekerLantern>());
            Projectile usedLantern = null;

            // Calculate source positions. If a dismal seeker is present, it counts as a separate source.
            sourcePositions[0] = Main.LocalPlayer.Center;
            if (lanterns.Any())
            {
                usedLantern = lanterns.First();
                sourcePositions[1] = usedLantern.Center;
            }
            else if (seekerIndex >= 0 && Main.npc[seekerIndex].As<DismalSeeker>().LanternIsInUse)
                sourcePositions[1] = Main.npc[seekerIndex].Center;

            // Calculate brightness values for each source position.
            for (int i = 0; i < sourcePositions.Length; i++)
            {
                Vector2 sourcePosition = sourcePositions[i];
                float brightness = Pow(Clamp(Lighting.Brightness((int)(sourcePosition.X / 16f), (int)(sourcePosition.Y / 16f)), 0f, 1.2f), 2f);
                if (i == 1)
                {
                    brightness = seekerIndex >= 0 ? (1f - Main.npc[seekerIndex].As<DismalSeeker>().LanternBackglowFadeout) * 1.2f : 0f;
                    if (usedLantern is not null)
                        brightness = usedLantern.Opacity;
                }

                // Smoothly interpolate towards the desired brightness.
                smoothBrightnesses[i] = Lerp(smoothBrightnesses[i], brightness, 0.1f);
                if (brightness < smoothBrightnesses[i])
                    smoothBrightnesses[i] = Clamp(smoothBrightnesses[i] - 0.01f, 0f, 10f);
            }

            Shader.Parameters["centerBrightnesses"]?.SetValue(smoothBrightnesses);
            Shader.Parameters["sourcePositions"]?.SetValue(sourcePositions.Select(WorldSpaceToScreenUV).ToArray());
            base.Apply();
        }
    }
}
