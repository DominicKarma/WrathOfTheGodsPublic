using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using ReLogic.Peripherals.RGB;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class KeyboardShaderLoader : ModSystem
    {
        public class SimpleCondition : CommonConditions.ConditionBase
        {
            private readonly Func<Player, bool> _condition;

            public SimpleCondition(Func<Player, bool> condition) => _condition = condition;

            public override bool IsActive() => _condition(CurrentPlayer);
        }

        private static readonly List<ChromaShader> loadedShaders = new();

        public static bool HasLoaded
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Allow for custom boss tracking with the keyboard shader system.
            On_NPC.UpdateRGBPeriheralProbe += TrackCustomBosses;
        }

        public override void OnModUnload()
        {
            // Manually remove all shaders from the central registry.
            foreach (ChromaShader loadedShader in loadedShaders)
                Main.Chroma.UnregisterShader(loadedShader);
        }

        public override void PostUpdateWorld()
        {
            if (HasLoaded || Main.netMode == NetmodeID.Server)
                return;

            // Register shaders.
            Color fogColor = Color.Lerp(Color.MediumPurple, Color.DarkGray, 0.85f);
            RegisterShader(new NoxusKeyboardShader(Color.MediumPurple, Color.Black, fogColor), NoxusKeyboardShader.IsActive, ShaderLayer.Boss);
            RegisterShader(new NamelessDeityKeyboardShader(), NamelessDeityKeyboardShader.IsActive, ShaderLayer.Boss);
            RegisterShader(new EternalGardenKeyboardShader(), EternalGardenKeyboardShader.IsActive, ShaderLayer.Weather);

            HasLoaded = true;
        }

        private void TrackCustomBosses(On_NPC.orig_UpdateRGBPeriheralProbe orig)
        {
            orig();

            // Noxus.
            if (EntropicGod.Myself is not null || NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>()))
                CommonConditions.Boss.HighestTierBossOrEvent = ModContent.NPCType<EntropicGod>();

            // Nameless Deity.
            if (NamelessDeityBoss.Myself is not null)
                CommonConditions.Boss.HighestTierBossOrEvent = ModContent.NPCType<NamelessDeityBoss>();
        }

        private static void RegisterShader(ChromaShader keyboardShader, ChromaCondition condition, ShaderLayer layer)
        {
            Main.QueueMainThreadAction(() =>
            {
                Main.Chroma.RegisterShader(keyboardShader, condition, layer);
                loadedShaders.Add(keyboardShader);
            });
        }
    }
}
