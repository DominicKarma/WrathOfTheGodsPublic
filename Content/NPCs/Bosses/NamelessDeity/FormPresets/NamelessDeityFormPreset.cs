using System;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using ReLogic.Content;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets
{
    public class NamelessDeityFormPreset
    {
        public bool UseCensor;

        public Func<bool> UsageCondition;

        public Action<Texture2D> ShaderOverlayEffect;

        public int[] PreferredAntlerTextures;

        public int[] PreferredArmTextures;

        public int[] PreferredFinTextures;

        public int[] PreferredFlowerTextures;

        public int[] PreferredForearmTextures;

        public int[] PreferredHandTextures;

        public int[] PreferredVineTextures;

        public int[] PreferredWheelTextures;

        public int[] PreferredWingTextures;

        public Referenced<Asset<Texture2D>> CensorReplacementTexture;

        public NamelessDeityFormPreset(Func<bool> usageCondition)
        {
            UsageCondition = usageCondition;
            UseCensor = true;
        }

        public NamelessDeityFormPreset WithDisabledCensor()
        {
            UseCensor = false;
            return this;
        }

        public NamelessDeityFormPreset WithCensorReplacement(Referenced<Asset<Texture2D>> censor)
        {
            CensorReplacementTexture = censor;
            return this;
        }

        public NamelessDeityFormPreset WithCustomShader(Action<Texture2D> shaderOverlayEffect)
        {
            ShaderOverlayEffect = shaderOverlayEffect;
            return this;
        }

        public NamelessDeityFormPreset WithAntlerPreference(params int[] preferredAntlerTextures)
        {
            PreferredAntlerTextures = preferredAntlerTextures;
            return this;
        }

        public NamelessDeityFormPreset WithArmPreference(params int[] preferredArmTextures)
        {
            PreferredArmTextures = preferredArmTextures;
            return this;
        }

        public NamelessDeityFormPreset WithFinPreference(params int[] preferredFinTextures)
        {
            PreferredFinTextures = preferredFinTextures;
            return this;
        }

        public NamelessDeityFormPreset WithFlowerPreference(params int[] preferredFlowerTextures)
        {
            PreferredFlowerTextures = preferredFlowerTextures;
            return this;
        }

        public NamelessDeityFormPreset WithForearmPreference(params int[] preferredForearmTextures)
        {
            PreferredForearmTextures = preferredForearmTextures;
            return this;
        }

        public NamelessDeityFormPreset WithHandPreference(params int[] preferredHandTextures)
        {
            PreferredHandTextures = preferredHandTextures;
            return this;
        }

        public NamelessDeityFormPreset WithVinePreference(params int[] preferredVineTextures)
        {
            PreferredVineTextures = preferredVineTextures;
            return this;
        }

        public NamelessDeityFormPreset WithWheelPreference(params int[] preferredWheelTextures)
        {
            PreferredWheelTextures = preferredWheelTextures;
            return this;
        }

        public NamelessDeityFormPreset WithWingPreference(params int[] preferredWingTextures)
        {
            PreferredWingTextures = preferredWingTextures;
            return this;
        }
    }
}
