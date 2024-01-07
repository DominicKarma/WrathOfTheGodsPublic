using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Shaders
{
    [DebuggerDisplay("Shader - '{Name}'")]
    public sealed class ManagedShader
    {
        private readonly Dictionary<string, object> parameterCache;

        public readonly Ref<Effect> Shader;

        public readonly string Name;

        public const string TextureSizeParameterPrefix = "textureSize";

        public const string DefaultPassName = "AutoloadPass";

        internal ManagedShader(string name, Ref<Effect> shader)
        {
            Name = name;
            Shader = shader;

            // Initialize the parameter cache.
            parameterCache = new();
        }

        internal bool ParameterIsCachedAsValue(string parameterName, object value)
        {
            // If the parameter cache has not registered this parameter yet, that means it can't have changed, because there's nothing to compare against.
            // In this case, initialize the parameter in the cache for later.
            if (!parameterCache.ContainsKey(parameterName))
                return false;

            return parameterCache[parameterName].Equals(value);
        }

        /// <summary>
        /// Resets the cache of parameters for this shader. Should be used in contexts where the underlying shader used by this can be changed in contexts that do not respect the cache.<br></br>
        /// An example of this could be having this shader shared with a screen shader, which supplies its values directly and without the <see cref="TrySetParameter(string, object)"/> wrapper.
        /// </summary>
        public void ResetCache() => parameterCache.Clear();

        /// <summary>
        /// Attempts to send parameter data to the GPU for the shader to use.
        /// </summary>
        /// <param name="parameterName">The name of the parameter. This must correspond with the parameter name in the shader.</param>
        /// <param name="value">The value to supply to the parameter.</param>
        public bool TrySetParameter(string parameterName, object value)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return false;

            // Check if the parameter even exists. If it doesn't, obviously do nothing else.
            EffectParameter parameter = Shader.Value.Parameters[parameterName];
            if (parameter is null)
                return false;

            // Check if the parameter value is already cached as the supplied value. If it is, don't waste resources informing the GPU of
            // parameter data, since nothing relevant has changed.
            if (ParameterIsCachedAsValue(parameterName, value))
                return false;

            // Store the value in the cache.
            parameterCache[parameterName] = value;

            // Unfortunately, there is no simple type upon which singles, ints, matrices, etc. can be converted in order to be sent to the GPU, and there is no
            // super easy solution for checking a parameter's expected type. FNA just messes with pointers under the hood and tosses back exceptions if that doesn't work.
            // Unless something neater arises, this conditional chain will do, I suppose.

            // Booleans.
            if (value is bool b)
            {
                parameter.SetValue(b);
                return true;
            }
            if (value is bool[] b2)
            {
                parameter.SetValue(b2);
                return true;
            }

            // Integers.
            if (value is int i)
            {
                parameter.SetValue(i);
                return true;
            }
            if (value is int[] i2)
            {
                parameter.SetValue(i2);
                return true;
            }

            // Floats.
            if (value is float f)
            {
                parameter.SetValue(f);
                return true;
            }
            if (value is float[] f2)
            {
                parameter.SetValue(f2);
                return true;
            }

            // Vector2s.
            if (value is Vector2 v2)
            {
                parameter.SetValue(v2);
                return true;
            }
            if (value is Vector2[] v22)
            {
                parameter.SetValue(v22);
                return true;
            }

            // Vector3s.
            if (value is Vector3 v3)
            {
                parameter.SetValue(v3);
                return true;
            }
            if (value is Vector3[] v32)
            {
                parameter.SetValue(v32);
                return true;
            }

            // Colors.
            if (value is Color c)
            {
                parameter.SetValue(c.ToVector3());
                return true;
            }

            // Vector4s.
            if (value is Vector4 v4)
            {
                parameter.SetValue(v4);
                return true;
            }
            if (value is Rectangle rect)
            {
                parameter.SetValue(new Vector4(rect.X, rect.Y, rect.Width, rect.Height));
                return true;
            }
            if (value is Vector4[] v42)
            {
                parameter.SetValue(v42);
                return true;
            }

            // Matrices.
            if (value is Matrix m)
            {
                parameter.SetValue(m);
                return true;
            }
            if (value is Matrix[] m2)
            {
                parameter.SetValue(m2);
                return true;
            }

            // Textures, for if those are explicitly designed as parameters.
            if (value is Texture2D t)
            {
                parameter.SetValue(t);
                return true;
            }

            // None of the condition cases were met, and something went wrong.
            return false;
        }

        /// <summary>
        /// Sets a texture at a given index for this shader to use based a the <see cref="Asset{T}"/> wrapper. Typically, index 0 is populated with whatever was passed into the <see cref="SpriteBatch"/>.Draw call.
        /// </summary>
        /// <param name="textureAsset">The asset that contains the texture to supply.</param>
        /// <param name="textureIndex">The index to place the texture in.</param>
        /// <param name="samplerStateOverride">Which sampler should be used for the texture.</param>
        public void SetTexture(Asset<Texture2D> textureAsset, int textureIndex, SamplerState samplerStateOverride = null)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Collect the texture.
            Texture2D texture = textureAsset.Value;
            SetTexture(texture, textureIndex, samplerStateOverride);
        }

        /// <summary>
        /// Sets a texture at a given index for this shader to use. Typically, index 0 is populated with whatever was passed into the <see cref="SpriteBatch"/>.Draw call.
        /// </summary>
        /// <param name="texture">The texture to supply.</param>
        /// <param name="textureIndex">The index to place the texture in.</param>
        /// <param name="samplerStateOverride">Which sampler should be used for the texture.</param>
        public void SetTexture(Texture2D texture, int textureIndex, SamplerState samplerStateOverride = null)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Try to send texture sizes as parameters. Such parameters are optional, and no penalty is incurred if a shader decides that it doesn't need that data.
            TrySetParameter($"{TextureSizeParameterPrefix}{textureIndex}", texture.Size());

            // Grab the graphics device and send the texture to it.
            var gd = Main.instance.GraphicsDevice;
            gd.Textures[textureIndex] = texture;
            if (samplerStateOverride is not null)
                gd.SamplerStates[textureIndex] = samplerStateOverride;
        }

        /// <summary>
        /// Finalizes the shader for drawing.
        /// </summary>
        /// <param name="passName">The pass to apply.</param>
        public void Apply(string passName = DefaultPassName)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Try to send the global time as a parameter. It is optional, and no penalty is incurred if a shader decides that it doesn't need that data for some reason.
            TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);

            Shader.Value.CurrentTechnique.Passes[passName].Apply();
        }
    }
}
