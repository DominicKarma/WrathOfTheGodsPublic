using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.PreFightForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.NPCs.Critters.EternalGarden;
using NoxusBoss.Content.Projectiles.Typeless;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.MiscOPTools
{
    public class NoxusSprayer : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        internal static int LaRugaID = -9999;

        public static List<int> NPCsToNotDelete => new()
        {
            NPCID.CultistTablet,
            NPCID.DD2LanePortal,
            NPCID.DD2EterniaCrystal,
            NPCID.TargetDummy,
            ModContent.NPCType<NoxusEgg>(),
            ModContent.NPCType<NoxusEggCutscene>(),
            ModContent.NPCType<EntropicGod>(),

            ModContent.NPCType<Aelithrysuwl>(),
            ModContent.NPCType<Vivajuyfylae>()
        };

        public static List<int> NPCsThatReflectSpray
        {
            get;
            private set;
        } = new();

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            NPCsThatReflectSpray.Add(ModContent.NPCType<NamelessDeityBoss>());
            if (ModReferences.CalamityRemix is not null)
            {
                LaRugaID = ModReferences.CalamityRemix.Find<ModNPC>("LaRuga").Type;
                NPCsThatReflectSpray.Add(LaRugaID);
            }
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 34;
            Item.useAnimation = 2;
            Item.useTime = 2;
            Item.autoReuse = true;
            Item.noMelee = true;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.DD2_BookStaffCast with { MaxInstances = 50, Volume = 0.3f, PitchVariance = 0.2f };

            Item.UseCalamityRedRarity();

            Item.shoot = ModContent.ProjectileType<NoxusSprayerGas>();
            Item.shootSpeed = 7f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new(0f, 4.5f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position -= velocity * 4f;
        }
    }
}
