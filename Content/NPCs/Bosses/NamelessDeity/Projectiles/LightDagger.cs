using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class LightDagger : ModProjectile, IDrawAdditive, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public float DaggerAppearInterpolant => InverseLerp(TelegraphTime - 16f, TelegraphTime - 3f, Time);

        public Color GeneralColor => Color.Lerp(LocalScreenSplitSystem.UseCosmicEffect ? Color.Wheat : Color.IndianRed, Color.White, HueInterpolant * 0.66f) * Projectile.Opacity;

        public ref float Time => ref Projectile.localAI[0];

        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float HueInterpolant => ref Projectile.ai[1];

        public ref float Index => ref Projectile.ai[2];

        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public const string GrazeEchoFieldName = "GrazeEchoSoundDelay";

        public const int DefaultGrazeDelay = 150;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;

            if (Main.netMode != NetmodeID.Server)
                MyTexture = ModContent.Request<Texture2D>(Texture);

            NoxusPlayer.PostUpdateEvent += DecrementGrazeSoundDelay;
        }

        private void DecrementGrazeSoundDelay(NoxusPlayer p)
        {
            p.GetValueRef<int>(GrazeEchoFieldName).Value--;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;

            // Increased so that the graze checks are more precise.
            Projectile.MaxUpdates = 2;

            Projectile.timeLeft = Projectile.MaxUpdates * 120;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Time);

        public override void ReceiveExtraAI(BinaryReader reader) => Time = reader.ReadSingle();

        public override void AI()
        {
            // Sharply fade in.
            Projectile.Opacity = InverseLerp(0f, 12f, Time);

            // Decide rotation based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Accelerate after the telegraph dissipates.
            if (Time >= TelegraphTime)
            {
                float newSpeed = Clamp(Projectile.velocity.Length() + 5f / Projectile.MaxUpdates, 14f, 90f / Projectile.MaxUpdates);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;

                // Play a graze sound if a player was very, very close to being hit.
                int closestIndex = Player.FindClosest(Projectile.Center, 1, 1);
                Player closest = Main.player[closestIndex];
                float playerDirectionAngle = Projectile.velocity.AngleBetween(closest.DirectionToSafe(Projectile.Center));
                bool aimedTowardsClosest = playerDirectionAngle >= ToRadians(16f) && playerDirectionAngle < PiOver2;
                bool dangerouslyCloseToHit = Projectile.WithinRange(closest.Center, 57f) && aimedTowardsClosest;
                if (newSpeed >= 40f && dangerouslyCloseToHit && Main.myPlayer == closestIndex && closest.GetValueRef<int>(GrazeEchoFieldName) <= 0 && Projectile.Opacity >= 1f)
                {
                    if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState != NamelessDeityBoss.NamelessAIType.EnterPhase2)
                        StartShake(10f, Pi / 3f, Projectile.velocity, 0.15f);

                    SoundEngine.PlaySound(NamelessDeityBoss.RealityTearSound with { Volume = 0.14f });
                    SoundEngine.PlaySound(NamelessDeityBoss.GrazeSoundEcho with { Volume = 2.4f });
                    RadialScreenShoveSystem.Start(Projectile.Center, 24);
                    ScreenEffectSystem.SetChromaticAberrationEffect(Projectile.Center, 0.5f, 54);
                    ScreenEffectSystem.SetFlashEffect(Projectile.Center, 0.7f, DefaultGrazeDelay - 45);

                    closest.GetValueRef<int>(GrazeEchoFieldName).Value = DefaultGrazeDelay;
                }
            }

            // Play the ordinary graze slice sound.
            if (Time == TelegraphTime + 9f && Index == 0)
            {
                SoundEngine.PlaySound(NamelessDeityBoss.GrazeSound with { Volume = 0.6f, MaxInstances = 20 });
                StartShake(4f);
            }

            if (Projectile.IsFinalExtraUpdate())
                Time++;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            // Play a custom sound when hitting the player.
            modifiers.DisableSound();
            SoundEngine.PlaySound(MediumBloodSpillSound with { Volume = 0.6f }, target.Center);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            if (Time <= TelegraphTime)
                DrawTelegraph();

            // Draw bloom underneath the dagger. This is strongest when the blade itself has not yet fully faded in.
            float bloomOpacity = Lerp(0.75f, 0.51f, DaggerAppearInterpolant) * Projectile.Opacity;

            Color c1 = DialogColorRegistry.NamelessDeityTextColor;
            Color c2 = Color.Orange;
            if (LocalScreenSplitSystem.UseCosmicEffect)
            {
                c1 = Color.DeepSkyBlue;
                c2 = Color.White;
            }

            Color mainColor = Color.Lerp(c1, c2, Sin01(TwoPi * HueInterpolant + Main.GlobalTimeWrappedHourly * 2f + Pi * Projectile.Opacity) * 0.44f) * bloomOpacity;
            Color secondaryColor = Color.Lerp(c1, c2, Sin01(TwoPi * (1f - HueInterpolant) + Main.GlobalTimeWrappedHourly * 2f + Pi * Projectile.Opacity) * 0.44f) * bloomOpacity;

            Main.EntitySpriteDraw(BloomCircleSmall, Projectile.Center - Main.screenPosition, null, mainColor, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 1.32f, 0, 0);
            Main.EntitySpriteDraw(BloomCircleSmall, Projectile.Center - Main.screenPosition, null, secondaryColor, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 0.6f, 0, 0);

            // Make the dagger appear near the end of the telegraph fade-in.
            float daggerOffsetFactor = Projectile.velocity.Length() * 0.2f;
            Texture2D daggerTexture = MyTexture.Value;
            for (int i = 0; i < 30; i++)
            {
                float daggerScale = Lerp(1f, 0.48f, i / 29f) * Projectile.scale;
                Vector2 daggerDrawOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY) * DaggerAppearInterpolant * i * daggerScale * -daggerOffsetFactor;
                Color daggerDrawColor = c1 * DaggerAppearInterpolant * Pow(1f - i / 10f, 1.6f) * Projectile.Opacity * 1.8f;
                Main.EntitySpriteDraw(daggerTexture, Projectile.Center + daggerDrawOffset - Main.screenPosition, null, daggerDrawColor, Projectile.rotation, daggerTexture.Size() * 0.5f, daggerScale, 0, 0);
            }
        }

        public void DrawTelegraph()
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2100f;
            Main.spriteBatch.DrawBloomLine(start, end, GeneralColor * Sqrt(1f - DaggerAppearInterpolant), Projectile.Opacity * 40f);
        }

        public override bool? CanDamage() => Time >= TelegraphTime;

        public override bool ShouldUpdatePosition() => Time >= TelegraphTime;
    }
}
