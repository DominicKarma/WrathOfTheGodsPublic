using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public class NamelessDeitySwappableTexture
    {
        /// <summary>
        /// How long the texture has to wait until swaps can be performed again.
        /// </summary>
        public int SwapBlockCountdown;

        /// <summary>
        /// The currently used texture variant index.
        /// </summary>
        public int TextureVariant;

        /// <summary>
        /// An optional list of specially allowed variants. Used primarily for the purpose of establishing restrictions on what textures can be used via presets.
        /// </summary>
        public int[] SpeciallyAllowedVariants;

        /// <summary>
        /// A simple event that is fired whenever a texture swap happens.
        /// </summary>
        public event Action OnSwap;

        /// <summary>
        /// An automatic condition that when true causes a texture swap to happen. Check via <see cref="Update"/>.
        /// </summary>
        public Func<bool> SwapRule;

        /// <summary>
        /// A list of all possible textures that can be used.
        /// </summary>
        public Asset<Texture2D>[] PossibleTextures;

        /// <summary>
        /// The amount of possible variants this swappable texture can choose from.
        /// </summary>
        public int PossibleVariants => SpeciallyAllowedVariants?.Length ?? PossibleTextures.Length;

        /// <summary>
        /// The currently used texture.
        /// </summary>
        public Texture2D UsedTexture => PossibleTextures[TextureVariant].Value;

        public NamelessDeitySwappableTexture(string partPrefix, int totalVariants, int[] speciallyAllowedVariants = null)
        {
            PossibleTextures = new Asset<Texture2D>[totalVariants];
            TextureVariant = Main.rand.Next(totalVariants);
            SpeciallyAllowedVariants = speciallyAllowedVariants;
            for (int i = 0; i < totalVariants; i++)
                PossibleTextures[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/{partPrefix}{i + 1}");
        }

        /// <summary>
        /// Assigns an automatic swap rule to this texture set.
        /// </summary>
        /// <param name="rule">The swap rule.</param>
        public NamelessDeitySwappableTexture WithAutomaticSwapRule(Func<bool> rule)
        {
            SwapRule = rule;
            return this;
        }

        /// <summary>
        /// Temporarily forces this texture to a specific variant.
        /// </summary>
        public void ForceToVariant(int variant)
        {
            TextureVariant = variant;
            SwapBlockCountdown = 20;
        }

        /// <summary>
        /// Checks if a swap should happen in accordance with the <see cref="SwapRule"/>, assuming one exists.
        /// </summary>
        public void Update()
        {
            if (SwapBlockCountdown >= 1)
                SwapBlockCountdown--;
            else if (SwapRule?.Invoke() ?? false)
                Swap();
        }

        /// <summary>
        /// Selects a new texture variant, ensuring that a different selection is made. This does not do anything on servers.
        /// </summary>
        public void Swap()
        {
            if (Main.netMode == NetmodeID.Server || SwapBlockCountdown >= 1)
                return;

            if (PossibleVariants >= 2)
            {
                int originalVariant = TextureVariant;
                do
                {
                    TextureVariant = Main.rand.Next(PossibleTextures.Length);
                }
                while (TextureVariant == originalVariant || (!(SpeciallyAllowedVariants?.Contains(TextureVariant) ?? true)));

                // Call the OnSwap event.
                OnSwap?.Invoke();
            }

            if (SpeciallyAllowedVariants is not null && SpeciallyAllowedVariants.Length == 1)
                TextureVariant = SpeciallyAllowedVariants[0];
        }
    }
}
