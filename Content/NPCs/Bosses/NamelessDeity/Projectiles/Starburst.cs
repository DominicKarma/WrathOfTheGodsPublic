using System.Linq;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
	public class Starburst : ModProjectile, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<NamelessDeityBoss>
	{
		public bool Big => Projectile.ai[1] == 2f || BigAndHoming;

		public bool BigAndHoming => Projectile.ai[1] == 3f;

		public bool Redirect => Projectile.ai[1] == 1f || BigAndHoming;

		public bool GoingBackInTime => Projectile.ai[2] == 1f;

		public ref float Time => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 30;
		}

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.hostile = true;
			Projectile.timeLeft = 150;
			if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.SunBlenderBeams)
				Projectile.timeLeft = 300;

			// These should last as long as they need to during the clock attack, so that they can go back to their original position.
			if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.ClockConstellation)
				Projectile.timeLeft = 3600;
		}

		public override void AI()
		{
			// No Nameless Deity? Die.
			if (NamelessDeityBoss.Myself is null)
			{
				Projectile.Kill();
				return;
			}

			// Accelerate over time.
			float maxSpeed = 33f;
			if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.SunBlenderBeams)
				maxSpeed = 22.5f;
			if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.ClockConstellation)
				maxSpeed = 16.7f;

			if (!ClockConstellation.TimeIsStopped)
				Projectile.velocity = (Projectile.velocity * (Big ? 1.0284f : 1.04f)).ClampLength(0f, maxSpeed);

			// Keep the projectile in stasis if time is stopped.
			if (ClockConstellation.TimeIsStopped)
				Projectile.timeLeft++;

			// Release short-lived orange-red sparks.
			if (Main.rand.NextBool(12))
			{
				Color sparkColor = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.25f, 0.75f));
				sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

				Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
				spark.noLight = true;
				spark.color = sparkColor;
				spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
				spark.noGravity = spark.velocity.Length() >= 4.2f;
				spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
			}

			// Animate frames.
			Projectile.frameCounter++;
			Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

			if (Projectile.localAI[0] == 0f && Big)
			{
				Projectile.Size *= 1.8f;
				Projectile.scale *= 1.8f;
				Projectile.localAI[0] = 1f;
			}

			// Sharply redirect towards the closest player if this projectile is instructed to do so.
			if (Redirect && Time >= 65f)
			{
				float redirectAngularVelocity = ToRadians(8f);
				NPCAimedTarget target = NamelessDeityBoss.Myself.GetTargetData();
				Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
				Vector2 idealDirection = Projectile.SafeDirectionTo(target.Center);
				Projectile.velocity = currentDirection.ToRotation().AngleTowards(idealDirection.ToRotation(), redirectAngularVelocity).ToRotationVector2() * Projectile.velocity.Length();
				if (Projectile.velocity.Length() >= 21f)
					Projectile.velocity *= 0.95f;
			}

			// Die shortly after redirecting, assuming that behavior is in use.
			if (Redirect)
				Projectile.Opacity = InverseLerp(112f, 100f, Time);
			if (Redirect && Time >= 112f)
				Projectile.Kill();
			Time++;
		}

		public float FlameTrailWidthFunction(float completionRatio)
		{
			return SmoothStep(25f, 5f, completionRatio) * Projectile.Opacity;
		}

		public Color FlameTrailColorFunction(float completionRatio)
		{
			// Make the trail fade out at the end and fade in sharply at the start, to prevent the trail having a definitive, flat "start".
			float trailOpacity = InverseLerpBump(0f, 0.067f, 0.27f, 0.75f, completionRatio) * 0.9f;

			// Interpolate between a bunch of colors based on the completion ratio.
			Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
			Color middleColor = Color.Lerp(Color.OrangeRed, Color.Yellow, 0.4f);
			Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
			Color color = MulticolorLerp(completionRatio, startingColor, middleColor, endColor);
			if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.SunBlenderBeams)
				color = Color.Lerp(color, Color.Red, 0.7f);

			color *= trailOpacity;

			color.A = (byte)(trailOpacity * 255);
			return color * Projectile.Opacity;
		}

		public static void DrawStarburstBloomFlare(Projectile projectile, float opacityFactor = 1f)
		{
			if (NoxusBossConfig.Instance.PhotosensitivityMode)
				return;

			float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + projectile.identity;

			Color baseColor1 = ClockConstellation.TimeIsStopped ? Color.Turquoise : Color.Yellow;
			Color baseColor2 = ClockConstellation.TimeIsStopped ? Color.Cyan : Color.Lerp(Color.Red, Color.Wheat, Cos01(Main.GlobalTimeWrappedHourly * 3f + projectile.identity * 0.2f));

			// Make starbursts red during the time stop, to indicate that they're going to be shot back inward.
			float backglowOpacityFactor = opacityFactor;
			if (ClockConstellation.TimeIsStopped)
			{
				baseColor1 = Color.Red;
				baseColor2 = Color.Red;
				backglowOpacityFactor *= 0.25f;
			}
			else if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.ClockConstellation)
				backglowOpacityFactor *= 0.125f;
			else if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.SunBlenderBeams)
			{
				baseColor1 = Color.Red;
				baseColor2 = Color.Wheat;
			}

			// Draw the bloom flare.
			Color bloomFlareColor1 = baseColor1 with
			{
				A = 0
			} * projectile.Opacity * opacityFactor * 0.45f;
			Color bloomFlareColor2 = baseColor2 with
			{
				A = 0
			} * projectile.Opacity * opacityFactor * 0.45f;
			Vector2 bloomFlareDrawPosition = projectile.Center - Main.screenPosition;
			Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor1, bloomFlareRotation, BloomFlare.Size() * 0.5f, projectile.scale * 0.08f, 0, 0f);
			Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor2, -bloomFlareRotation, BloomFlare.Size() * 0.5f, projectile.scale * 0.096f, 0, 0f);

			// Draw the backglow.
			Main.spriteBatch.Draw(BloomCircleSmall, bloomFlareDrawPosition, null, Color.Red with
			{
				A = 0
			} * backglowOpacityFactor * 0.5f, 0f, BloomCircleSmall.Size() * 0.5f, projectile.scale * 0.3f, 0, 0f);
			Main.spriteBatch.Draw(BloomCircleSmall, bloomFlareDrawPosition, null, Color.Wheat with
			{
				A = 0
			} * backglowOpacityFactor * 0.4f, 0f, BloomCircleSmall.Size() * 0.5f, projectile.scale * 0.8f, 0, 0f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (NamelessDeityBoss.Myself is null)
				return false;

			// Draw a bloom flare behind the starburst.
			DrawStarburstBloomFlare(Projectile);

			// Draw afterimages that trail closely behind the star core.
			int afterimageCount = Big ? 16 : 5;
			float minScale = 0.33f;
			float positionClumpInterpolant = 0.7f;
			Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
			Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame % 5);
			float scaleFactor = NamelessDeityBoss.Myself.As<NamelessDeityBoss>().TeleportVisualsAdjustedScale.Length() * 0.707f;
			float againstNamelessInterpolant = InverseLerp(600f, 450f, Projectile.Distance(NamelessDeityBoss.Myself.Center) / scaleFactor) * NamelessDeityBoss.Myself.Opacity;

			for (int i = afterimageCount - 1; i >= 0; i--)
			{
				int afterimageIndex = i;
				Vector2 drawOffset = Vector2.Zero;
				if (GoingBackInTime)
				{
					afterimageIndex = afterimageCount - 1 - i;
					drawOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY) * (Projectile.oldPos[afterimageCount / 2 + 1] - Projectile.position).Length() * 0.5f;
				}

				float afterimageRot = Projectile.oldRot[afterimageIndex];
				float scale = Projectile.scale * Lerp(1f, minScale, 1f - (afterimageCount - afterimageIndex) / (float)afterimageCount);
				SpriteEffects sfxForThisAfterimage = Projectile.oldSpriteDirection[afterimageIndex] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

				// Fix visibility problems near Nameless. By default starbursts are a bright, shining white.
				// However, this very easily blends in with Nameless' colors and can cause problems telling whether there are, particularly during the sun blenders attack.
				// As a result, they become more orange-ish yellow as they get close to Nameless.
				Color drawColor = Color.Lerp(Color.White with { A = 0 }, Color.Wheat with { A = 10 }, againstNamelessInterpolant);

				Vector2 drawPos = Vector2.Lerp(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Center, positionClumpInterpolant) - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY + drawOffset;
				Color color = Projectile.GetAlpha(drawColor) * ((afterimageCount - afterimageIndex) / (float)afterimageCount);
				Main.spriteBatch.Draw(texture, drawPos, frame, color, afterimageRot, frame.Size() * 0.5f, scale, sfxForThisAfterimage, 0f);
			}

			return false;
		}

		public override bool ShouldUpdatePosition() => !ClockConstellation.TimeIsStopped;

		public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
		{
			if (Big)
				return;

			ManagedShader shader = ShaderManager.GetShader("NoxusBoss.GenericFlameTrail");
			shader.SetTexture(StreakBloomLine, 1, SamplerState.LinearWrap);

			PrimitiveSettings settings = new(FlameTrailWidthFunction, FlameTrailColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader);
			PrimitiveRenderer.RenderTrail(Projectile.oldPos.Take(6).ToArray(), settings, 5);
		}
	}
}
