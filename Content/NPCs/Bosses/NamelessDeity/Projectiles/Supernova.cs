using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class Supernova : ModProjectile, IProjOwnedByBoss<NamelessDeityBoss>, IDrawsWithShader
    {
        public ref float Time => ref Projectile.ai[0];

        public static int Lifetime => SecondsToFrames(8f);

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 25000;

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
            Projectile.hide = true;

            // This technically screws up the width/height values but that doesn't really matter since the supernova itself isn't meant to do damage.
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // No Nameless Deity? Die.
            if (NamelessDeityBoss.Myself is null)
                Projectile.Kill();

            // Grow over time.
            Projectile.scale += Remap(Projectile.scale, 1f, 28f, 0.45f, 0.08f);
            if (Projectile.scale >= 32f)
                Projectile.scale = 32f;

            // Periodically release light bursts.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 20f == 5f && Time <= 90f)
                NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

            // Dissipate at the end.
            Projectile.Opacity = InverseLerp(8f, 120f, Projectile.timeLeft);

            Time++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            var supernovaShader = ShaderManager.GetShader("SupernovaShader");
            supernovaShader.TrySetParameter("supernovaColor1", Color.Orange.ToVector3());
            supernovaShader.TrySetParameter("supernovaColor2", Color.Red.ToVector3());
            supernovaShader.TrySetParameter("generalOpacity", Projectile.Opacity);
            supernovaShader.TrySetParameter("scale", Projectile.scale);
            supernovaShader.TrySetParameter("brightness", InverseLerp(20f, 4f, Projectile.scale) * 2f + 1.25f);
            supernovaShader.SetTexture(WavyBlotchNoise, 1);
            supernovaShader.SetTexture(DendriticNoiseZoomedOut, 2);
            supernovaShader.SetTexture(VoidTexture, 3);
            supernovaShader.Apply();

            spriteBatch.Draw(InvisiblePixel, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity * 0.42f, Projectile.rotation, InvisiblePixel.Size() * 0.5f, Projectile.scale * 400f, 0, 0f);
        }
    }
}
