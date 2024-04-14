using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.NamelessDeityBoss;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public class NamelessDeityHand
    {
        public int DirectionOverride;

        public int PositionalDirectionOverride;

        public bool UsePalmForm;

        public float RotationOffset;

        public bool CanDoDamage;

        public bool HasGlock;

        public bool HasArms;

        public float Opacity = 1f;

        public float ScaleFactor = 1f;

        public float TrailOpacity;

        public Vector2 Center;

        public Vector2 ActualCenter;

        public Vector2 Velocity;

        public Vector2[] OldCenters = new Vector2[40];

        public NamelessDeityHand(Vector2 spawnPosition, bool useHands)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Center = spawnPosition;
            HasArms = useHands;
        }

        public void ClearPositionCache()
        {
            for (int i = 0; i < OldCenters.Length; i++)
                OldCenters[i] = Vector2.Zero;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return SmoothStep(80f, 7.8f, completionRatio) * TrailOpacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            // Make the trail fade out at the end and fade in shparly at the start, to prevent the trail having a definitive, flat "start".
            float trailOpacity = InverseLerpBump(0.75f, 0.27f, 0.067f, 0f, completionRatio) * 0.9f;

            // Interpolate between a bunch of colors based on the completion ratio.
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.OrangeRed, Color.Yellow, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = MulticolorLerp(Pow(completionRatio, 1.6f), startingColor, middleColor, endColor) * trailOpacity;

            color.A /= 8;
            return color * TrailOpacity;
        }

        /// <summary>
        /// Draws the hand.
        /// </summary>
        /// <param name="screenPos">The screen position.</param>
        /// <param name="ownerCenter">The Nameless Deity's center position.</param>
        /// <param name="scale">The scale of the robes.</param>
        /// <param name="currentAttack">The current attack of the Nameless Deity this hand is associated with.</param>
        /// <param name="zPosition">The Nameless Deity's Z position.</param>
        /// <param name="zPositionOpacity">The opacity this hand should use a consequence of being in the background.</param>
        /// <param name="opacity">The general opacity of the hand.</param>
        /// <param name="variant">The hand's texture variant.</param>
        /// <param name="handTexture">The hand's texture.</param>
        /// <param name="forearmTexture">The forearm's texture.</param>
        /// <param name="armTexture">The arm's texture.</param>
        public void Draw(Vector2 screenPos, Vector2 ownerCenter, Vector2 scale, NamelessAIType currentAttack, float zPosition, float zPositionOpacity, float opacity, int variant, Texture2D handTexture, Texture2D forearmTexture, Texture2D armTexture)
        {
            // Collect draw information at first, such as texture selections and draw position.
            float generalOpacity = Pow(Opacity, 3f) * (currentAttack == NamelessAIType.DeathAnimation ? 1f : opacity);
            float handRotation = RotationOffset;
            Color handColor = Color.Lerp(Color.White, Color.DarkGray, 1f - zPositionOpacity) * generalOpacity;
            Vector2 drawPosition = Center - screenPos;
            Rectangle frame = handTexture.Frame();
            Vector2 handScale = scale * ScaleFactor * 0.5f;
            if (currentAttack == NamelessAIType.DeathAnimation && UsePalmForm)
                handScale /= Pow(zPosition + 1f, 0.75f);

            SpriteEffects direction = Center.X > ownerCenter.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            SpriteEffects positionalDirection = direction;
            if (PositionalDirectionOverride != 0)
                positionalDirection = PositionalDirectionOverride == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            int positionalDirectionInt = positionalDirection.HasFlag(SpriteEffects.FlipHorizontally).ToDirectionInt();
            if (DirectionOverride != 0)
                direction = (-DirectionOverride).ToSpriteDirection();
            Vector2 handOrigin = frame.Size() * 0.5f;

            // Handle draw values for if this hand is an open palm.
            if (UsePalmForm)
            {
                handTexture = PalmTexture.Value;
                frame = handTexture.Frame();
                handScale *= 1.3f;
            }

            // If the hand doesn't have a palm, draw the arm.
            else if (HasArms)
            {
                // Calculate arm lengths.
                float armScale = 0.5f;
                float forearmLength = forearmTexture.Width * armScale;
                float armLength = armTexture.Size().Length() * armScale * 0.8f;
                float forearmEndRetraction = 0.22f;
                if (variant == 1)
                    forearmEndRetraction = 0.15f;

                // Calculate arm draw positions.
                Vector2 armStart = ownerCenter + new Vector2((Center.X - ownerCenter.X).NonZeroSign() * 120f, 40f) - screenPos;
                Vector2 armMidpoint = CalculateElbowPosition(armStart, drawPosition, forearmLength * (1f - forearmEndRetraction), armLength, positionalDirectionInt != 1);

                // Calculate the forearm and hand origins since those vary by texture.
                // Als calculate the hand offset.
                float handOffsetDirectionOffset = 0f;
                Vector2 armOrigin = new(257, 234);
                Vector2 forearmOrigin = Vector2.Zero;
                Vector2 handOffset = Vector2.Zero;
                switch (variant)
                {
                    case 0:
                        forearmOrigin = new(135, 208);
                        handOrigin = new(71, 147);
                        handOffset = new(805, 55);
                        handOffsetDirectionOffset = positionalDirectionInt * -0.18f;
                        break;
                    case 1:
                        forearmOrigin = new(79, 222);
                        handOrigin = new(61, 149);
                        handOffset = new(831, 25);
                        if (Myself.As<NamelessDeityBoss>().ForearmTexture.TextureVariant == 0)
                        {
                            handOffset.X += 50f;
                            handOffset.Y += positionalDirectionInt == 1 ? 330f : -120f;
                        }
                        break;
                    case 2:
                        forearmOrigin = new(147, 253);
                        handOrigin = new(74, 269);
                        handOffset = new(868, 70);
                        break;
                    case 3:
                        forearmOrigin = new(118, 235);
                        handOrigin = new(60, 349);
                        handOffset = new(938, 33);
                        handOffsetDirectionOffset = -0.1f;
                        break;
                    case 4:
                        forearmOrigin = new(140, 196);
                        handOrigin = new(63, 242);
                        handOffset = new(749, 92);

                        if (currentAttack is NamelessAIType.SwordConstellation or NamelessAIType.RealityTearPunches or NamelessAIType.Glock)
                            handOrigin = new(39, 148);
                        if (Myself.As<NamelessDeityBoss>().ForearmTexture.TextureVariant == 0)
                            handOffset.Y += positionalDirectionInt == 1 ? 290f : -150f;

                        break;
                }
                float armAngularOffset = -Atan(armTexture.Height / (float)armTexture.Width);
                float armRotation = (armMidpoint - armStart).ToRotation() + armAngularOffset;
                float forearmAngularOffset = Atan(forearmTexture.Height / (float)forearmTexture.Width);
                float forearmRotation = (drawPosition - armMidpoint).ToRotation();
                if (positionalDirectionInt == 1)
                {
                    armOrigin.X = armTexture.Width - armOrigin.X;
                    forearmOrigin.X = forearmTexture.Width - forearmOrigin.X;
                    handOrigin.X = handTexture.Width - handOrigin.X;
                    armRotation += Pi - armAngularOffset * 2f;
                    forearmRotation += Pi;

                    if (variant == 1)
                        handOffset.Y -= 150f;
                    if (variant == 2)
                        handOffset.Y -= 230f;
                    if (variant == 4)
                        handOffset.Y -= 188f;
                }
                else
                {
                    if (variant == 2)
                        handOffset.Y += 20f;
                    if (variant == 4)
                        handOffset.Y += 24f;
                }
                handRotation += forearmRotation;

                // Draw the arm.
                Main.spriteBatch.Draw(armTexture, armStart, null, handColor, armRotation, armOrigin, armScale, positionalDirection, 0f);

                // Draw the forearm.
                armMidpoint.Y -= 16f;
                Main.spriteBatch.Draw(forearmTexture, armMidpoint, null, handColor, forearmRotation, forearmOrigin, armScale, positionalDirection, 0f);

                // Draw the elbow joint.
                Texture2D elbowJoint = ElbowJointTexture.Value;
                Main.spriteBatch.Draw(elbowJoint, armMidpoint + Vector2.UnitY * 16f, null, handColor, 0f, elbowJoint.Size() * 0.5f, armScale, positionalDirection, 0f);
                handOffset = handOffset.RotatedBy(forearmAngularOffset * (positionalDirectionInt == -1 ? 1f : -2f));

                // Prepare drawing for hands.
                float handOffsetFactor = 0.4f;
                drawPosition = armMidpoint + (handOffset * new Vector2(-positionalDirectionInt, 1f)).RotatedBy(forearmRotation + armAngularOffset + handOffsetDirectionOffset * positionalDirectionInt) * handOffsetFactor;
                if (variant == 3)
                {
                    if (positionalDirectionInt == 1)
                        drawPosition.X -= 20f;
                    drawPosition.Y -= 50f;
                }

                ActualCenter = drawPosition + screenPos;
            }

            // Draw the glock if this hand has one.
            if (HasGlock)
            {
                Texture2D glockTexture = GlockTexture.Value;
                Vector2 glockOrigin = new(40, 223);
                Main.spriteBatch.Draw(glockTexture, drawPosition, null, handColor, handRotation, glockOrigin, handScale, direction, 0f);
            }

            // Draw hand afterimages.
            for (int i = 8; i >= 0; i--)
            {
                float afterimageOpacity = 1f - i / 7f;
                Vector2 afterimageDrawOffset = Velocity * i * -0.15f;
                if (currentAttack == NamelessAIType.RealityTearPunches)
                    afterimageDrawOffset *= 4f;

                Main.spriteBatch.Draw(handTexture, drawPosition + afterimageDrawOffset, frame, handColor * afterimageOpacity, handRotation, handOrigin, handScale, direction, 0f);
            }

            // Draw the hands in their position.
            Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor, handRotation, handOrigin, handScale, direction, 0f);
            Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor with { A = 0 } * 0.5f, handRotation, handOrigin, handScale, direction, 0f);
        }

        /// <summary>
        /// Writes this hand's non-visual data to a <see cref="BinaryWriter"/> for the purposes of packet creation.
        /// </summary>
        /// <param name="writer">The packet's binary writer.</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((byte)HasArms.ToInt());
            writer.Write((byte)UsePalmForm.ToInt());
            writer.Write((byte)CanDoDamage.ToInt());
            writer.Write(Opacity);
            writer.Write(RotationOffset);
            writer.WriteVector2(Center);
            writer.WriteVector2(Velocity);
        }

        /// <summary>
        /// Reads this hand's non-visual data from a <see cref="BinaryReader"/> for the purposes of packet handling.
        /// </summary>
        /// <param name="reader">The packet's binary reader.</param>
        public static NamelessDeityHand ReadFrom(BinaryReader reader)
        {
            bool hasArms = reader.ReadByte() != 0;
            bool usePalmForm = reader.ReadByte() != 0;
            bool canDoDamage = reader.ReadByte() != 0;
            float opacity = reader.ReadSingle();
            float rotationOffset = reader.ReadSingle();
            Vector2 center = reader.ReadVector2();
            Vector2 velocity = reader.ReadVector2();

            return new(center, hasArms)
            {
                HasArms = hasArms,
                UsePalmForm = usePalmForm,
                CanDoDamage = canDoDamage,
                Opacity = opacity,
                RotationOffset = rotationOffset,
                Velocity = velocity
            };
        }
    }
}
