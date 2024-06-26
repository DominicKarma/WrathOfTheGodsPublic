﻿using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class NoxusFumesIllusionParticle : Particle
    {
        public int Variant;

        public static Asset<Texture2D> CalamitasIllusionTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> DraedonIllusionTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> LaRugaIllusionTexture
        {
            get;
            private set;
        }

        public override BlendState BlendState => BlendState.NonPremultiplied;

        public override string AtlasTextureName => "NoxusBoss.BloodParticle.png";

        public NoxusFumesIllusionParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                CalamitasIllusionTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/NoxusFumesCalamitasIllusion");
                DraedonIllusionTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/NoxusFumesDraedonIllusion");
                LaRugaIllusionTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/NoxusFumesLaRugaIllusion");
            }

            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
            Variant = Main.rand.Next(2);

            // Very rarely cause manifestations of La Ruga.
            if (Main.rand.NextBool(1000))
                Variant = 3;

            Direction = Main.rand.NextFromList(-1, 1);
        }

        public override void Update()
        {
            if (Time <= 6f)
                Rotation = Velocity.X * 0.12f;

            Opacity = InverseLerpBump(0f, 25f, 32f, Lifetime, Time) * 0.8f;
            Velocity *= 0.91f;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = Variant switch
            {
                1 => CalamitasIllusionTexture.Value,
                2 => DraedonIllusionTexture.Value,
                _ => LaRugaIllusionTexture.Value
            };
            int instanceCount = EntropicGod.Myself is null ? 1 : 2;
            Vector2 scale = new(Cbrt(Sin(Main.GlobalTimeWrappedHourly * 6.2f + Direction + Variant)) * 0.07f + Scale.X, Scale.Y);

            for (int i = 0; i < instanceCount; i++)
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, texture.Size() * 0.5f, scale, Direction.ToSpriteDirection(), 0f);
        }
    }
}
