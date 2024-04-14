using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Evaluates the underlying size of a texture-based <see cref="LazyAsset{T}"/>.
        /// </summary>
        /// <param name="texture">The texture to evaluate the size of.</param>
        public static Vector2 Size(this LazyAsset<Texture2D> texture) => texture.Value.Size();
    }
}
