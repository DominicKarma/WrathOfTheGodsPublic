using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class CalRemixCompatibilitySystem : ModSystem
    {
        private static readonly Queue<FannyDialog> deferredDialogToRegister = new();

        public class FannyDialog
        {
            private readonly object instance;

            public FannyDialog(string dialogKey, string portrait)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return;

                // Add the mod name in front of the identifier.
                string identifier = $"NoxusBoss_{dialogKey}";

                string dialog = Language.GetTextValue($"Mods.NoxusBoss.FannyDialog.{dialogKey}");
                instance = CalamityRemix.Call("CreateFannyDialog", identifier, dialog, portrait);
            }

            public FannyDialog WithoutPersistenceBetweenWorlds()
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("MakeFannyDialogNotPersist", instance);
                return this;
            }

            public FannyDialog WithoutClickability()
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("MakeFannyDialogNonClickable", instance);
                return this;
            }

            public FannyDialog WithCooldown(float cooldownInSeconds)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("SetFannyDialogCooldown", instance, cooldownInSeconds);
                return this;
            }

            public FannyDialog WithCondition(Func<IEnumerable<NPC>, bool> condition)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("AddFannyDialogCondition", instance, condition);
                return this;
            }

            public FannyDialog WithDrawSizes(int maxWidth = 380, float fontSizeFactor = 1f)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("SetFannyDialogDrawSize", instance, maxWidth, fontSizeFactor);
                return this;
            }

            public FannyDialog WithDuration(float durationInSeconds)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("SetFannyDialogDuration", instance, durationInSeconds);
                return this;
            }

            public FannyDialog WithRepeatability()
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("MakeFannyDialogRepeatable", instance);
                return this;
            }

            public FannyDialog WithEvilness()
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("MakeFannyDialogSpokenByEvilFanny", instance);
                return this;
            }

            public FannyDialog WithHoverItem(int itemID, float drawScale = 1f, Vector2 drawOffset = default)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("AddFannyItemDisplay", instance, itemID, drawScale, drawOffset);
                return this;
            }

            public FannyDialog WithParentDialog(FannyDialog parent, float appearDelayInSeconds, bool parentNeedsToBeClickedOff = false)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("ChainFannyDialog", parent.instance, instance, appearDelayInSeconds);
                if (!parentNeedsToBeClickedOff)
                    return WithoutClickability().WithCondition(_ => true);

                return WithCondition(_ => true);
            }

            public FannyDialog WithHoverText(string hoverText)
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return this;

                CalamityRemix.Call("SetFannyHoverText", instance, hoverText);
                return this;
            }

            public static bool JustReadLoreItem(int loreItemID)
            {
                (bool readLoreItem, int hoverItemID) = (Tuple<bool, int>)CalamityRemix.Call("GetFannyItemHoverInfo");

                return readLoreItem && hoverItemID == loreItemID;
            }

            public void Register()
            {
                if (CalamityRemix is null || Main.netMode == NetmodeID.Server)
                    return;

                if (Main.gameMenu)
                {
                    deferredDialogToRegister.Enqueue(this);
                    return;
                }

                CalamityRemix.Call("RegisterFannyDialog", instance);
            }
        }

        public static void MakeCountAsLoreItem(int loreItemID)
        {
            CalamityRemix?.Call("MakeItemCountAsLoreItem", loreItemID);
        }

        public override void PreUpdateEntities()
        {
            while (deferredDialogToRegister.TryDequeue(out FannyDialog dialog))
                dialog.Register();
        }
    }
}
