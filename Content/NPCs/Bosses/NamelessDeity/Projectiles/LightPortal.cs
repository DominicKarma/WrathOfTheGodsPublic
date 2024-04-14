using Luminance.Assets;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
	public class LightPortal : ModProjectile, IDrawsWithShader, IProjOwnedByBoss<NamelessDeityBoss>
	{
		public static LazyAsset<Texture2D> MyTexture
		{
			get;
			private set;
		}

		public float MaxScale => Projectile.ai[0];

		public ref float Time => ref Projectile.localAI[0];

		public ref float Lifetime => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			if (Main.netMode != NetmodeID.Server)
				MyTexture = LazyAsset<Texture2D>.Request(Texture);
		}

		public override void SetDefaults()
		{
			Projectile.width = 600;
			Projectile.height = 600;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 9600;
			Projectile.hide = true;
			CooldownSlot = ImmunityCooldownID.Bosses;
		}

		public override void AI()
		{
			Time++;

			// Decide the current scale.
			Projectile.scale = InverseLerpBump(0f, 15f, Lifetime - 16f, Lifetime, Time);
			Projectile.Opacity = Pow(Projectile.scale, 2.6f);
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Time >= Lifetime)
			{
				SoundEngine.PlaySound(TwinkleSound with { Volume = 0.3f, MaxInstances = 20 }, Projectile.Center);
				TwinkleParticle twinkle = new(Projectile.Center, Vector2.Zero, Color.LightCyan, 30, 6, Vector2.One * MaxScale * 1.3f);
				twinkle.Spawn();
				Projectile.Kill();
			}
		}

		public void DrawWithShader(SpriteBatch spriteBatch)
		{
			var portalShader = ShaderManager.GetShader("NoxusBoss.PortalShader");
			portalShader.TrySetParameter("generalColor", Color.White.ToVector3());
			portalShader.TrySetParameter("circleStretchInterpolant", Projectile.scale);
			portalShader.TrySetParameter("transformation", Matrix.CreateScale(3f, 1f, 1f));
			portalShader.TrySetParameter("aimDirection", Projectile.velocity);
			portalShader.TrySetParameter("edgeFadeInSharpness", 20.3f);
			portalShader.TrySetParameter("aheadCircleMoveBackFactor", 0.67f);
			portalShader.TrySetParameter("aheadCircleZoomFactor", 0.9f);
			portalShader.TrySetParameter("spaceBrightness", InverseLerp(0.7f, 0.82f, ScreenEffectSystem.FlashIntensity) * 3.6f + 1.5f);
			portalShader.TrySetParameter("spaceTextureZoom", Vector2.One * 0.55f);
			portalShader.TrySetParameter("spaceTextureOffset", Vector2.UnitX * Projectile.identity * 0.156f);
			portalShader.TrySetParameter("distanceIrregularity", 0f);
			portalShader.SetTexture(LemniscateDistanceLookup, 1);
			portalShader.SetTexture(TurbulentNoise, 2);
			portalShader.SetTexture(MyTexture.Value, 3);
			portalShader.SetTexture(PerlinNoise, 4);
			portalShader.SetTexture(SpikesTexture, 5);
			portalShader.Apply();

			Vector2 textureArea = Projectile.Size / WhitePixel.Size() * MaxScale;
			textureArea *= 1f + Cos(Main.GlobalTimeWrappedHourly * 15f + Projectile.identity) * 0.013f;
			spriteBatch.Draw(WhitePixel, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.IndianRed), Projectile.rotation, WhitePixel.Size() * 0.5f, textureArea, 0, 0f);
		}

		public override bool ShouldUpdatePosition() => false;
	}
}
