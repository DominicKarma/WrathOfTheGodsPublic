using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    // This system serves as a safe, self-cleaning way of blocking UI and user input information. By defining conditions explicitly the blocks are automatically undone when they are no longer valid.
    // This way the burden isn't on whatever initiated the block to handle all of the edge-cases where they accidentally linger for longer than expected (such as if a player dies and boss goes away before said boss can undo things).
    [Autoload(Side = ModSide.Client)]
    public class InputAndUIBlockerSystem : ModSystem
    {
        public readonly struct BlockCondition
        {
            public readonly bool BlockInputs;

            public readonly bool BlockUI;

            public readonly Func<bool> BlockIsInEffect;

            public bool IsntDoingAnything => !BlockInputs && !BlockUI;

            public BlockCondition(bool input, bool ui, Func<bool> blockIsActiveCondition)
            {
                BlockInputs = input;
                BlockUI = ui;
                BlockIsInEffect = blockIsActiveCondition;
            }
        }

        private static readonly List<BlockCondition> blockerConditions = new();

        public static bool AnythingWasBlockedLastFrame
        {
            get;
            private set;
        }

        public static void Start(bool input, bool ui, Func<bool> blockIsActiveCondition)
        {
            blockerConditions.Add(new(input, ui, blockIsActiveCondition));
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Determine whether the UI or game inputs should be blocked.
            AnythingWasBlockedLastFrame = false;
            foreach (BlockCondition block in blockerConditions)
            {
                // Blocks that affect neither UI nor game inputs are irrelevant and can be safely skipped over. They will be naturally disposed of in the blocker condition.
                if (block.IsntDoingAnything)
                    continue;

                if (block.BlockIsInEffect())
                {
                    if (block.BlockInputs)
                        Main.blockInput = true;
                    if (block.BlockUI)
                        Main.hideUI = true;

                    AnythingWasBlockedLastFrame = true;
                }
            }

            // Remove all block conditions that are no longer applicable, keeping track of how many conditions existed prior to the block.
            int originalBlockCount = blockerConditions.Count;
            blockerConditions.RemoveAll(b => b.IsntDoingAnything || !b.BlockIsInEffect());

            // Check if the block conditions are all gone. If they are, return things to normal on the frame they were removed.
            // Condition verification checks are not necessary for these queries because invalid blocks are already filtered out by the RemoveAll above.
            bool anythingWasRemoved = blockerConditions.Count < originalBlockCount;
            if (anythingWasRemoved)
            {
                bool anythingIsBlockingInputs = blockerConditions.Any(b => b.BlockUI);
                bool anythingIsBlockingUI = blockerConditions.Any(b => b.BlockUI);
                Main.hideUI = anythingIsBlockingInputs;
                Main.blockInput = anythingIsBlockingUI;
            }
        }

        public static void WorldEnterAndExitClearing()
        {
            // Clear all blocking conditions if any were active at the time of entering/exiting the world.
            blockerConditions.Clear();
            if (AnythingWasBlockedLastFrame)
            {
                Main.blockInput = false;
                Main.hideUI = false;
                AnythingWasBlockedLastFrame = false;
            }
        }

        public override void OnWorldLoad() => WorldEnterAndExitClearing();

        public override void OnWorldUnload() => WorldEnterAndExitClearing();
    }
}
