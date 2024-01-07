using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NoxusSky.SkyIntensityOverride > 0f || (NPC.AnyNPCs(ModContent.NPCType<EntropicGod>()) && NamelessDeityBoss.Myself is null) || NoxusSky.InProximityOfMidnightMonolith;

        public override void Load()
        {
            Terraria.GameContent.Events.On_MoonlordDeathDrama.DrawWhite += DrawFog;
        }

        private void DrawFog(Terraria.GameContent.Events.On_MoonlordDeathDrama.orig_DrawWhite orig, SpriteBatch spriteBatch)
        {
            orig(spriteBatch);

            var sky = (NoxusSky)SkyManager.Instance["NoxusBoss:NoxusSky"];

            if (NoxusSky.FogIntensity >= 0.0001f)
            {
                spriteBatch.PrepareForShaders();
                sky.DrawFog();
            }

            var twinkleParticles = ParticleManager.activeParticles.Where(p => p is TwinkleParticle).Cast<TwinkleParticle>();
            if (twinkleParticles.Any())
            {
                spriteBatch.UseBlendState(BlendState.Additive);

                // Draw twinkles on top of the fog.
                foreach (TwinkleParticle t in twinkleParticles)
                {
                    t.Opacity *= 0.6f;
                    t.Draw();
                    t.Opacity /= 0.6f;
                }
                spriteBatch.ResetToDefault();
            }

            SpecialLayeringSystem.EmptyDrawCache_NPC(SpecialLayeringSystem.DrawCacheAfterNoxusFog);
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NoxusSky", isActive);
        }
    }

    public class NoxusSky : CustomSky
    {
        public class FloatingRubble
        {
            public int Time;

            public int Lifetime;

            public int Variant;

            public float Depth;

            public Vector2 Position;

            public float Opacity => InverseLerpBump(0f, 20f, Lifetime - 90f, Lifetime, Time);

            public void Update()
            {
                Position += Vector2.UnitY * Sin(TwoPi * Time / 180f) * 1.2f;
                Time++;
            }
        }

        private bool isActive;

        private float fogSpreadDistance;

        private Vector2 fogCenter;

        private readonly List<FloatingRubble> rubble = new();

        private static Asset<Texture2D> rubbleTextureAsset;

        internal static float intensity;

        public static bool HasMidnightMonolithAccessory => Main.LocalPlayer.GetValueRef<bool>(MidnightMonolith.HasMidnightMonolithAccessoryFieldName);

        public static TimeSpan DrawCooldown
        {
            get;
            set;
        }

        public static TimeSpan LastFrameElapsedGameTime
        {
            get;
            set;
        }

        public static float FogIntensity
        {
            get;
            private set;
        }

        public static float FlashIntensity
        {
            get;
            private set;
        }

        public static float SkyIntensityOverride
        {
            get;
            set;
        }

        public static float WindTimer
        {
            get;
            set;
        }

        public static float WindSpeedFactor
        {
            get;
            set;
        }

        public static Vector2 FlashNoiseOffset
        {
            get;
            private set;
        }

        public static Vector2 FlashPosition
        {
            get;
            private set;
        }

        public static bool InProximityOfMidnightMonolith
        {
            get;
            set;
        }

        // Ideally it'd be possible to just turn InProximityOfMidnightMonolith back to false if it was already on and its effects were registered, but since NearbyEffects hooks
        // don't run on the same update cycle as the PrepareDimensionTarget method this delay exists.
        public static int TimeSinceCloseToMidnightMonolith
        {
            get;
            set;
        }

        public static Color FogColor => new(49, 40, 70);

        public static readonly SoundStyle ThunderSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Environment/ThunderRumble", 3) with { Volume = 0.32f, PitchVariance = 0.35f };

        public override void Update(GameTime gameTime)
        {
            // Keep the effect active when generating a Noxus World.
            float maxIntensity = 1f;
            if (WorldGen.generatingWorld && Main.gameMenu && NoxusWorldManager.Enabled)
            {
                maxIntensity = 0.88f;
                isActive = true;
            }

            // Increase the Midnight monolith proximity timer.
            if (!Main.gamePaused && Main.instance.IsActive)
                TimeSinceCloseToMidnightMonolith++;
            if (TimeSinceCloseToMidnightMonolith >= 10)
                InProximityOfMidnightMonolith = false;

            // Make the intensity go up or down based on whether the sky is in use.
            intensity = Clamp(intensity + isActive.ToDirectionInt() * 0.01f, 0f, maxIntensity);

            // Make the fog intensity go down if the sky is not in use. It does not go up by default, however.
            FogIntensity = Clamp(FogIntensity - (!isActive).ToInt(), 0f, 1f);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the dark cloud of death.
            if (isActive)
                SkyManager.Instance["Ambience"].Deactivate();

            // Make flashes exponentially decay into nothing.
            FlashIntensity *= 0.88f;

            // Randomly create flashes.
            int flashCreationChance = 540;
            int noxusIndex = NPC.FindFirstNPC(ModContent.NPCType<EntropicGod>());
            float flashIntensity = NoxusBossConfig.Instance.VisualOverlayIntensity * 71f;
            if (noxusIndex != -1)
            {
                NPC noxus = Main.npc[noxusIndex];
                flashCreationChance = (int)Lerp(210, 36, 1f - noxus.life / (float)noxus.lifeMax);
                flashIntensity = Lerp(35f, 50f, 1f - noxus.life / (float)noxus.lifeMax);
            }
            if (InProximityOfMidnightMonolith)
            {
                flashCreationChance = 150;
                flashIntensity = 80f;
            }

            if (FlashIntensity <= 2f && FogIntensity < 1f && Main.rand.NextBool(flashCreationChance) && NoxusDeathCutsceneSystem.AnimationTimer <= 0)
            {
                FlashIntensity = flashIntensity * (1f - FogIntensity);
                FlashNoiseOffset = Main.rand.NextVector2Square(0f, 1f);
                FlashPosition = Main.rand.NextVector2Square(0.3f, 0.7f);
                if (Main.instance.IsActive && (EntropicGod.Myself is not null || !HasMidnightMonolithAccessory))
                    SoundEngine.PlaySound(ThunderSound with { Volume = (1f - FogIntensity) * 0.9f, MaxInstances = 5 });
            }

            // Prepare the fog overlay.
            if (EntropicGod.Myself is not null)
            {
                FogIntensity = EntropicGod.Myself.As<EntropicGod>().FogIntensity;
                fogSpreadDistance = EntropicGod.Myself.As<EntropicGod>().FogSpreadDistance;
                fogCenter = EntropicGod.Myself.Center + EntropicGod.Myself.As<EntropicGod>().HeadOffset;
            }

            // Randomly create rubble around the player.
            if (Main.rand.NextBool(20) && rubble.Count <= 80)
            {
                FloatingRubble r = new()
                {
                    Depth = Main.rand.NextFloat(1.1f, 2.78f),
                    Variant = Main.rand.Next(3),
                    Position = new Vector2(Main.LocalPlayer.Center.X + Main.rand.NextFloatDirection() * 3300f, Main.rand.NextFloat(8000f)),
                    Lifetime = Main.rand.Next(240, 360)
                };
                rubble.Add(r);
            }

            // Update all rubble.
            rubble.RemoveAll(r => r.Time >= r.Lifetime);
            rubble.ForEach(r => r.Update());

            if (InProximityOfMidnightMonolith)
            {
                SkyIntensityOverride = Clamp(SkyIntensityOverride + 0.08f, 0f, 1f);
                intensity = SkyIntensityOverride;
            }
            else
                SkyIntensityOverride = Clamp(SkyIntensityOverride - 0.07f, 0f, 1f);

            // Increment time.
            WindTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * WindSpeedFactor;

            // Make the wind speed factor return to a default resting state.
            float idealWindSpeedFactor = 0.9f;
            if (EntropicGod.Myself is not null && EntropicGod.Myself.As<EntropicGod>().CurrentPhase >= 1)
                idealWindSpeedFactor = 1.1f;
            if (EntropicGod.Myself is not null && EntropicGod.Myself.As<EntropicGod>().CurrentPhase >= 2)
                idealWindSpeedFactor = 1.56f;
            WindSpeedFactor = Lerp(WindSpeedFactor, idealWindSpeedFactor, 0.1f);
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, Color.White, intensity * 0.5f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Ensure that the background only draws once per frame for efficiency.
            DrawCooldown -= LastFrameElapsedGameTime;
            bool generatingNoxusWorld = Main.gameMenu && NoxusWorldManager.Enabled;
            if ((minDepth >= -1000000f || (DrawCooldown.TotalMilliseconds >= 17 && Main.instance.IsActive)) && !generatingNoxusWorld)
                return;

            // Draw the sky background overlay, sun, and smoke.
            DrawCooldown = TimeSpan.FromSeconds(1D / 60D);

            Main.spriteBatch.PrepareForShaders();
            DrawBackground();

            DrawRubble(minDepth, maxDepth);
        }

        public static void DrawBackground()
        {
            // Make the background colors more muted based on how strong the fog is.
            Color baseColor = Color.Lerp(Color.Lerp(Color.BlueViolet, Color.Indigo, 0.6f), Color.DarkGray, 0.2f);
            if (EntropicGod.Myself is not null)
            {
                float fogIntensity = EntropicGod.Myself.As<EntropicGod>().FogIntensity;
                float fogSpreadDistance = EntropicGod.Myself.As<EntropicGod>().FogSpreadDistance;
                float colorDarknessInterpolant = Clamp(fogSpreadDistance * InverseLerp(0f, 0.15f, fogIntensity), 0f, 1f);
                baseColor = Color.Lerp(baseColor, Color.DarkGray, colorDarknessInterpolant * 0.7f);
            }

            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / WhitePixel.Size() * 2f;

            // Calculate Noxus' UV position on the background texture.
            Vector2 noxusUV = EntropicGod.Myself is null ? Vector2.One * 100f : (EntropicGod.Myself.Center - Main.screenPosition + screenArea * 0.5f + new Vector2(380f, 200f)) / textureArea;

            // Draw the background with a special shader.
            var backgroundShader = ShaderManager.GetShader("NoxusBackgroundShader");
            backgroundShader.TrySetParameter("intensity", Clamp(intensity, SkyIntensityOverride, 1f));
            backgroundShader.TrySetParameter("scrollSpeed", 0.13f);
            backgroundShader.TrySetParameter("noiseZoom", 0.32f);
            backgroundShader.TrySetParameter("flashCoordsOffset", FlashNoiseOffset);
            backgroundShader.TrySetParameter("flashPosition", FlashPosition);
            backgroundShader.TrySetParameter("flashIntensity", FlashIntensity);
            backgroundShader.TrySetParameter("flashNoiseZoom", 1.76f);
            backgroundShader.TrySetParameter("screenPosition", Main.screenPosition);
            backgroundShader.TrySetParameter("backgroundColor1", baseColor.ToVector3());
            backgroundShader.TrySetParameter("backgroundColor2", new Color(16, 4, 27).ToVector3());
            backgroundShader.TrySetParameter("noxusPosition", noxusUV);
            backgroundShader.TrySetParameter("time", WindTimer);
            backgroundShader.TrySetParameter("darknessIntensity", EntropicGod.Myself is null ? 0f : Clamp(EntropicGod.Myself.Opacity, 0f, 1f));
            backgroundShader.SetTexture(SwirlNoise, 1, SamplerState.LinearWrap);
            backgroundShader.SetTexture(DendriticNoiseZoomedOut, 2, SamplerState.LinearWrap);
            backgroundShader.SetTexture(CrackedNoise, 3, SamplerState.AnisotropicWrap);
            backgroundShader.Apply();
            Main.spriteBatch.Draw(WhitePixel, screenArea * 0.5f, null, Color.White, 0f, WhitePixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        public void DrawFog()
        {
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / WhitePixel.Size();

            var backgroundShader = ShaderManager.GetShader("DarkFogShader");
            backgroundShader.TrySetParameter("fogCenter", (fogCenter - Main.screenPosition) / screenArea);
            backgroundShader.TrySetParameter("screenResolution", screenArea);
            backgroundShader.TrySetParameter("fogTravelDistance", fogSpreadDistance);
            backgroundShader.SetTexture(SmudgeNoise, 1);
            backgroundShader.Apply();
            Main.spriteBatch.Draw(WhitePixel, Vector2.Zero, null, FogColor * FogIntensity, 0f, Vector2.Zero, textureArea, 0, 0f);
        }

        public void DrawRubble(float minDepth, float maxDepth)
        {
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle cutoffArea = new(-1000, -1000, 4000, 4000);
            rubbleTextureAsset ??= ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SpecificEffectManagers/BackgroundRubble");

            Texture2D rubbleTexture = rubbleTextureAsset.Value;
            for (int i = 0; i < rubble.Count; i++)
            {
                if (rubble[i].Depth > minDepth && rubble[i].Depth < maxDepth)
                {
                    Vector2 rubbleScale = new(1f / rubble[i].Depth, 0.9f / rubble[i].Depth);
                    Vector2 position = (rubble[i].Position - screenCenter) * rubbleScale + screenCenter - Main.screenPosition;
                    if (cutoffArea.Contains((int)position.X, (int)position.Y))
                    {
                        Rectangle frame = rubbleTexture.Frame(3, 1, rubble[i].Variant, 0);
                        Main.spriteBatch.Draw(rubbleTexture, position, frame, Color.White * rubble[i].Opacity * intensity * 0.1f, 0f, frame.Size() * 0.5f, rubbleScale.X, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        public override float GetCloudAlpha() => 1f - Clamp(intensity, SkyIntensityOverride, 1f);

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || intensity > 0f;
        }
    }
}
