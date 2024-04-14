using System;
using System.Collections.Generic;
using System.Linq;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NoxusBoss.Assets.Fonts;
using NoxusBoss.Common.SentenceGeneration;
using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using ReLogic.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace NoxusBoss.Content.Items.LoreItems
{
    public class LoreNamelessDeity : BaseLoreItem, IToastyQoLChecklistItemSupport
    {
        private static List<string> determiners;

        private static List<string> nouns;

        private static List<string> prepositions;

        private static List<string> adjectives;

        private static List<string> verbs;

        private static Dictionary<string, List<string>> grammar;

        internal static string textSpacer;

        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNamelessDeity;

        public override int TrophyID => ModContent.ItemType<NamelessDeityTrophy>();

        private List<string> GenerateFromLocalization(string key, int totalEntries)
        {
            // Generate all numbers from 1 to totalEntries and read the localization value for each number in that range.
            return Enumerable.Range(1, totalEntries).Select(suffix =>
            {
                return this.GetLocalizedValue($"{key}{suffix}");
            }).ToList();
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            UpdateTextOptions(LanguageManager.Instance);
            LanguageManager.Instance.OnLanguageChanged += UpdateTextOptions;
        }

        private void UpdateTextOptions(LanguageManager languageManager)
        {
            // Get contents from the localization.
            determiners = GenerateFromLocalization("Determiner", 2);
            nouns = GenerateFromLocalization("Noun", 201);
            prepositions = GenerateFromLocalization("Preposition", 41);
            adjectives = GenerateFromLocalization("Adjective", 200);
            verbs = GenerateFromLocalization("Verb", 150);

            // Generate the grammar with its default values.
            // https://www.linguisticsnetwork.com/an-intro-to-phrase-structure-rules/
            grammar = new()
            {
                ["DETERMINER"] = determiners,
                ["NOUN"] = nouns,
                ["PREPOSITION"] = prepositions,
                ["ADJECTIVE"] = adjectives,
                ["VERB"] = verbs,
            };

            textSpacer = this.GetLocalizedValue("IncludeSpacesBetweenWords").Equals("True", StringComparison.OrdinalIgnoreCase) ? " " : string.Empty;

            // Load grammar rules from the localization.
            string grammarText = this.GetLocalizedValue("PhraseStructureRules");
            string[] grammarLines = grammarText.Split(new string[]
            {
                "\n", "\r\n"
            }, StringSplitOptions.None);
            foreach (string grammarLine in grammarLines)
            {
                string[] lineData = grammarLine.Split(" = ");
                string grammarKeyword = lineData[0];
                List<string> grammarReplacementTypes = lineData[1].Split(", ").ToList();
                grammar[grammarKeyword] = grammarReplacementTypes;
            }
        }

        public override void Unload()
        {
            LanguageManager.Instance.OnLanguageChanged -= UpdateTextOptions;
        }

        public override void SetDefaults()
        {
            Item.UseCalamityRedRarity();
            base.SetDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine[] loreLines = new TooltipLine[8];

            if (Main.keyState.IsKeyDown(Keys.LeftShift))
                NamelessDeityLoreManager.LookingAtNamelessDeityLoreItem = true;

            // Generate individual lore lines.
            var font = FontRegistry.Instance.NamelessDeityText;
            for (int i = 0; i < loreLines.Length; i++)
            {
                // Generate the line text.
                ContextFreeGrammar sentenceGenerator = new(NamelessDeityLoreManager.Seed1 + i * 112, grammar);
                string sentence = sentenceGenerator?.GenerateSentence(font) ?? string.Empty;

                // Calculate the color of the text. It should flow in a continuous gradient between Nameless' magenta -> red -> orange -> yellow palette that his wings use.
                float colorInterpolant = Sin01(i * 0.3f - Main.GlobalTimeWrappedHourly * 3.4f);
                float hue = (0.9f + colorInterpolant * 0.19f) % 1f;
                Color lineColor = Main.hslToRgb(hue, 1f, 0.74f);

                // Use a custom text color if the sentence is the easter egg text.
                if (sentence == NamelessDeityLoreManager.EasterEggLine)
                    lineColor = Color.Lerp(Color.Aquamarine, Color.Blue, colorInterpolant * 0.4f);

                loreLines[i] = new(Mod, $"{TextKey}{i}", sentence)
                {
                    OverrideColor = lineColor
                };
            }

            // Override vanilla tooltips and display the lore tooltip instead.
            DrawHeldShiftTooltip(tooltips, loreLines, true);
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (!line.Name.Contains(TextKey))
                return true;

            // Use a special font.
            line.Font = FontRegistry.Instance.NamelessDeityText;

            // Generate the alternate sentence. When trolling mode is disabled, the text will interpolate between the two.
            int lineNumber = int.Parse(string.Concat(line.Name.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse()));
            ContextFreeGrammar sentenceGenerator = new(NamelessDeityLoreManager.Seed2 + lineNumber, grammar);
            string alternateSentence = sentenceGenerator.GenerateSentence(line.Font);

            float opacityInterpolant = EasingCurves.Quintic.Evaluate(EasingType.In, NamelessDeityLoreManager.SeedInterpolant);
            Color lineColor = line.OverrideColor ?? Color.White;

            // Draw the text. It will interpolate between the two values.
            Vector2 linePosition = new(line.X, line.Y);
            DrawBorderString(Main.spriteBatch, line.Font, line.Text, linePosition, lineColor * (1f - opacityInterpolant), 0.4f);
            DrawBorderString(Main.spriteBatch, line.Font, alternateSentence, linePosition, lineColor * opacityInterpolant, 0.4f);

            return false;
        }

        public static Vector2 DrawBorderString(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 pos, Color color, float scale = 1f, float anchorx = 0f, float anchory = 0f, int maxCharactersDisplayed = -1)
        {
            if (maxCharactersDisplayed != -1 && text.Length > maxCharactersDisplayed)
                text = text[..maxCharactersDisplayed];

            Vector2 textSize = font.MeasureString(text);
            ChatManager.DrawColorCodedStringWithShadow(sb, font, text, pos, color, 0f, new Vector2(anchorx, anchory) * textSize, new Vector2(scale), -1f, 1.5f);
            return textSize * scale;
        }
    }
}
