using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets.Fonts;
using NoxusBoss.Common.Biomes;
using NoxusBoss.Common.DropRules;
using NoxusBoss.Content.Items;
using NoxusBoss.Content.Items.Accessories.VanityEffects;
using NoxusBoss.Content.Items.Accessories.Wings;
using NoxusBoss.Content.Items.Dyes;
using NoxusBoss.Content.Items.LoreItems;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Content.Items.Placeable;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Content.Items.SummonItems;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core;
using NoxusBoss.Core.Autoloaders;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.UI.Bestiary;
using NoxusBoss.Core.MiscSceneManagers;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;
using static NoxusBoss.Core.CrossCompatibility.Inbound.CalRemixCompatibilitySystem;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC, IBossChecklistSupport, IInfernumBossIntroCardSupport, IToastyQoLChecklistBossSupport
    {
        #region Crossmod Compatibility

        public LocalizedText IntroCardTitleName => this.GetLocalization("InfernumCompatibility.Title").WithFormatArgs(NPC.GivenOrTypeName);

        public int IntroCardAnimationDuration => SecondsToFrames(2.3f);

        public bool IsMiniboss => false;

        public string ChecklistEntryName => "NamelessDeity";

        public bool IsDefeated => WorldSaveSystem.HasDefeatedNamelessDeity;

        public FieldInfo IsDefeatedField => typeof(WorldSaveSystem).GetField("hasDefeatedNamelessDeity", BindingFlags.Static | BindingFlags.NonPublic);

        public float ProgressionValue => 28f;

        public List<int> Collectibles => new()
        {
            ModContent.ItemType<LoreNamelessDeity>(),
            Rok.RockID,
            ModContent.ItemType<DivineMonolith>(),
            ModContent.ItemType<CheatPermissionSlip>(),
            MaskID,
            RelicID,
            ModContent.ItemType<NamelessDeityTrophy>(),
            ModContent.ItemType<BlackHole>(),
            ModContent.ItemType<Starseed>(),
        };

        public int? SpawnItem => FakeTerminus.TerminusID;

        public bool UsesCustomPortraitDrawing => true;

        public float IntroCardScale => 1.95f;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Vector2 centeredDrawPosition = area.Center.ToVector2() - BossChecklistTexture.Size() * 0.5f;
            spriteBatch.Draw(BossChecklistTexture.Value, centeredDrawPosition, color);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        public bool ShouldDisplayIntroCard() => Myself is not null && Myself.As<NamelessDeityBoss>().CurrentState == NamelessAIType.RoarAnimation;

        public SoundStyle ChooseIntroCardLetterSound() => NamelessDeityScarySkyManager.FlashSound with { MaxInstances = 10 };

        public SoundStyle ChooseIntroCardMainSound() => ChuckleSound;

        public Color GetIntroCardTextColor(float horizontalCompletion, float animationCompletion)
        {
            return Color.Lerp(Color.White, Color.Fuchsia, 0.1f);
        }

        #endregion Crossmod Compatibility

        #region Attack Cycles

        // These attack cycles for Nameless are specifically designed to go in a repeated quick paced -> precise dance that gradually increases in speed across the different cycles.
        // Please do not change them without careful consideration.
        public static NamelessAIType[] Phase1Cycle => new[]
        {
            // Start off with the arcing attack. It will force the player to move around to evade the starbursts.
            NamelessAIType.ArcingEyeStarbursts,

            // After the starburst attack, there will be some leftover momentum. Force them to quickly get rid of it and transition to weaving with the dagger walls attack.
            NamelessAIType.RealityTearDaggers,

            // After the daggers have passed, it's a safe bet the player won't have much movement at the start to mess with the attack. As such, the exploding star attack happens next to work with that.
            NamelessAIType.ConjureExplodingStars,

            // Resume the slower pace with a "slower" attack in the form of a laserbeam attack.
            NamelessAIType.PerpendicularPortalLaserbeams,

            // Now that the player has spent a bunch of time doing weaving and tight, precise movements, get them back into the fast moving action again with the arcing starbursts.
            NamelessAIType.ArcingEyeStarbursts,

            // And again, follow up with a precise attack in the form of the star lasers. This naturally follows with the chasing quasar, which amps up the pacing again.
            NamelessAIType.SunBlenderBeams,
            NamelessAIType.CrushStarIntoQuasar,

            // Return to the fast starbursts attack again.
            NamelessAIType.ArcingEyeStarbursts,

            // Do the precise laserbeam charge attack to slow them down. From here the cycle will repeat at another high point.
            NamelessAIType.PerpendicularPortalLaserbeams
        };

        public static NamelessAIType[] Phase2Cycle => new[]
        {
            // Start out with a fast attack in the form of the screen slices.
            NamelessAIType.VergilScreenSlices,

            // Continue the fast pace with the punches + screen slices attack.
            NamelessAIType.RealityTearPunches,

            // Amp the pace up again with stars from the background. This will demand fast movement and zoning of the player.
            NamelessAIType.BackgroundStarJumpscares,

            // Get the player up close and personal with Nameless with the true-melee sword attack.
            NamelessAIType.SwordConstellation,

            // Return to something a bit slower again with the converging stars. This has a fast end point, however, which should naturally transition to the other attacks.
            NamelessAIType.InwardStarPattenedExplosions,

            // Make the player use their speed from the end of the previous attack with the punches.
            NamelessAIType.RealityTearPunches,
            
            // Use the zoning background stars attack again the continue applying fast pressure onto the player.
            NamelessAIType.BackgroundStarJumpscares,

            // Follow with a precise attack in the form of the star lasers. This naturally follows with the chasing quasar, which amps up the pacing again.
            // This is a phase 1 attack, but is faster in the second phase.
            NamelessAIType.SunBlenderBeams,
            NamelessAIType.CrushStarIntoQuasar,

            // Return to the fast paced cycle with the true melee sword constellation attack again.
            NamelessAIType.SwordConstellation,

            // Use the star convergence again, as the cycle repeats.
            NamelessAIType.InwardStarPattenedExplosions,
        };

        // With the exception of the clock attack this cycle should keep the player constantly on the move.
        public static NamelessAIType[] Phase3Cycle => new[]
        {
            // A chaotic slash chase sequence to keep the player constantly on their feet, acting as a quick introduction to the phase.
            NamelessAIType.DarknessWithLightSlashes,

            // Use the true melee sword attack to help keep the flow between the music-muting attacks. This one is a bit faster than the original from the second phase.
            NamelessAIType.SwordConstellation,

            // A "slower" attack in the form of the clock constellation.
            NamelessAIType.ClockConstellation,
            
            // Perform the cosmic laserbeam attack, ramping up the pace again.
            NamelessAIType.SuperCosmicLaserbeam,
            
            // Show the moment of creation as a final step.
            NamelessAIType.MomentOfCreation,
        };

        public static NamelessAIType[] TestCycle => new[]
        {
            NamelessAIType.RealityTearDaggers,
        };

        #endregion Attack Cycles

        #region Initialization

        public static int MaskID
        {
            get;
            private set;
        }

        public static int RelicID
        {
            get;
            private set;
        }

        public override void Load()
        {
            // Autoload the mask item.
            MaskID = MaskAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/NamelessDeity/AutoloadedContent/NamelessDeityMask", ToastyQoLRequirementRegistry.PostNamelessDeity);

            // Autoload the music boxes for Nameless.
            string musicPath = "Assets/Sounds/Music/NamelessDeity";
            MusicBoxAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/NamelessDeity/AutoloadedContent/NamelessDeityMusicBox", musicPath, ToastyQoLRequirementRegistry.PostNoxus, out _, out _);

            string musicPathP3 = "Assets/Sounds/Music/ARIA BEYOND THE BLAZING FIRMAMENT";
            MusicBoxAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/NamelessDeity/AutoloadedContent/NamelessDeityMusicBoxP3", musicPathP3, ToastyQoLRequirementRegistry.PostNoxus, out _, out _, DrawPhase3MusicBoxTile, DrawPhase3MusicBoxItemTooltips);

            // Autoload the relic for Nameless.
            RelicAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/NamelessDeity/AutoloadedContent/NamelessDeityRelic", ToastyQoLRequirementRegistry.PostNamelessDeity, out int relicID, out _);
            RelicID = relicID;
        }


        private static bool DrawPhase3MusicBoxTile(int x, int y)
        {
            Tile t = Framing.GetTileSafely(x, y);

            // Calculate the top left of the tile.
            int left = x - t.TileFrameX % 32 / 16;
            int top = y - t.TileFrameY % 32 / 16;

            // The time multiplier is a prime number to ensure that the cycle can for all values traverse the set of possible music box frames.
            ulong frameSeed = (ulong)(left * 19 + top * 76 + (int)(Main.GlobalTimeWrappedHourly * 50f) * 37);
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY % 32 + (int)(frameSeed / 36 * 36 % 2016);

            // Draw the texture.
            Texture2D mainTexture = TextureAssets.Tile[TileID.MusicBoxes].Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(x * 16 - Main.screenPosition.X, y * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(x, y).MultiplyRGB(new(198, 198, 198));
            Main.spriteBatch.Draw(mainTexture, drawPosition, new Rectangle(frameX, frameY, 16, 16), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

            return false;
        }

        private static bool DrawPhase3MusicBoxItemTooltips(DrawableTooltipLine line, ref int yOffset)
        {
            string replacementString = "REPLACED VIA CODE DONT CHANGE THIS";
            if (line.Text.Contains(replacementString))
            {
                Color rarityColor = line.OverrideColor ?? line.Color;
                Vector2 drawPosition = new(line.X, line.Y);

                // Draw lines.
                List<string> lines = [.. line.Text.Split(replacementString)];
                float staticSpacing = 150f;
                Vector2 staticPosition = drawPosition;
                Vector2 staticSize = Vector2.One;
                for (int i = 0; i < lines.Count; i++)
                {
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, lines[i], drawPosition, rarityColor, line.Rotation, line.Origin, line.BaseScale, line.MaxWidth, line.Spread);
                    drawPosition.X += line.Font.MeasureString(lines[i]).X * line.BaseScale.X;
                    if (i == 0)
                    {
                        drawPosition.X += staticSpacing;
                        staticPosition = drawPosition + new Vector2(-2f, -4f);
                        staticSize = new Vector2(staticSpacing - 6f, line.Font.MeasureString(lines[i]).Y * line.BaseScale.Y);
                    }
                }

                // Draw static where the replaced name text would be.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

                // Prepare the static shader.
                var staticShader = ShaderManager.GetShader("NoxusBoss.StaticOverlayShader");
                staticShader.TrySetParameter("staticInterpolant", 1f);
                staticShader.TrySetParameter("staticZoomFactor", 0.5f);
                staticShader.SetTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/noise"), 1, SamplerState.PointWrap);
                staticShader.Apply();

                // Draw the pixel.
                Main.spriteBatch.Draw(WhitePixel, staticPosition, null, Color.Black, 0f, WhitePixel.Size() * Vector2.UnitX, staticSize / WhitePixel.Size(), 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

                return false;
            }

            return true;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Scale = 0.16f,
                PortraitScale = 0.2f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.UsesNewTargetting[Type] = true;

            // Apply miracleblight immunities.
            NPC.MakeImmuneToMiracleblight();

            // Allow Nameless' fists to do optionally contact damage.
            On_NPC.GetMeleeCollisionData += ExpandEffectiveHitboxForHands;

            // Define loot data for players when they defeat Nameless.
            NoxusPlayer.LoadDataEvent += LoadDefeatStateForPlayer;
            NoxusPlayer.SaveDataEvent += SaveDefeatStateForPlayer;
            NoxusPlayer.PostUpdateEvent += GivePlayerLootIfNecessary;

            // Allow Nameless to have more than five stars in the bestiary.
            On_UIBestiaryEntryInfoPage.GetBestiaryInfoCategory += AddDynamicFlavorText;
            new ManagedILEdit("Remove Bestiary Five-Star Limitation", edit =>
            {
                IL_NPCPortraitInfoElement.CreateStarsContainer += edit.SubscriptionWrapper;
            }, RemoveBestiaryStarLimits).Apply();

            // Load textures.
            LoadTextures();

            // Load evil Fanny text.
            var hatred1 = new FannyDialog("NamelessDeityGFB1", "EvilIdle").WithDuration(6f).WithEvilness().WithCondition(_ =>
            {
                return Myself is not null && Myself.As<NamelessDeityBoss>().CurrentState == NamelessAIType.RealityTearPunches;
            }).WithoutClickability();
            var hatred2 = new FannyDialog("NamelessDeityGFB2", "EvilIdle").WithDuration(15f).WithEvilness().WithDrawSizes(900).WithParentDialog(hatred1, 3f);
            var hatred3 = new FannyDialog("NamelessDeityGFB3", "EvilIdle").WithDuration(9f).WithEvilness().WithDrawSizes(600).WithParentDialog(hatred2, 3f);
            hatred1.Register();
            hatred2.Register();
            hatred3.Register();

            // Initialize AI states.
            LoadStates();
        }

        private int AddDynamicFlavorText(On_UIBestiaryEntryInfoPage.orig_GetBestiaryInfoCategory orig, UIBestiaryEntryInfoPage self, IBestiaryInfoElement element)
        {
            // UIBestiaryEntryInfoPage.BestiaryInfoCategory.Flavor is inaccessible due to access modifiers. Use its literal value of 2 instead.
            if (element is DynamicFlavorTextBestiaryInfoElement)
                return 2;

            return orig(self, element);
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 100f;
            NPC.damage = 300;
            NPC.width = 270;
            NPC.height = 500;
            NPC.defense = 150;
            NPC.SetLifeMaxByMode(9600000, 11000000, 12000000, 13767256, 100000000);

            if (Main.expertMode)
                NPC.damage = 275;

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(100, 0, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.hide = true;
            NPC.Opacity = 0f;
            NPC.MakeCalamityBossBarClose();
            LoadSwappableTextures();

            SpawnModBiomes = new int[]
            {
                ModContent.GetInstance<EternalGardenBiome>().Type
            };
            Wings = new();
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * bossAdjustment / (Main.masterMode ? 3f : 2f));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // Remove the original NPCPortraitInfoElement instance, so that a new one with far more stars can be added.
            bestiaryEntry.Info.RemoveAll(i => i is NPCPortraitInfoElement);

            // Remove the original name display text instance, so that a new one with nothing can be added.
            bestiaryEntry.Info.RemoveAll(i => i is NamePlateInfoElement);

            string[] bestiaryKeys = new string[12];
            for (int i = 0; i < bestiaryKeys.Length; i++)
                bestiaryKeys[i] = Language.GetTextValue($"Mods.{Mod.Name}.Bestiary.{Name}{i + 1}");

            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new DynamicFlavorTextBestiaryInfoElement(bestiaryKeys, FontRegistry.Instance.NamelessDeityText),
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement(),
                new NPCPortraitInfoElement(50),
                new NamePlateInfoElement(string.Empty, NPC.netID)
            });
        }
        #endregion Initialization

        #region Loot

        public const string PlayerGiveLootFieldName = "GiveNamelessDeityLootUponReenteringWorld";

        private static void SaveDefeatStateForPlayer(NoxusPlayer p, TagCompound tag)
        {
            tag[PlayerGiveLootFieldName] = p.Player.GetValueRef<bool>(PlayerGiveLootFieldName).Value;
        }

        private static void LoadDefeatStateForPlayer(NoxusPlayer p, TagCompound tag)
        {
            p.GetValueRef<bool>(PlayerGiveLootFieldName).Value = tag.TryGet(PlayerGiveLootFieldName, out bool result) && result;
        }

        private static void GivePlayerLootIfNecessary(NoxusPlayer p)
        {
            // Give the player loot if they're entitled to it. If not, terminate immediately.
            if (!p.GetValueRef<bool>(PlayerGiveLootFieldName))
                return;

            // Move Nameless up, so that the loot comes from the sky.
            NPC dummyNameless = new();
            dummyNameless.SetDefaults(ModContent.NPCType<NamelessDeityBoss>());
            dummyNameless.Center = p.Player.Center - Vector2.UnitY * 275f;
            if (dummyNameless.position.Y < 400f)
                dummyNameless.position.Y = 400f;

            // Ensure that the loot does not appear in the middle of a bunch of blocks.
            for (int i = 0; i < 600; i++)
            {
                if (!Collision.SolidCollision(dummyNameless.Center, 1, 1))
                    break;

                dummyNameless.position.Y++;
            }

            // Log a kill in the bestiary
            Main.BestiaryTracker.Kills.RegisterKill(dummyNameless);

            // Nameless' loot and mark him as defeated.
            dummyNameless.NPCLoot();
            dummyNameless.active = false;
            WorldSaveSystem.HasDefeatedNamelessDeity = true;

            // Disable the loot flag.
            p.GetValueRef<bool>(PlayerGiveLootFieldName).Value = false;
        }

        public override void BossLoot(ref string name, ref int potionType) => SetOmegaPotionLoot(ref potionType);

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CheatPermissionSlip>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DeificTouch>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DivineWings>()));

            // Lore item.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<LoreNamelessDeity>()));

            // Vanity and decorations.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Cattail>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThePurifier>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DivineMonolith>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NuminousDye>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(MaskID, 7));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NamelessDeityTrophy>(), 10));

            // The rock only drops the first time Nameless is defeated.
            LeadingConditionRule firstTime = new(new BeforeNamelessDefeatedDropRule());
            firstTime.OnSuccess(ItemDropRule.Common(Rok.RockID));
            npcLoot.Add(firstTime);

            // Revengeance/Master exclusive items.
            LeadingConditionRule revOrMaster = new(new RevengeanceOrMasterDropRule());
            revOrMaster.OnSuccess(ItemDropRule.Common(RelicID));
            revOrMaster.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BlackHole>()));
            revOrMaster.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Starseed>()));
            npcLoot.Add(revOrMaster);
        }

        #endregion Loot
    }
}
