using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class InterfacedProjectileDrawSystem : ModSystem
    {
        private static IEnumerable<IDrawsWithShader> projectileShaderDrawers
        {
            get
            {
                return Main.projectile.Take(Main.maxProjectiles).Where(p =>
                {
                    return p.active && p.ModProjectile is IDrawsWithShader drawer && !p.IsOffscreen();
                }).Select(p => p.ModProjectile as IDrawsWithShader);
            }
        }

        private static IEnumerable<IDrawsWithShader> npcShaderDrawers
        {
            get
            {
                return Main.npc.Take(Main.maxNPCs).Where(n =>
                {
                    return n.active && n.ModNPC is IDrawsWithShader drawer;
                }).Select(n => n.ModNPC as IDrawsWithShader);
            }
        }

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            On_Main.DrawProjectiles += DrawInterfaceProjectiles;
        }

        private void DrawInterfaceProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            // Call the base DrawProjectiles method.
            orig(self);

            // Collect all entities with the IDrawsWithShader and IDrawAdditive interface.
            List<IDrawsWithShader> shaderDrawers = projectileShaderDrawers.ToList();
            shaderDrawers.AddRange(npcShaderDrawers);
            shaderDrawers.OrderBy(i => i.LayeringPriority).ToList();

            List<IDrawAdditive> additiveDrawers = Main.projectile.Take(Main.maxProjectiles).Where(p =>
            {
                return p.active && p.ModProjectile is IDrawAdditive drawer && !p.IsOffscreen();
            }).Select(p => p.ModProjectile as IDrawAdditive).ToList();

            // Use screen culling for optimization reasons.
            Main.instance.GraphicsDevice.ScissorRectangle = new(-5, -5, Main.screenWidth + 10, Main.screenHeight + 10);
            if (shaderDrawers.Any())
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, DefaultRasterizerScreenCull, null, Main.GameViewMatrix.TransformationMatrix);
                DrawShaderEntities(shaderDrawers);
                Main.spriteBatch.End();
            }

            if (additiveDrawers.Any())
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, DefaultRasterizerScreenCull, null, Main.GameViewMatrix.TransformationMatrix);
                DrawAdditiveProjectiles(additiveDrawers);
                Main.spriteBatch.End();
            }
        }

        public static void DrawShaderEntities(List<IDrawsWithShader> orderedDrawers)
        {
            // Draw all projectiles that have the shader interface.
            foreach (var drawer in orderedDrawers.Where(d => !d.ShaderShouldDrawAdditively))
                drawer.DrawWithShader(Main.spriteBatch);

            // Check for shader projectiles marked with the additive bool.
            var additiveDrawers = orderedDrawers.Where(d => d.ShaderShouldDrawAdditively);
            if (additiveDrawers.Any())
            {
                Main.spriteBatch.PrepareForShaders(BlendState.Additive);
                foreach (var drawer in additiveDrawers)
                    drawer.DrawWithShader(Main.spriteBatch);
            }
        }

        public static void DrawAdditiveProjectiles(List<IDrawAdditive> orderedDrawers)
        {
            // Draw all projectiles that have the additive interface.
            foreach (var drawer in orderedDrawers)
                drawer.DrawAdditive(Main.spriteBatch);
        }
    }
}
