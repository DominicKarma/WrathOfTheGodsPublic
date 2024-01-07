using System.IO;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Autoloaders
{
    public static class MaskAutoloader
    {
        [Autoload(false)]
        [AutoloadEquip(EquipType.Head)]
        public class AutoloadableMask : ModItem, IToastyQoLChecklistItemSupport
        {
            private readonly string texturePath;

            private readonly string name;

            private readonly ToastyQoLRequirement obtainmentRequirement;

            public ToastyQoLRequirement Requirement => obtainmentRequirement;

            public override string Name => name;

            public override string Texture => texturePath;

            // Necessary for autoloaded types since the constructor is important in determining the behavior of the given instance, making it impossible to rely on an a parameterless one for
            // managing said instances.
            protected override bool CloneNewInstances => true;

            public AutoloadableMask(string texturePath, ToastyQoLRequirement requirement)
            {
                string name = Path.GetFileName(texturePath);
                obtainmentRequirement = requirement;
                this.texturePath = texturePath;
                this.name = name;
            }

            public override void SetStaticDefaults()
            {
                Item.ResearchUnlockCount = 1;

                if (Main.netMode != NetmodeID.Server)
                {
                    int headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
                    ArmorIDs.Head.Sets.DrawHead[headSlot] = false;
                }
            }

            public override void SetDefaults()
            {
                Item.width = 24;
                Item.height = 24;
                Item.rare = ItemRarityID.Blue;
                Item.vanity = true;
                Item.maxStack = 1;
            }
        }

        public static int Create(Mod mod, string maskPath, ToastyQoLRequirement requirement)
        {
            AutoloadableMask mask = new(maskPath, requirement);
            mod.AddContent(mask);

            return mask.Type;
        }
    }
}
