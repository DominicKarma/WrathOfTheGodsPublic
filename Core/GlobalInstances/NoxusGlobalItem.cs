using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalItem : GlobalItem
    {
        // Declare custom events and their respective backing delegates.
        public delegate void ItemActionDelegate(Item item);

        public static event ItemActionDelegate SetDefaultsEvent;

        public delegate void ModifyTooltipsDelegate(Item item, List<TooltipLine> tooltips);

        public static event ModifyTooltipsDelegate ModifyTooltipsEvent;

        public delegate bool PreDrawInInventoryDelegate(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale);

        public static event PreDrawInInventoryDelegate PreDrawInInventoryEvent;

        public delegate bool PreDrawInWorldDelegate(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI);

        public static event PreDrawInWorldDelegate PreDrawInWorldEvent;

        public delegate bool CanItemDoActionWithPlayerDelegate(Item item, Player player);

        public static event CanItemDoActionWithPlayerDelegate CanUseItemEvent;

        public delegate void ItemPlayerActionDelegate(Item item, Player player);

        public static event ItemPlayerActionDelegate UseItemEvent;

        public override void Unload()
        {
            // Reset all events on mod unload.
            SetDefaultsEvent = null;
            ModifyTooltipsEvent = null;
            PreDrawInInventoryEvent = null;
            PreDrawInWorldEvent = null;
            CanUseItemEvent = null;
            UseItemEvent = null;
        }

        public override void SetDefaults(Item item)
        {
            SetDefaultsEvent?.Invoke(item);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            ModifyTooltipsEvent?.Invoke(item, tooltips);
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Use default behavior if the event has no subscribers.
            if (PreDrawInInventoryEvent is null)
                return true;

            bool result = true;
            foreach (Delegate d in PreDrawInInventoryEvent.GetInvocationList())
                result &= ((PreDrawInInventoryDelegate)d).Invoke(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);

            return result;
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // Use default behavior if the event has no subscribers.
            if (PreDrawInWorldEvent is null)
                return true;

            bool result = true;
            foreach (Delegate d in PreDrawInWorldEvent.GetInvocationList())
                result &= ((PreDrawInWorldDelegate)d).Invoke(item, spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);

            return result;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            // Use default behavior if the event has no subscribers.
            if (CanUseItemEvent is null)
                return true;

            bool result = true;
            foreach (Delegate d in CanUseItemEvent.GetInvocationList())
                result &= ((CanItemDoActionWithPlayerDelegate)d).Invoke(item, player);

            return result;
        }

        public override bool? UseItem(Item item, Player player)
        {
            UseItemEvent?.Invoke(item, player);
            return null;
        }
    }
}
