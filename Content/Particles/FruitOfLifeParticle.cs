using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items;
using NoxusBoss.Core.Graphics.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class FruitOfLifeParticle : Particle
    {
        public int FallOverDirection;

        public bool HasHitGround;

        public override string TexturePath => "NoxusBoss/Content/Items/GoodApple";

        public FruitOfLifeParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Lifetime = lifetime;
            Scale = scale;
            FallOverDirection = Main.rand.NextFromList(-1, 1);
        }

        public override void Update()
        {
            // Fade out when close to death.
            Opacity = InverseLerp(Lifetime, Lifetime - 120f, Time);

            // Move.
            Velocity.Y += 0.3f;
            Velocity = Collision.TileCollision(Position - Vector2.One * Scale * 11f, Velocity, (int)(Scale * 22f), (int)(Scale * 22f));

            // Decide rotation.
            if (Abs(Velocity.Y) >= 0.0001f)
                Rotation = Velocity.ToRotation() - PiOver2;
            else
            {
                Velocity.X = 0f;

                float fallOverSpeed = Remap(Abs(Rotation), 0.1f, 0.6f, 0.04f, 0.12f);
                Rotation = Clamp(Rotation + FallOverDirection * fallOverSpeed, -PiOver2, PiOver2);

                if (!HasHitGround)
                {
                    SoundEngine.PlaySound(SoundID.Item48 with { MaxInstances = 15, Volume = 0.75f, PitchVariance = 0.2f }, Position);
                    HasHitGround = true;
                }
            }

            // Give the player an apple if interacted with.
            if (Main.LocalPlayer.Hitbox.Intersects(Utils.CenteredRectangle(Position, Vector2.One * Scale * 20f)) && Time < Lifetime && Abs(Velocity.Y) <= 0.0001f)
            {
                Time = Lifetime;
                Main.LocalPlayer.QuickSpawnItem(new EntitySource_WorldEvent(), ModContent.ItemType<GoodApple>());
            }
        }
    }
}
