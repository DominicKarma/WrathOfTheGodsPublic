using System;
using System.Collections.Generic;
using System.Linq;
using NoxusBoss.Content.Items.LoreItems;
using ReLogic.Graphics;
using Terraria;
using Terraria.Utilities;

namespace NoxusBoss.Common.SentenceGeneration
{
    public class ContextFreeGrammar
    {
        protected Dictionary<string, List<string>> grammar;

        protected UnifiedRandom rng;

        protected int recursionLimitCounter;

        protected readonly int seed;

        public ContextFreeGrammar(int seed, Dictionary<string, List<string>> grammar)
        {
            this.grammar = grammar;
            rng = new(seed);
            this.seed = seed;
        }

        protected string InternalGenerate(string symbol)
        {
            recursionLimitCounter++;

            // Terminate if the grammer does not contain the given symbol.
            if (!grammar.TryGetValue(symbol, out List<string> production) || recursionLimitCounter >= 50)
                return symbol;

            string randomToken = rng.Next(production);
            string[] newSymbols = randomToken.Split(' ');

            // Recursively combine tokens until the entire result is populated with custom words.
            return string.Join(" ", newSymbols.Select(s => InternalGenerate(s)));
        }

        public string GenerateSentence(DynamicSpriteFont font)
        {
            // Generate the base sentence.
            string sentence;
            float maxLengthDeviation = NamelessDeityLoreManager.UseTrollingMode ? float.MaxValue : 30f;
            do
            {
                recursionLimitCounter = 0;
                sentence = InternalGenerate("SENTENCE");
                recursionLimitCounter = 0;

                // Replace "a blahblahblah" with "an blahblahblah" if the next word starts with a vowel.
                sentence = $" {sentence}";
                sentence = sentence.Replace(" a a", " an a").Replace(" a e", " an e").Replace(" a i", " an i").Replace(" a o", " an o").Replace(" a u", " an u");
                sentence = sentence.Replace(" a A", " an A").Replace(" a E", " an E").Replace(" a I", " an I").Replace(" a O", " an O").Replace(" a U", " an U");
                sentence = sentence[1..];

                // Ensure that the first character is capitalized.
                sentence = string.Concat(sentence.ToUpper()[0].ToString(), sentence.AsSpan(1));

                // Ogscule.
                sentence = sentence.Replace("the ogscule", "ogscule").Replace("a ogscule", "ogscule");

                // Replace spaces as necessary if the language doesn't use them.
                sentence = sentence.Replace(" ", LoreNamelessDeity.textSpacer);

                // Add a period at the end.
                sentence += ".";
            }
            while (Distance(font.MeasureString(sentence).X, 820f) >= maxLengthDeviation);

            // Very occasionally replace text with customized easter egg text.
            if (rng.NextBool(NamelessDeityLoreManager.EasterEggLineChance))
                return NamelessDeityLoreManager.EasterEggLine;

            return sentence;
        }

        public void RegenerateRNG() => rng = new(seed);
    }
}
