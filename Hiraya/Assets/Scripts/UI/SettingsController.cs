using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The settings screen (wireframe 09): sound sliders, language, accessibility and the parent controls.
    /// Everything is saved at once in GameSettings. Press back to leave.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [Serializable]
        public class SliderRow
        {
            public Slider slider;
            public TMP_Text percent;
        }

        [Serializable]
        public class SwitchRow
        {
            public Toggle toggle;
            public Image track;
            public RectTransform knob;
        }

        public event Action BackPressed;
        /// <summary>Raised by "Buksan ang Dashboard". TitleScreenController opens the parent dashboard on it.</summary>
        public event Action DashboardPressed;

        [Header("Tunog")]
        [SerializeField] SliderRow music;
        [SerializeField] SliderRow sfx;
        [SerializeField] SliderRow voice;
        [SerializeField] SwitchRow muteAll;

        [Header("Wika")]
        [SerializeField] Button filipinoButton;
        [SerializeField] Image filipinoFill;
        [SerializeField] TMP_Text filipinoLabel;
        [SerializeField] Button englishButton;
        [SerializeField] Image englishFill;
        [SerializeField] TMP_Text englishLabel;

        [Header("Accessibility")]
        [SerializeField] SwitchRow largeText;

        [Header("Parent / Teacher")]
        [SerializeField] Button dashboardButton;
        [SerializeField] UnityEvent onDashboard;

        [SerializeField] Button backButton;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color switchOff = new Color(0.82f, 0.82f, 0.80f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] float knobTravel = 24f;

        void OnEnable()
        {
            Load();

            Listen(music, v => GameSettings.MusicVolume = v);
            Listen(sfx, v => GameSettings.SfxVolume = v);
            Listen(voice, v => GameSettings.VoiceVolume = v);
            Listen(muteAll, on => GameSettings.Muted = on);
            Listen(largeText, on => GameSettings.LargeText = on);

            if (filipinoButton != null) filipinoButton.onClick.AddListener(() => ChooseLanguage(GameSettings.Filipino));
            if (englishButton != null) englishButton.onClick.AddListener(() => ChooseLanguage(GameSettings.English));
            if (dashboardButton != null) dashboardButton.onClick.AddListener(OnDashboardClicked);
            if (backButton != null) backButton.onClick.AddListener(Back);
        }

        void OnDisable()
        {
            Unlisten(music);
            Unlisten(sfx);
            Unlisten(voice);
            Unlisten(muteAll);
            Unlisten(largeText);

            if (filipinoButton != null) filipinoButton.onClick.RemoveAllListeners();
            if (englishButton != null) englishButton.onClick.RemoveAllListeners();
            if (dashboardButton != null) dashboardButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            PlayerPrefs.Save();
        }

        void OnDashboardClicked()
        {
            onDashboard.Invoke();
            DashboardPressed?.Invoke();
        }

        void Back()
        {
            PlayerPrefs.Save();
            BackPressed?.Invoke();
        }

        // ------------------------------------------------------------------ show what is saved

        void Load()
        {
            Show(music, GameSettings.MusicVolume);
            Show(sfx, GameSettings.SfxVolume);
            Show(voice, GameSettings.VoiceVolume);
            Show(muteAll, GameSettings.Muted);
            Show(largeText, GameSettings.LargeText);
            ShowLanguage(GameSettings.Language);
        }

        void Show(SliderRow row, float value)
        {
            if (row == null || row.slider == null) return;
            row.slider.SetValueWithoutNotify(value);
            ShowPercent(row);
        }

        void ShowPercent(SliderRow row)
        {
            if (row.percent != null) row.percent.text = Mathf.RoundToInt(row.slider.value * 100f) + "%";
        }

        void Show(SwitchRow row, bool on)
        {
            if (row == null || row.toggle == null) return;
            row.toggle.SetIsOnWithoutNotify(on);
            Paint(row, on);
        }

        void Paint(SwitchRow row, bool on)
        {
            if (row.track != null) row.track.color = on ? orange : switchOff;
            if (row.knob != null) row.knob.anchoredPosition = new Vector2(on ? knobTravel : -knobTravel, 0f);
        }

        void ShowLanguage(string language)
        {
            bool filipino = language != GameSettings.English;
            Paint(filipinoFill, filipinoLabel, filipino);
            Paint(englishFill, englishLabel, !filipino);
        }

        void Paint(Image fill, TMP_Text label, bool selected)
        {
            if (fill != null) fill.color = selected ? orange : Color.white;
            if (label != null) label.color = selected ? Color.white : ink;
        }

        // ------------------------------------------------------------------ react to the child / parent

        void Listen(SliderRow row, UnityAction<float> save)
        {
            if (row == null || row.slider == null) return;
            row.slider.onValueChanged.AddListener(value =>
            {
                save(value);
                ShowPercent(row);
            });
        }

        void Listen(SwitchRow row, UnityAction<bool> save)
        {
            if (row == null || row.toggle == null) return;
            row.toggle.onValueChanged.AddListener(on =>
            {
                save(on);
                Paint(row, on);
            });
        }

        static void Unlisten(SliderRow row)
        {
            if (row != null && row.slider != null) row.slider.onValueChanged.RemoveAllListeners();
        }

        static void Unlisten(SwitchRow row)
        {
            if (row != null && row.toggle != null) row.toggle.onValueChanged.RemoveAllListeners();
        }

        void ChooseLanguage(string language)
        {
            GameSettings.Language = language;
            ShowLanguage(language);
        }
    }
}
