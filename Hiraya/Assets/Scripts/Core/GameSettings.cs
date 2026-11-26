using System;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// The choices made on the settings screen, saved with PlayerPrefs. Anything in the game that plays sound,
    /// speaks or draws text can read them from here and listen to Changed.
    /// AudioManager follows the volumes, LargeTextSupport follows Large text and the texts follow the language.
    /// </summary>
    public static class GameSettings
    {
        public const string Filipino = "fil";
        public const string English = "en";

        const string MusicKey = "Hiraya.Music";
        const string SfxKey = "Hiraya.Sfx";
        const string VoiceKey = "Hiraya.Voice";
        const string LanguageKey = "Hiraya.Language";
        const string LargeTextKey = "Hiraya.LargeText";
        const string MutedKey = "Hiraya.Muted";

        /// <summary>Raised after any setting changed.</summary>
        public static event Action Changed;

        /// <summary>Background music volume, 0 to 1.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.70f);
            set => SetFloat(MusicKey, value);
        }

        /// <summary>Volume of button taps and other effects, 0 to 1.</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.55f);
            set => SetFloat(SfxKey, value);
        }

        /// <summary>Volume of the narrator who says the Filipino words, 0 to 1.</summary>
        public static float VoiceVolume
        {
            get => PlayerPrefs.GetFloat(VoiceKey, 0.80f);
            set => SetFloat(VoiceKey, value);
        }

        /// <summary>GameSettings.Filipino or GameSettings.English.</summary>
        public static string Language
        {
            get => PlayerPrefs.GetString(LanguageKey, English);
            set
            {
                PlayerPrefs.SetString(LanguageKey, value == English ? English : Filipino);
                Changed?.Invoke();
            }
        }

        /// <summary>Bigger letters for small readers (LargeTextSupport on the texts).</summary>
        public static bool LargeText
        {
            get => PlayerPrefs.GetInt(LargeTextKey, 0) == 1;
            set => SetBool(LargeTextKey, value);
        }

        /// <summary>Silences everything. Also used by the sound button of the title screen.</summary>
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                ApplyAudio();
                Changed?.Invoke();
            }
        }

        /// <summary>Applies the mute setting to the whole game. Called when a screen opens.</summary>
        public static void ApplyAudio()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            Changed?.Invoke();
        }

        static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            Changed?.Invoke();
        }
    }
}
