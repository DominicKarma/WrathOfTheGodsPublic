using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Metaballs;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets
{
    public class BabyNoxus : ModProjectile
    {
        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> BackTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> HandTexture
        {
            get;
            private set;
        }

        public Vector2 LeftHandPosition
        {
            get;
            set;
        }

        public Vector2 RightHandPosition
        {
            get;
            set;
        }

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;

            if (Main.netMode != NetmodeID.Server)
            {
                MyTexture = ModContent.Request<Texture2D>(Texture);
                BackTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Projectiles/Pets/BabyNoxusBack");
                HandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Projectiles/Pets/BabyNoxusHand");
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                LeftHandPosition = RightHandPosition = Projectile.Center;
            }

            CheckActive();

            // Hover near the owner.
            Vector2 hoverDestination = Owner.Center + new Vector2(Owner.direction * -50f, -36f);
            Projectile.velocity = (Projectile.velocity + Projectile.DirectionToSafe(hoverDestination) * 0.2f).ClampLength(0f, 24f);
            if (Vector2.Dot(Projectile.velocity, Projectile.DirectionToSafe(hoverDestination)) < 0f)
                Projectile.velocity *= 0.95f;
            Projectile.rotation = Projectile.velocity.X * 0.04f;

            // Have hands hover near Noxus.
            Vector2 leftHandDestination = Projectile.Center + new Vector2(-60f, 44f);
            Vector2 rightHandDestination = Projectile.Center + new Vector2(60f, 44f);
            LeftHandPosition = Utils.MoveTowards(Vector2.Lerp(LeftHandPosition, leftHandDestination, 0.05f), leftHandDestination, 15f);
            RightHandPosition = Utils.MoveTowards(Vector2.Lerp(RightHandPosition, rightHandDestination, 0.05f), rightHandDestination, 15f);

            // Emit pitch black metaballs around based on movement.
            if (Projectile.Opacity >= 0.5f)
            {
                int metaballSpawnLoopCount = (int)Remap(Projectile.Opacity, 1f, 0f, 5f, 1f);
                for (int i = 0; i < metaballSpawnLoopCount; i++)
                {
                    Vector2 gasSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(20f, 20f) * Projectile.scale + (Projectile.position - Projectile.oldPosition).SafeNormalize(Vector2.UnitY) * 3f + Vector2.UnitY.RotatedBy(Projectile.rotation) * 18f;
                    float gasSize = Projectile.width * Projectile.scale * Projectile.Opacity * 0.45f;
                    float angularOffset = Sin(Main.GlobalTimeWrappedHourly * 1.1f) * 0.77f - Projectile.rotation;
                    PitchBlackMetaball.CreateParticle(gasSpawnPosition, Main.rand.NextVector2Circular(2f, 2f) + Projectile.velocity.RotatedBy(angularOffset).RotatedByRandom(0.2f) * 0.26f, gasSize);
                }
            }
        }

        public void CheckActive()
        {
            // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
            if (!Owner.dead && Owner.HasBuff(ModContent.BuffType<BabyNoxusBuff>()))
                Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = MyTexture.Value;
            Texture2D handTexture = HandTexture.Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);

            float leftHandRotation = (LeftHandPosition - Projectile.Center).ToRotation();
            float rightHandRotation = (RightHandPosition - Projectile.Center).ToRotation();
            if (LeftHandPosition.X < Projectile.Center.X)
                leftHandRotation += Pi;
            if (RightHandPosition.X < Projectile.Center.X)
                rightHandRotation += Pi;

            Main.EntitySpriteDraw(handTexture, LeftHandPosition - Main.screenPosition, null, Projectile.GetAlpha(Color.White), leftHandRotation, handTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.FlipHorizontally, 0);
            Main.EntitySpriteDraw(handTexture, RightHandPosition - Main.screenPosition, null, Projectile.GetAlpha(Color.White), rightHandRotation, handTexture.Size() * 0.5f, Projectile.scale, 0, 0);
            return false;
        }

        public void DrawBack()
        {
            Texture2D back = BackTexture.Value;
            Vector2 backDrawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY.RotatedBy(Projectile.rotation) * Projectile.scale * 28f;
            Main.EntitySpriteDraw(back, backDrawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, back.Size() * 0.5f, Projectile.scale * new Vector2(0.6f, 0.88f), 0, 0);
        }
    }
}
