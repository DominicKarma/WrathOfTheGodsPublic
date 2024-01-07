using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.Graphics.Particles
{
    public class ParticleManager : ModSystem
    {
        internal static readonly List<Particle> activeParticles = new();

        internal static readonly Dictionary<Type, int> particleIDLookup = new();

        // This acts as a central storage for particle textures, so that each one does not require a ModContent.Request call.
        internal static readonly Dictionary<int, Texture2D> particleTextureLookup = new();

        public override void OnModLoad()
        {
            // Don't attempt to load particle information on the server, as particles are purely graphical objects.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Load all particles.
            LoadParticles(Mod.Code);

            // Prepare the draw detour.
            On_Main.DrawInfernoRings += DrawParticlesDetour;
        }

        private void DrawParticlesDetour(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            DrawParticles();
            orig(self);
        }

        private static void DrawParticles()
        {
            // Do nothing if there are no particles to draw.
            if (!activeParticles.Any())
                return;

            // Flush draws from the original detour this is called from.
            Main.spriteBatch.End();

            // Group particles via their blend state. This will determine how they are drawn.
            var blendGroups = activeParticles.GroupBy(p => p.DrawBlendState);
            foreach (var blendGroup in blendGroups)
            {
                // Prepare a rasterizer for screen culling, to keep drawing optimized.
                RasterizerState screenCull = PrepareScreenCullRasterizer();

                // Prepare the drawing with the specified blend state.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendGroup.First().DrawBlendState, Main.DefaultSamplerState, DepthStencilState.None, screenCull, null, Main.GameViewMatrix.TransformationMatrix);

                // Draw all particles with the desired blend state.
                foreach (Particle p in blendGroup)
                    p.Draw();

                Main.spriteBatch.End();
            }

            // Return to regular drawing.
            Main.spriteBatch.ResetToDefault(false);
        }

        public override void PostUpdateDusts()
        {
            // Perform automatic particle cleanup.
            activeParticles.RemoveAll(p => p.Time >= p.Lifetime);

            // Update all particles.
            int particleCount = activeParticles.Count;
            for (int i = 0; i < particleCount; i++)
            {
                activeParticles[i].Time++;
                activeParticles[i].Update();
                activeParticles[i].Position += activeParticles[i].Velocity;
            }
        }

        public static void LoadParticles(Assembly assembly)
        {
            int currentParticleID = 0;
            if (particleIDLookup?.Any() ?? false)
                currentParticleID = particleIDLookup.Values.Max() + 1;

            foreach (Type particleType in AssemblyManager.GetLoadableTypes(assembly))
            {
                // Don't attempt to load abstract types.
                if (!particleType.IsSubclassOf(typeof(Particle)) || particleType.IsAbstract)
                    continue;

                Particle particle = (Particle)FormatterServices.GetUninitializedObject(particleType);

                // Store an ID for the particle. All particles of this type that are spawned will copy the ID.
                particleIDLookup[particleType] = currentParticleID;

                // Store the particle's texture in the lookup table.
                Texture2D particleTexture = ModContent.Request<Texture2D>(particle.TexturePath, AssetRequestMode.ImmediateLoad).Value;
                particleTextureLookup[currentParticleID] = particleTexture;

                // Perform particle-specific loading.
                particle.Load();

                // Increment the particle ID.
                currentParticleID++;
            }
        }
    }
}
