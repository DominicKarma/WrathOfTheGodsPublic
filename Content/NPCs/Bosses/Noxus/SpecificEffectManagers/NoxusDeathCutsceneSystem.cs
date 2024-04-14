using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles.Metaballs;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.NamelessDeityBoss;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusDeathCutsceneSystem : ModSystem
    {
        private LoopedSoundInstance screamLoopSound;

        public static int AnimationTimer
        {
            get;
            set;
        }

        public static int WhiteFadeinTime => 30;

        public static int EyeAppearDelay => 60;

        public static int EyeAppearTime => 30;

        public static int HandsAppearDelay => 90;

        public static int HandsAppearTime => 60;

        public static int SlashDelay => 150;

        public static int SlashDuration => 180;

        public static int NoxusPostSlashScreamTime => 120;

        public static int ExplosionTime => 25;

        public static int AnimationDuration => WhiteFadeinTime + EyeAppearDelay + EyeAppearTime + HandsAppearDelay + HandsAppearTime + SlashDelay + SlashDuration + NoxusPostSlashScreamTime + ExplosionTime;

        public static int SlashTimer => AnimationTimer - WhiteFadeinTime - EyeAppearDelay - EyeAppearTime - HandsAppearDelay - SlashDelay;

        public static float EyeAppearInterpolant => Sqrt(InverseLerp(0f, EyeAppearTime, AnimationTimer - WhiteFadeinTime - EyeAppearDelay));

        public static float OverlayInterpolant => InverseLerp(0f, WhiteFadeinTime, AnimationTimer) * InverseLerp(-1f, -10f, AnimationTimer - AnimationDuration);

        public static Asset<Texture2D> EyeAsset
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            On_FilterManager.EndCapture += DrawSilhouettesAndScene;

            if (Main.netMode == NetmodeID.Server)
                return;

            EyeAsset = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/SpecificEffectManagers/NamelessDeityEyeSilhoette");
        }

        private void DrawSilhouettesAndScene(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            // Do nothing special if Noxus is not present or the scene is not ongoing.
            if (EntropicGod.Myself is null || AnimationTimer <= 0)
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                return;
            }

            // Draw the screen overlay.
            Main.spriteBatch.ResetToDefault(false);
            bool namelessDeityIsVisible = SlashTimer < SlashDuration;
            float generalOpacity = 1f - InverseLerpBump(-8f, 0f, SlashDuration, SlashDuration + 8f, SlashTimer);
            Color screenOverlayColor = Color.Lerp(Color.White, Color.Black, 1f - generalOpacity) * OverlayInterpolant;
            Vector2 screenArea = Main.ScreenSize.ToVector2();
            Main.spriteBatch.Draw(WhitePixel, screenArea * 0.5f, null, screenOverlayColor, 0f, WhitePixel.Size() * 0.5f, screenArea * 2f, 0, 0f);

            // Draw a Nameless Deity hand pulling Noxus out of the portal if necessary.
            if (EntropicGod.Myself is not null && namelessDeityIsVisible)
                EntropicGod.Myself.As<EntropicGod>().DrawYankHand(EntropicGod.Myself.Center - Main.screenPosition, Color.DarkGray * generalOpacity, EntropicGod.Myself.rotation);

            // Reset the spritebatch for shader operations.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw Noxus' metaballs.
            ModContent.GetInstance<NoxusGasMetaball>().RenderLayerWithShader();

            // Apply the black silhoette shader.
            var blackShader = ShaderManager.GetShader("NoxusBoss.BlackShader");
            blackShader.Apply();

            // Draw Nameless' eye with the silhoette.
            int handTimer = AnimationTimer - WhiteFadeinTime - EyeAppearDelay - EyeAppearTime - HandsAppearDelay;
            if (EyeAppearInterpolant > 0f && namelessDeityIsVisible)
            {
                Texture2D eyeTexture = EyeAsset.Value;
                Vector2 eyeDrawPosition = EntropicGod.Myself.Center - Vector2.UnitY * Lerp(450f, 400f, EyeAppearInterpolant) - Main.screenPosition;

                float cartoonPopout = EasingCurves.Elastic.Evaluate(EasingType.InOut, EyeAppearInterpolant);

                Vector2 baseScale = Vector2.One * Pow(cartoonPopout, 5f) + Vector2.One * Clamp(handTimer * 0.004f, 0f, 0.7f);
                Main.spriteBatch.Draw(eyeTexture, eyeDrawPosition, null, Color.White * Pow(EyeAppearInterpolant, 3f) * generalOpacity, 0f, eyeTexture.Size() * 0.5f, baseScale * 0.6f, 0, 0f);
            }

            // Draw Noxus with the silhouette.
            if (EntropicGod.Myself is not null)
            {
                float explodeInterpolant = InverseLerp(8f, 0f, handTimer - SlashDelay - SlashDuration - NoxusPostSlashScreamTime);
                EntropicGod.Myself.Opacity = generalOpacity * Sqrt(explodeInterpolant);
                EntropicGod.Myself.scale = 1f + (1f - explodeInterpolant) * 3f;
                EntropicGod.Myself.As<EntropicGod>().BigEyeOpacity = 0f;
                EntropicGod.Myself.As<EntropicGod>().DrawBack(EntropicGod.Myself.Center - Main.screenPosition, Color.White * EntropicGod.Myself.Opacity, EntropicGod.Myself.rotation);
                Main.instance.DrawNPC(EntropicGod.Myself.whoAmI, false);
            }

            // Draw hands.
            if (handTimer >= 1 && namelessDeityIsVisible)
            {
                float creepInterpolant = InverseLerp(22f, SlashDelay + 30f, handTimer);
                float handAppearInterpolant = InverseLerp(1f, 22f, handTimer);
                float handDistanceFactor = Lerp(2.3f, 1f, Pow(handAppearInterpolant, 0.75f)) - creepInterpolant * 0.25f;
                float handScale = (creepInterpolant * 0.56f + handAppearInterpolant) * 0.5f;
                Texture2D handTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/Hand1").Value;
                Rectangle handFrame = handTexture.Frame();
                Vector2 leftHandDrawPosition = EntropicGod.Myself.Center + 0.91f.ToRotationVector2() * handDistanceFactor * new Vector2(-600f, -200f) - Main.screenPosition;
                Vector2 rightHandDrawPosition = EntropicGod.Myself.Center + 0.91f.ToRotationVector2() * handDistanceFactor * new Vector2(600f, -200f) - Main.screenPosition;

                Main.spriteBatch.Draw(handTexture, leftHandDrawPosition, handFrame, Color.White * generalOpacity, 0.37f, handFrame.Size() * 0.5f, handScale, SpriteEffects.FlipVertically, 0f);
                Main.spriteBatch.Draw(handTexture, rightHandDrawPosition, handFrame, Color.White * generalOpacity, -0.37f, handFrame.Size() * 0.5f, handScale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f);
            }

            // End the sprite batch.
            Main.spriteBatch.End();

            // Explode.
            if (handTimer == SlashDelay + SlashDuration + NoxusPostSlashScreamTime + 1f)
                EntropicGod.Myself.As<EntropicGod>().Explode();

            // Ensure that typical screen shader related operations occur.
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        public static void Start()
        {
            // Start the animation timer.
            AnimationTimer = 1;

            // Disable the player's inputs and UI.
            BlockerSystem.Start(true, true, () => AnimationTimer >= 1);
        }

        public override void PostUpdateProjectiles()
        {
            if (AnimationTimer <= 0 || EntropicGod.Myself is null)
            {
                AnimationTimer = 0;
                return;
            }

            // Make the animation progress.
            AnimationTimer++;
            if (AnimationTimer >= AnimationDuration)
                AnimationTimer = 0;

            // Handle slash behaviors.
            if (SlashTimer >= 1 && SlashTimer < SlashDuration)
            {
                // Start Nameless' scream sound.
                if (SlashTimer == 1)
                {
                    screamLoopSound?.Stop();
                    screamLoopSound = LoopedSoundManager.CreateNew(ScreamSoundLooped_Start, ScreamSoundLooped, () =>
                    {
                        return SlashTimer <= 0 || SlashTimer >= SlashDuration;
                    });
                }

                // Update Nameless' scream sound.
                screamLoopSound?.Update(EntropicGod.Myself.Center);

                // Play slash sounds.
                if (AnimationTimer % 4 == 0)
                    SoundEngine.PlaySound(SliceSound with { Volume = 0.4f, MaxInstances = 400 });

                // Play scream sounds from Noxus.
                if (AnimationTimer % 10 == 0)
                    SoundEngine.PlaySound(EntropicGod.ScreamSound with { Volume = 0.9f, Pitch = -0.15f });

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(new EntitySource_WorldEvent(), EntropicGod.Myself.Center + Main.rand.NextVector2Circular(100f, 60f), Vector2.Zero, ModContent.ProjectileType<LightSlash>(), 0, 0f, -1, Main.rand.NextFloat(TwoPi));
            }

            if (SlashTimer >= SlashDuration)
            {
                // Play scream sounds from Noxus.
                if (SlashTimer <= SlashDuration + NoxusPostSlashScreamTime)
                {
                    if (AnimationTimer % 13 == 0 && SlashTimer <= SlashDuration + NoxusPostSlashScreamTime - 30f)
                        SoundEngine.PlaySound(EntropicGod.ScreamSound with { Volume = 0.9f, Pitch = -0.25f });
                }

                for (int i = 0; i < 8; i++)
                {
                    Vector2 gasSpawnPosition = EntropicGod.Myself.Center + Main.rand.NextVector2Circular(82f, 82f);
                    float gasSize = EntropicGod.Myself.width * EntropicGod.Myself.Opacity * Main.rand.NextFloat(0.4f, 0.96f);
                    ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(gasSpawnPosition, Main.rand.NextVector2Circular(19f, 19f), gasSize);
                }
            }
        }
    }
}
