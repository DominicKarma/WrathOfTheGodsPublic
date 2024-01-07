using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Particles
{
    public abstract class Particle
    {
        private bool hasSpawned;

        /// <summary>
        /// The internal ID of this particle. This exists for the purpose of allowing the texture manager to efficiently differentiate particle types, and should not matter for most operations.
        /// </summary>
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// A cached <see cref="Texture2D"/> that holds this projectile's standard texture. Should be used for any draw operations.
        /// </summary>
        public Texture2D Texture
        {
            get;
            protected set;
        }

        public int Frame;

        public int Time;

        public int Lifetime;

        public int Direction;

        public Vector2 Position;

        public Vector2 Velocity;

        public Color Color;

        public float Rotation;

        public float Scale = 1f;

        public float Opacity = 1f;

        /// <summary>
        /// The lifetime ratio of this particle.
        /// </summary>
        public float LifetimeRatio => Time / (float)Lifetime;

        /// <summary>
        /// The path to this particle's default texture.
        /// <br></br>
        /// For efficiency, use <see cref="Texture"/> when drawing, rather than using <see cref="ModContent.Request{T}(string, ReLogic.Content.AssetRequestMode)"/> on this string.
        /// </summary>
        public abstract string TexturePath
        {
            get;
        }

        /// <summary>
        /// How many frames this particle has in its standard texture. Defaults to 1.
        /// </summary>
        public virtual int FrameCount => 1;

        /// <summary>
        /// The desired <see cref="BlendState"/> that this particle should be drawn with. Defaults to <see cref="BlendState.AlphaBlend"/>, but can be overriden.
        /// <br></br>
        /// <b>Particles are grouped by blend state when drawing for efficiency. Do not restart the sprite batch manually in the <see cref="Draw"/> hook unless you <u>absolutely</u> know what you're doing.</b>
        /// </summary>
        public virtual BlendState DrawBlendState => BlendState.AlphaBlend;

        /// <summary>
        /// Allows for optional one-time loading effects for a given particle. This hook is not called server-side, and as such can be safely used without needing to check before doing things like loading textures.
        /// </summary>
        public virtual void Load()
        {

        }

        /// <summary>
        /// The universal frame update hook. By default does nothing. <see cref="Time"/> increments and <see cref="Position"/> updates based on <see cref="Velocity"/> are separate from this and don't need to be manually included.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// The universal particle drawing hook. Can be overridden, but uses standard texture drawing based on things such as <see cref="Opacity"/>, <see cref="Scale"/>, <see cref="Direction"/>, etc.
        /// </summary>
        public virtual void Draw()
        {
            Rectangle frame = Texture.Frame(1, FrameCount, 0, Frame);
            SpriteEffects visualDirection = Direction.ToSpriteDirection();
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, Scale, visualDirection, 0f);
        }

        /// <summary>
        /// Spawns this particle in the world. This can only be performed once. This does not do anything if called server-side.
        /// </summary>
        public void Spawn()
        {
            // Particles cannot be spawned serverside.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Disallow spawning twice.
            if (hasSpawned)
                return;

            // Store this particle's ID and texture for draw lookups.
            ID = ParticleManager.particleIDLookup[GetType()];
            Texture = ParticleManager.particleTextureLookup[ID];

            // Spawn the particle by adding it to the manager's collection.
            ParticleManager.activeParticles.Add(this);

            // Mark this particle as spawned.
            hasSpawned = true;
        }

        /// <summary>
        /// Immediately destroys this particle.
        /// </summary>
        public void Kill() => Time = Lifetime;
    }
}
