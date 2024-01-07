using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusEggSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NoxusEggScreenShaderData.DistortionIntensity > 0f || NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>()) || NPC.AnyNPCs(ModContent.NPCType<EntropicGod>());

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NoxusEggSky", isActive);
        }
    }

    public class NoxusEggScreenShaderData : ScreenShaderData
    {
        public static Vector2 NoxusEggPosition
        {
            get;
            set;
        }

        public static float DistortionIntensity
        {
            get;
            set;
        }

        public static float VignetteInterpolant
        {
            get;
            set;
        }

        public static int DistortionPointCount => 6;

        public NoxusEggScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            int eggIndex = NPC.FindFirstNPC(ModContent.NPCType<NoxusEgg>());
            int noxusIndex = NPC.FindFirstNPC(ModContent.NPCType<EntropicGod>());
            bool disabledDueToDeathAnimation = NoxusDeathCutsceneSystem.AnimationTimer >= 1 && NoxusDeathCutsceneSystem.AnimationTimer < NoxusDeathCutsceneSystem.AnimationDuration - NoxusDeathCutsceneSystem.ExplosionTime - 150;
            if (eggIndex != -1)
            {
                NPC egg = Main.npc[eggIndex];
                NoxusEggPosition = egg.Center;

                float maxIntensity = 1f;
                if (egg.As<NoxusEgg>().CurrentAttack == NoxusEgg.NoxusEggAttackType.Awaken)
                    maxIntensity = Clamp(egg.As<NoxusEgg>().AttackTimer * 0.005f, 0.01f, 1f);
                VignetteInterpolant = maxIntensity;

                DistortionIntensity = Clamp(egg.Opacity * egg.scale, 0f, maxIntensity);
            }

            else if (noxusIndex != -1 && !disabledDueToDeathAnimation)
            {
                NPC noxus = Main.npc[noxusIndex];
                NoxusEggPosition = noxus.Center;
                if (NoxusDeathCutsceneSystem.AnimationTimer >= NoxusDeathCutsceneSystem.AnimationDuration - NoxusDeathCutsceneSystem.ExplosionTime - 150)
                    DistortionIntensity = Clamp(DistortionIntensity + 0.14f, 0f, 1.5f);
                else
                    DistortionIntensity = Clamp(noxus.Opacity * noxus.scale * 0.9f, 0f, 1f);

                VignetteInterpolant = 1f;
            }

            else
            {
                DistortionIntensity = Clamp(DistortionIntensity - 0.1f, 0f, 1f);
                VignetteInterpolant = Clamp(VignetteInterpolant - 0.1f, 0f, 1f);
            }
        }

        public override void Apply()
        {
            UseIntensity(DistortionIntensity);

            Vector2[] distortionCenters = new Vector2[DistortionPointCount];
            distortionCenters[DistortionPointCount - 1] = NoxusEggPosition;

            // Count distortion field projectiles as part of the effect.
            List<Vector2> distortionFields = Main.projectile.Where(p => p.active && p.type == ModContent.ProjectileType<DistortionField>()).Select(p => p.Center).ToList();
            if (distortionFields.Any())
            {
                for (int i = 0; i < MathF.Min(DistortionPointCount - 1f, distortionFields.Count); i++)
                    distortionCenters[i] = distortionFields[i];
            }

            for (int i = 0; i < distortionCenters.Length; i++)
                distortionCenters[i] = WorldSpaceToScreenUV(distortionCenters[i]);

            Shader.Parameters["distortionCenters"].SetValue(distortionCenters);

            float darknessFactor = (0.25f + (1f - ((NoxusSky)SkyManager.Instance["NoxusBoss:NoxusSky"]).GetCloudAlpha()) * 0.3f) * DistortionIntensity;
            if (AllProjectilesByID(ModContent.ProjectileType<NightmareDeathRay>()).Any())
                darknessFactor = 0.9f;

            float darknessDissipateInterpolant = InverseLerp(20f, 11f, Main.ColorOfTheSkies.ToVector3().Length());
            darknessFactor = Lerp(darknessFactor, 1f, Main.ColorOfTheSkies.ToVector3().Length());

            Shader.Parameters["darknessFactor"].SetValue(Lerp(1f, darknessFactor, DistortionIntensity));
            Shader.Parameters["vignetteIntensityInterpolant"].SetValue(VignetteInterpolant);

            base.Apply();
        }
    }
}
