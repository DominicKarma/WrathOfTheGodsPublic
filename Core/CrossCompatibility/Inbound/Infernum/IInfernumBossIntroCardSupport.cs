using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.Localization;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public interface IInfernumBossIntroCardSupport
    {
        public LocalizedText IntroCardTitleName
        {
            get;
        }

        public int IntroCardAnimationDuration
        {
            get;
        }

        public float IntroCardScale => 1f;

        public bool ShouldIntroCardTextBeCentered => true;

        public bool ShouldDisplayIntroCard();

        public void OnIntroCardCompletion() { }

        public SoundStyle ChooseIntroCardLetterSound();

        public SoundStyle ChooseIntroCardMainSound();

        public Effect LetterDrawShaderEffect => null;

        public void PrepareLetterDrawShader() { }

        public Color GetIntroCardTextColor(float horizontalCompletion, float animationCompletion);
    }
}
