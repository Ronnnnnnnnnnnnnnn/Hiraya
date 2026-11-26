using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The pause menu (wireframe 06): "Naka-pause" with Ituloy, Uliting ang antas, Mga Setting and Bumalik sa Mapa,
    /// and a sound button in the corner. "Mga Setting" opens the same settings screen as the title screen
    /// (SettingsScreen > SettingsPanel) and its back arrow returns here.
    /// It only shows the menu and raises events; GameplayDirector stops the game and does what the buttons ask.
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        public event Action Resumed;
        public event Action RestartRequested;
        public event Action MapRequested;

        [Header("Menu")]
        [Tooltip("The dim background and the white card. Hidden while the settings screen is open.")]
        [SerializeField] GameObject menu;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button mapButton;
        [SerializeField] Button soundButton;
        [SerializeField] Image soundIcon;
        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;

        [Header("Settings (opened by Mga Setting)")]
        [SerializeField] GameObject settingsScreen;
        [SerializeField] SettingsController settings;

        public bool IsSettingsOpen => settingsScreen != null && settingsScreen.activeSelf;

        void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (mapButton != null) mapButton.onClick.AddListener(OnMap);
            if (soundButton != null) soundButton.onClick.AddListener(OnSound);
            if (settings != null) settings.BackPressed += CloseSettings;
            GameSettings.Changed += RefreshSound;

            if (settingsScreen != null) settingsScreen.SetActive(false);
            if (menu != null) menu.SetActive(true);
            ShowSubtitle();
            RefreshSound();
        }

        void ShowSubtitle()
        {
            if (subtitle != null) subtitle.text = Loc.T("Let's take a break, ", "Pahinga muna tayo, ") + GameSession.DisplayName + "!";
        }

        void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (mapButton != null) mapButton.onClick.RemoveListener(OnMap);
            if (soundButton != null) soundButton.onClick.RemoveListener(OnSound);
            if (settings != null) settings.BackPressed -= CloseSettings;
            GameSettings.Changed -= RefreshSound;
        }

        /// <summary>The Escape key (the Android back button): leaves the settings screen, or else resumes the game.</summary>
        public void Back()
        {
            if (IsSettingsOpen) CloseSettings();
            else Resumed?.Invoke();
        }

        void OnResume() => Resumed?.Invoke();

        void OnRestart() => RestartRequested?.Invoke();

        void OnMap() => MapRequested?.Invoke();

        void OnSound() => GameSettings.Muted = !GameSettings.Muted;

        void RefreshSound()
        {
            if (soundIcon != null) soundIcon.sprite = GameSettings.Muted ? soundOffSprite : soundOnSprite;
        }

        void OpenSettings()
        {
            if (settingsScreen == null) return;
            if (menu != null) menu.SetActive(false);
            settingsScreen.SetActive(true);
        }

        void CloseSettings()
        {
            if (settingsScreen != null) settingsScreen.SetActive(false);
            if (menu != null) menu.SetActive(true);
            ShowSubtitle();   // the language may have been changed
            RefreshSound();
        }
    }
}
