using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace NoxusBoss.Core.Graphics.UI
{
    public class UITextDynamic : UIElement
    {
        private float textScale = 1f;

        private bool isWrapped;

        private string visibleText;

        private string lastTextReference;

        private Vector2 textSize = Vector2.Zero;

        public bool DynamicallyScaleDownToWidth;

        public event Action OnInternalTextChange;

        public DynamicSpriteFont Font
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        public float TextOriginX
        {
            get;
            set;
        }

        public float TextOriginY
        {
            get;
            set;
        }

        public float WrappedTextBottomPadding
        {
            get;
            set;
        }

        public bool IsWrapped
        {
            get => isWrapped;
            set
            {
                isWrapped = value;
                InternalSetText(Text, textScale, Font);
            }
        }

        public Color TextColor
        {
            get;
            set;
        }

        public Color ShadowColor
        {
            get;
            set;
        } = Color.Black;

        public Vector2 Origin
        {
            get;
            set;
        }

        public Action PreDrawText
        {
            get;
            set;
        }

        public Action PostDrawText
        {
            get;
            set;
        }

        public UITextDynamic(string text, Color textColor, float textScale = 1f, DynamicSpriteFont font = null, Vector2? origin = null)
        {
            TextOriginX = 0.5f;
            TextOriginY = 0f;
            WrappedTextBottomPadding = 20f;
            Font = font ?? FontAssets.MouseText.Value;
            TextColor = textColor;
            Origin = origin ?? new Vector2(0f, 0.5f);
            InternalSetText(text, textScale, Font);
        }

        public UITextDynamic(LocalizedText text, Color textColor, float textScale = 1f, DynamicSpriteFont font = null, Vector2? origin = null)
        {
            TextOriginX = 0.5f;
            TextOriginY = 0f;
            WrappedTextBottomPadding = 20f;
            Font = font ?? FontAssets.MouseText.Value;
            TextColor = textColor;
            Origin = origin ?? new Vector2(0f, 0.5f);
            InternalSetText(text, textScale, Font);
        }

        public override void Recalculate()
        {
            InternalSetText(Text, textScale, Font);
            base.Recalculate();
        }

        public void SetText(string text)
        {
            InternalSetText(text, textScale, Font);
        }

        public void SetText(LocalizedText text)
        {
            InternalSetText(text, textScale, Font);
        }

        public void SetText(string text, float textScale, DynamicSpriteFont font)
        {
            InternalSetText(text, textScale, font);
        }

        public void SetText(LocalizedText text, float textScale, DynamicSpriteFont font)
        {
            InternalSetText(text, textScale, font);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            VerifyTextState();
            CalculatedStyle innerDimensions = base.GetInnerDimensions();
            Vector2 position = innerDimensions.Position() - Vector2.UnitY * textScale * 4f;

            // Offset text.
            Vector2 textSize = Font.MeasureString(visibleText);
            position.Y += (innerDimensions.Height - textSize.Y) * TextOriginY;

            // Calculate text scale, placing an upper bound to adhere to the max text size.
            Vector2 boundedScale = Vector2.One * textScale;
            if (DynamicallyScaleDownToWidth && textSize.X > innerDimensions.Width)
                boundedScale *= innerDimensions.Width / this.textSize.X;

            // Draw the text.
            Vector2 origin = textSize * Origin;
            TextSnippet[] snippets = ChatManager.ParseMessage(visibleText, TextColor).ToArray();
            ChatManager.ConvertNormalSnippets(snippets);

            PreDrawText?.Invoke();
            ChatManager.DrawColorCodedString(spriteBatch, Font, snippets, position, TextColor, 0f, origin, boundedScale, out _, -1f, false);
            PostDrawText?.Invoke();
        }

        private void VerifyTextState()
        {
            if (lastTextReference == Text)
                return;

            InternalSetText(Text, textScale, Font);
        }

        private void InternalSetText(object text, float textScale, DynamicSpriteFont font)
        {
            Text = text.ToString();
            this.textScale = textScale;
            lastTextReference = Text.ToString();
            visibleText = IsWrapped ? font.CreateWrappedText(lastTextReference, GetInnerDimensions().Width / this.textScale) : lastTextReference;

            Vector2 baseTextSize = font.MeasureString(visibleText);
            Vector2 textSize;
            if (IsWrapped)
                textSize = new Vector2(baseTextSize.X, baseTextSize.Y + WrappedTextBottomPadding) * textScale;
            else
                textSize = new Vector2(baseTextSize.X, 32f) * textScale;

            this.textSize = textSize;
            MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
            MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);

            OnInternalTextChange?.Invoke();
        }
    }
}
