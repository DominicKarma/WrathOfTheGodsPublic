using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets;
using NoxusBoss.Core.Configuration;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets.NamelessDeityFormPresetRegistry;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        private readonly List<NamelessDeitySwappableTexture> swappableTextures = new();

        public NamelessDeityFormPreset UsedPreset
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture AntlersTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture ArmTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture FinsTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture ForearmTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture HandTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture ScarfTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture SideFlowerTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture VinesTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture WheelTexture
        {
            get;
            private set;
        }

        public NamelessDeitySwappableTexture WingsTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D>[] ArmTextures
        {
            get;
            private set;
        }

        // NOTE -- Not used by the actual boss directly anymore, but it shall remain here due to its relevance to this boss as a character.
        public static Asset<Texture2D> EyeTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> EyeFullTexture
        {
            get;
            private set;
        }

        // Note: Used exclusively as legacy content for the ripper UI destruction visual.
        public static Asset<Texture2D> FistTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> Fist2Texture
        {
            get;
            private set;
        }

        // Note: Used exclusively as legacy content for the ripper UI destruction visual.
        public static Asset<Texture2D> PalmTexture
        {
            get;
            private set;
        }

        // NOTE -- Not used by the actual boss directly anymore, but it shall remain here due to its relevance to this boss as a character.
        public static Asset<Texture2D> PupilTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> BossChecklistTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> AtlasMothBodyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> AtlasMothWingTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> DivineBodyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> ElbowJointTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> FishGivingMiddleFingerTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> FlowerTopTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> FlowerClockHourHandTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> FlowerClockMinuteHandTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D>[] ForearmTextures
        {
            get;
            private set;
        }

        public static Asset<Texture2D> EyeOfEternityTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> GlockTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> GlowRingTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> HeadHandsTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> TopHatTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> XRayCensorSketchTexture
        {
            get;
            private set;
        }

        private void LoadTextures()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Load legacy textures.
            EyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityEye");
            EyeFullTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityEyeFull");
            PupilTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityPupil");

            // Load actual textures.
            BossChecklistTexture = ModContent.Request<Texture2D>($"{Texture}_BossChecklist");
            AtlasMothBodyTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/AtlasMothBody");
            AtlasMothWingTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/AtlasMothWing");
            DivineBodyTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/DivineBody");
            ElbowJointTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/ElbowJoint");
            FistTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityHandFist");
            Fist2Texture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityHandFist2");
            FishGivingMiddleFingerTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/FishGivingMiddleFinger");
            FlowerTopTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/FlowerTop");
            FlowerClockHourHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/SideFlowerHourHand");
            FlowerClockMinuteHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/SideFlowerMinuteHand");
            EyeOfEternityTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/EyeOfEternity");
            GlockTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/Glock");
            GlowRingTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/GlowRing");
            HeadHandsTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/HeadHands");
            PalmTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/NamelessDeityPalm");
            TopHatTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/TopHat");
            XRayCensorSketchTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/XRayCensorSketch");

            ArmTextures = new Asset<Texture2D>[6];
            ForearmTextures = new Asset<Texture2D>[5];
            for (int i = 0; i < ArmTextures.Length; i++)
                ArmTextures[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/Arm{i + 1}");

            for (int i = 0; i < ForearmTextures.Length; i++)
                ForearmTextures[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Parts/Forearm{i + 1}");
        }

        private void LoadSwappableTextures()
        {
            // Determine which preset should be used for this instance of the Nameless Deity.
            // This will be used in determining restrictions upon.
            if (!NPC.IsABestiaryIconDummy)
                UsedPreset = SelectFirstAvailablePreset();

            AntlersTexture = RegisterSwappableTexture("Antlers", 7, UsedPreset?.PreferredAntlerTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = 90;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            ArmTexture = RegisterSwappableTexture("Arm", 6, UsedPreset?.PreferredArmTextures ?? null);
            FinsTexture = RegisterSwappableTexture("Fins", 5, UsedPreset?.PreferredFinTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = 164;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            ForearmTexture = RegisterSwappableTexture("Forearm", 5, UsedPreset?.PreferredForearmTextures ?? null);
            HandTexture = RegisterSwappableTexture("Hand", 5, UsedPreset?.PreferredHandTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = 164;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            ScarfTexture = RegisterSwappableTexture("Scarf", 4).WithAutomaticSwapRule(() =>
            {
                int swapRate = NoxusBossConfig.Instance.PhotosensitivityMode ? 8 : 4;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            SideFlowerTexture = RegisterSwappableTexture("SideFlower", 6, UsedPreset?.PreferredFlowerTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = NoxusBossConfig.Instance.PhotosensitivityMode ? 13 : 7;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            VinesTexture = RegisterSwappableTexture("Vines", 5, UsedPreset?.PreferredVineTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = 150;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            WheelTexture = RegisterSwappableTexture("Wheel", 6, UsedPreset?.PreferredWheelTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = NoxusBossConfig.Instance.PhotosensitivityMode ? 19 : 7;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });
            WingsTexture = RegisterSwappableTexture("Wings", 5, UsedPreset?.PreferredWingTextures ?? null).WithAutomaticSwapRule(() =>
            {
                int swapRate = 180;
                if (UsingYuHPreset && !NoxusBossConfig.Instance.PhotosensitivityMode)
                    swapRate = 1;

                return FightLength % swapRate == 0;
            });

            HandTexture.OnSwap += () =>
            {
                // Keep the arm and forearm textures synced with the hand by default. Presets may override this.
                if ((UsedPreset?.PreferredArmTextures ?? null) is null)
                    ArmTexture.TextureVariant = HandTexture.TextureVariant;
                if ((UsedPreset?.PreferredForearmTextures ?? null) is null)
                    ForearmTexture.TextureVariant = HandTexture.TextureVariant;
            };
            SideFlowerTexture.OnSwap += () =>
            {
                SideFlowerScale = Main.rand.NextFloat(1f, 1.108f);
            };

            RerollAllSwappableTextures();
        }

        public NamelessDeitySwappableTexture RegisterSwappableTexture(string partPrefix, int totalVariants, int[] speciallyAllowedVariants = null)
        {
            NamelessDeitySwappableTexture texture = new(partPrefix, totalVariants, speciallyAllowedVariants);
            swappableTextures.Add(texture);

            return texture;
        }

        public void UpdateSwappableTextures()
        {
            for (int i = 0; i < swappableTextures.Count; i++)
                swappableTextures[i].Update();
        }

        public void RerollAllSwappableTextures()
        {
            for (int i = 0; i < swappableTextures.Count; i++)
                swappableTextures[i].Swap();
        }
    }
}
