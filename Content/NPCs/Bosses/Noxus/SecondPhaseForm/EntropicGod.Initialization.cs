using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DropRules;
using NoxusBoss.Content.Items.Accessories.VanityEffects;
using NoxusBoss.Content.Items.Dyes;
using NoxusBoss.Content.Items.LoreItems;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Content.Items.SummonItems;
using NoxusBoss.Core;
using NoxusBoss.Core.Autoloaders;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC, IBossChecklistSupport, IInfernumBossBarSupport, IInfernumBossIntroCardSupport, IToastyQoLChecklistBossSupport
    {
        #region Crossmod Compatibility

        public LocalizedText IntroCardTitleName => this.GetLocalization("InfernumCompatibility.Title");

        public int IntroCardAnimationDuration => SecondsToFrames(1f);

        public bool IsMiniboss => false;

        public string ChecklistEntryName => "EntropicGodNoxus";

        public bool IsDefeated => WorldSaveSystem.HasDefeatedNoxus;

        public FieldInfo IsDefeatedField => typeof(WorldSaveSystem).GetField("hasDefeatedNoxus", BindingFlags.Static | BindingFlags.NonPublic);

        public float ProgressionValue => 27f;

        public List<int> Collectibles => new()
        {
            ModContent.ItemType<LoreNoxus>(),
            ModContent.ItemType<MidnightMonolith>(),
            MaskID,
            ModContent.ItemType<NoxusTrophy>(),
            RelicID,
            ModContent.ItemType<OblivionRattle>(),
        };

        public int? SpawnItem => ModContent.ItemType<Genesis>();

        public bool UsesCustomPortraitDrawing => true;

        public float IntroCardScale => 1.7f;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color)
        {
            Vector2 centered = area.Center.ToVector2() - bossChecklistTexture.Size() * 0.5f;
            spriteBatch.Draw(bossChecklistTexture.Value, centered, color);
        }

        public bool ShouldDisplayIntroCard() => Myself is not null;

        public SoundStyle ChooseIntroCardLetterSound() => FireballShootSound with { MaxInstances = 20, Volume = 0.5f, Pitch = -0.3f };

        public SoundStyle ChooseIntroCardMainSound() => ExplosionTeleportSound;

        public Color GetIntroCardTextColor(float horizontalCompletion, float animationCompletion)
        {
            return Color.Lerp(DialogColorRegistry.NoxusTextColor, Color.Black, 0.85f);
        }

        #endregion Crossmod Compatibility

        #region Attack Cycles
        public static EntropicGodAttackType[] Phase1AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.DarkExplosionCharges,
            EntropicGodAttackType.DarkEnergyBoltHandWave,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.HoveringHandGasBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RapidExplosiveTeleports,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.DarkExplosionCharges,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RapidExplosiveTeleports,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.DarkEnergyBoltHandWave,
            EntropicGodAttackType.HoveringHandGasBursts,
            EntropicGodAttackType.MigraineAttack
        };

        public static EntropicGodAttackType[] Phase2AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.GeometricSpikesTeleportAndFireballs,
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.OrganizedPortalCometBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.GeometricSpikesTeleportAndFireballs,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.OrganizedPortalCometBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.MigraineAttack,
        };

        public static EntropicGodAttackType[] Phase3AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.PortalChainCharges2,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.BrainFogAndThreeDimensionalCharges,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.MigraineAttack,
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
            // Autoload the mask for Noxus.
            MaskID = MaskAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/Noxus/AutoloadedContent/NoxusMask", ToastyQoLRequirementRegistry.PostNoxus);

            // Autoload the relic for Noxus.
            RelicAutoloader.Create(Mod, "NoxusBoss/Content/NPCs/Bosses/Noxus/AutoloadedContent/NoxusRelic", ToastyQoLRequirementRegistry.PostNoxus, out int relicID, out _);
            RelicID = relicID;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Scale = 0.3f,
                PortraitScale = 0.5f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.UsesNewTargetting[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            // Apply miracleblight immunities.
            NPC.MakeImmuneToMiracleblight();

            // Load textures.
            LoadTextures();
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 50f;
            NPC.damage = 380;
            NPC.width = 122;
            NPC.height = 290;
            NPC.defense = 130;
            NPC.SetLifeMaxByMode(7544400, 8475000, 10000000, 12477600, 32000000);

            if (Main.expertMode)
            {
                NPC.damage = 600;

                // Undo vanilla's automatic Expert boosts.
                NPC.lifeMax /= 2;
                NPC.damage /= 2;
            }

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(50, 0, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.hide = true;
            NPC.MakeCalamityBossBarClose();
            InitializeHandsIfNecessary();

            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EntropicGod");
        }

        public void InitializeHandsIfNecessary()
        {
            if (Hands is not null && Hands[0] is not null)
                return;

            Hands[0] = new()
            {
                DefaultOffset = DefaultHandOffset * new Vector2(-1f, 1f),
                Center = NPC.Center + DefaultHandOffset * new Vector2(-1f, 1f),
                Velocity = Vector2.Zero
            };
            Hands[1] = new()
            {
                DefaultOffset = DefaultHandOffset,
                Center = NPC.Center + DefaultHandOffset,
                Velocity = Vector2.Zero
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement($"Mods.{Mod.Name}.Bestiary.{Name}"),
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement()
            });
        }
        #endregion Initialization

        #region Loot
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // General drops.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NoxiousEvocator>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NoxusSprayer>()));

            // Lore item.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<LoreNoxus>()));

            // Vanity and decorations.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MidnightMonolith>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EntropicDye>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(MaskID, 7));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NoxusTrophy>(), 10));

            // Rev/Master exclusive loot.
            LeadingConditionRule revOrMaster = new(new RevengeanceOrMasterDropRule());
            revOrMaster.OnSuccess(ItemDropRule.Common(RelicID));
            revOrMaster.OnSuccess(ItemDropRule.Common(ModContent.ItemType<OblivionRattle>()));
            npcLoot.Add(revOrMaster);
        }

        public override void BossLoot(ref string name, ref int potionType) => SetOmegaPotionLoot(ref potionType);

        #endregion Loot
    }
}
