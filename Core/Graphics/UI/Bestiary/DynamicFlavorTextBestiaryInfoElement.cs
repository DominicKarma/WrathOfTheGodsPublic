using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace NoxusBoss.Core.Graphics.UI.Bestiary
{
    public class DynamicFlavorTextBestiaryInfoElement : IBestiaryInfoElement
    {
        private string chosenText;

        private readonly string[] keys;

        private readonly DynamicSpriteFont font;

        public DynamicFlavorTextBestiaryInfoElement(string[] languageKeys, DynamicSpriteFont font)
        {
            keys = languageKeys;
            this.font = font;
        }

        public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
        {
            if (info.UnlockState < BestiaryEntryUnlockState.CanShowStats_2)
                return null;

            // Initialize the RNG if necessary and choose new text.
            Main.rand ??= new();
            string oldText = chosenText;
            do
                chosenText = Language.GetTextValue(Main.rand.Next(keys));
            while (chosenText == oldText);

            UIPanel panel = new(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Stat_Panel", AssetRequestMode.ImmediateLoad), null, 12, 7)
            {
                Width = new StyleDimension(-11f, 1f),
                Height = new StyleDimension(109f, 0f),
                BackgroundColor = new Color(43, 56, 101),
                BorderColor = Color.Transparent,
                Left = new StyleDimension(3f, 0f),
                PaddingLeft = 4f,
                PaddingRight = 4f
            };

            UITextDynamic text = new(chosenText, Color.Lerp(DialogColorRegistry.NamelessDeityTextColor, Color.White, 0.67f), 0.32f, font)
            {
                HAlign = 0f,
                VAlign = 0f,
                Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
                Height = StyleDimension.FromPixelsAndPercent(0f, 1f),
                IsWrapped = true,
                PreDrawText = PrepareForDrawing,
                PostDrawText = AfterDrawing,
            };
            AddDynamicResize(panel, text);
            panel.Append(text);
            return panel;
        }

        private void PrepareForDrawing()
        {
            Main.spriteBatch.End();            
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            // Apply the aberration dye effect on top of the text.
            float aberrationPower = Pow(AperiodicSin(Main.GlobalTimeWrappedHourly * 32f), 2f) * 0.4f;
            var aberrationShader = ShaderManager.GetShader("NoxusBoss.ChromaticAberrationShader");
            aberrationShader.TrySetParameter("splitIntensity", aberrationPower);
            aberrationShader.TrySetParameter("impactPoint", Vector2.One * 0.5f);
            aberrationShader.Apply();
        }

        private void AfterDrawing()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        private static void AddDynamicResize(UIElement container, UITextDynamic text)
        {
            text.OnInternalTextChange += () =>
            {
                container.Height = new StyleDimension(text.MinHeight.Pixels, 0f);
            };
        }
    }
}
