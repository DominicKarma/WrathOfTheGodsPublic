using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.SpecialWorldEvents;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Enemies.NoxusWorld.Mirrorwalkers
{
    public class Mirrorwalker : ModNPC
    {
        #region Initialization

        public Player Target => Main.player[NPC.target];

        public ref float MirrorX => ref NPC.ai[0];

        public ref float Time => ref NPC.ai[1];

        public ref float HorrorInterpolant => ref NPC.ai[2];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            this.ExcludeFromBestiary();
            NPCID.Sets.MustAlwaysDraw[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 30f;
            NPC.damage = 0;
            NPC.width = 26;
            NPC.height = 48;
            NPC.defense = 8;
            NPC.lifeMax = 1700;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.Opacity = 0f;
            NPC.ShowNameOnHover = false;
            NPC.dontTakeDamage = true;
            NPC.hide = true;
        }

        #endregion Initialization

        #region AI
        public override void AI()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // Ensure that the player's shader drawer is in use so that the render target contents can be used by this NPC.
                LocalPlayerDrawManager.StopCondition = () => !NPC.active;
                LocalPlayerDrawManager.ShaderDrawAction = () => { };
            }

            // Disable natural despawning.
            NPC.timeLeft = 7200;

            // Define the mirror position at first.
            // This is where the clone will be drawn relative to.
            if (MirrorX == 0f)
            {
                NPC.TargetClosest();
                MirrorX = Target.Center.X + Target.direction * Main.rand.NextFloat(960f, 1100f);
                NPC.Center = new Vector2(MirrorX + Target.direction * 10f, Target.Center.Y);
                NPC.netUpdate = true;
            }

            // Define position and opacity relative to the mirror.
            float distanceFromMirror = Distance(MirrorX, Target.Center.X);
            float mirrorSign = (NPC.Center.X - MirrorX).NonZeroSign();
            HorrorInterpolant = InverseLerp(50f, 16f, distanceFromMirror);
            NPC.Opacity = InverseLerp(485f, 200f, distanceFromMirror);
            NPC.Center = new Vector2(MirrorX + mirrorSign * distanceFromMirror, Target.Center.Y);
            NPC.gfxOffY = 0f;

            bool inTiles = Collision.SolidCollision(NPC.Center, 4, Target.height / 2);
            if (inTiles)
            {
                while (Collision.SolidCollision(NPC.Center, 4, Target.height / 2))
                    NPC.position.Y -= 2f;
            }
            else
            {
                float oldY = NPC.position.Y;
                while (!Collision.SolidCollision(NPC.Center + Vector2.UnitY * (Target.height / 2 - 2f), 4, 2))
                    NPC.position.Y += 2f;

                if (Distance(NPC.position.Y, oldY) >= 24f)
                    NPC.position.Y = oldY;
                else
                {
                    float _ = 1f;
                    Collision.StepDown(ref NPC.position, ref NPC.velocity, Target.width, Target.head, ref _, ref NPC.gfxOffY);
                }
            }

            // Check if the Mirrorwalker is incredibly close to the player it's copying.
            // If it is, disappear and disorient the player.
            if (distanceFromMirror <= 6f)
            {
                SoundEngine.PlaySound(GlitchSound);
                SoundEngine.PlaySound(NoxusFogEventManager.FogAmbienceSound);
                SoundMufflingSystem.MuffleFactor = 0.1f;
                MusicVolumeManipulationSystem.MuffleFactor = 0.1f;

                TotalScreenOverlaySystem.OverlayInterpolant = 2.3f;
                TotalScreenOverlaySystem.OverlayColor = Color.Black;

                // Manipulate time events in single player as an implication that the player went unconscious.
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    int unconsciousTime = Main.rand.Next(7200, 27000);
                    NoxusFogEventManager.IncrementTime(unconsciousTime / 5);

                    // Stop all target movement.
                    Target.velocity.X = 0f;

                    int dayCycleLength = (int)(Main.dayTime ? Main.dayLength : Main.nightLength);
                    int maxTimeStep = (int)Clamp(unconsciousTime, 0f, dayCycleLength - (int)Main.time);
                    Main.time += unconsciousTime;
                    if (Main.time > dayCycleLength - 1f)
                        Main.time = dayCycleLength - 1f;
                }

                StartShake(12f);
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 4f, 240);
                SoundEngine.PlaySound(EarRingingSound);
                NPC.active = false;
                return;
            }

            // Prevent the player from going super fast when near the mirror.
            float maxHorizontalPlayerSpeed = Utils.Remap(distanceFromMirror, 600f, 150f, 11f, 0.5f);
            Target.velocity.X = Clamp(Target.velocity.X, -maxHorizontalPlayerSpeed, maxHorizontalPlayerSpeed);

            // Make sounds and music quieter the close the player is to the mirror.
            MusicVolumeManipulationSystem.MuffleFactor = Utils.Remap(distanceFromMirror, 540f, 100f, 1f, 0.01f);
            SoundMufflingSystem.MuffleFactor = Utils.Remap(distanceFromMirror, 560f, 120f, 1f, 0f);

            Time++;
        }

        #endregion AI

        #region Drawing

        public override void DrawBehind(int index)
        {
            // Ensure that player draw code is done before this NPC is drawn, so that using the player target doesn't cause one-frame disparities.
            Main.instance.DrawCacheNPCsOverPlayers.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Matrix transformation = Main.GameViewMatrix.TransformationMatrix;
            Vector2 scale = Vector2.One / new Vector2(transformation.M11, transformation.M22);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, transformation);

            // The render target should always be inverted. Player draw information is already embedded into the target regardless, so FlipHorizontally means that everything
            // is reversed.
            SpriteEffects direction = SpriteEffects.FlipHorizontally;
            Vector2 drawPosition = NPC.Center - screenPos - Vector2.UnitY * Main.LocalPlayer.gfxOffY + Vector2.UnitY * NPC.gfxOffY;

            // Ensure that rotation is inverted.
            float rotation = -Main.LocalPlayer.fullRotation;

            // Apply the horror shader.
            var horrorShader = ShaderManager.GetShader("NoxusBoss.MirrorwalkerHorrorShader");
            horrorShader.TrySetParameter("contrastMatrix", HighContrastScreenShakeShaderData.CalculateContrastMatrix(NPC.Opacity * 2f));
            horrorShader.TrySetParameter("noiseOffsetFactor", Lerp(6f, 1f, NPC.Opacity) * HorrorInterpolant);
            horrorShader.TrySetParameter("noiseOverlayColor", Color.MediumPurple);
            horrorShader.TrySetParameter("noiseOverlayIntensityFactor", 31f);
            horrorShader.TrySetParameter("eyeColor", Color.Lerp(Color.Cyan, Color.Wheat, 0.7f));
            horrorShader.TrySetParameter("zoom", scale);
            horrorShader.TrySetParameter("horrorInterpolant", Pow(HorrorInterpolant, 2f));
            horrorShader.SetTexture(WavyBlotchNoise, 1, SamplerState.LinearWrap);
            horrorShader.Apply();

            // Draw the player's target.
            float pulse = Main.GlobalTimeWrappedHourly * 2.1f % 1f * Pow(HorrorInterpolant, 1.7f);
            Texture2D texture = LocalPlayerDrawManager.PlayerTarget;
            spriteBatch.Draw(texture, drawPosition, null, Color.White * NPC.Opacity, rotation, texture.Size() * 0.5f, scale, direction, 0f);
            spriteBatch.Draw(texture, drawPosition, null, Color.White * NPC.Opacity * Pow(1f - pulse, 2f), rotation, texture.Size() * 0.5f, scale * (1f + pulse * 0.9f), direction, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }
        #endregion Drawing
    }
}
