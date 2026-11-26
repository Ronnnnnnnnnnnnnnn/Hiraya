using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>One choice on the title screen. Boy plays Fin, Girl plays Fiona.</summary>
    [Serializable]
    public class CharacterOption
    {
        public string label;
        public CharacterChoice character;
    }

    /// <summary>
    /// The title screen (wireframe 01) and the steps that follow it:
    ///   1. Title: choose Boy or Girl, press MAGLARO.
    ///   2. Age: tap your age, press Susunod.
    ///   3. World map (WorldMapController): choose a place and press play. Age 5 and below opens the Kinder scene,
    ///      6 and up the Grade 1 and up scene.
    /// The Parent / Teacher and Settings buttons only raise an event for now; connect the screens to them in the Inspector.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Serializable]
        public class AvatarSlot
        {
            public Button button;
            public Image ring;
            public TMP_Text nameLabel;
        }

        [Serializable]
        public class AgeSlot
        {
            public Button button;
            public Image fill;
            public Image ring;
            public TMP_Text label;
        }

        [Header("Panels")]
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject agePanel;
        [SerializeField] GameObject worldMapPanel;
        [SerializeField] WorldMapController worldMap;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] SettingsController settings;
        [SerializeField] GameObject wordBookPanel;
        [SerializeField] SalitaBookController wordBook;
        [Header("Parent / Teacher")]
        [Tooltip("Ask a small sum before the dashboard opens, so a child cannot open it by accident. Untick to open it straight away.")]
        [SerializeField] bool requireParentGate = true;
        [SerializeField] GameObject parentGatePanel;
        [SerializeField] ParentGateController parentGate;
        [SerializeField] GameObject dashboardPanel;
        [SerializeField] ParentDashboardController dashboard;

        [Header("Boy / Girl")]
        [SerializeField] CharacterOption[] options =
        {
            new CharacterOption { label = "Boy", character = CharacterChoice.Fin },
            new CharacterOption { label = "Girl", character = CharacterChoice.Fiona }
        };
        [SerializeField] AvatarSlot[] avatars;
        [SerializeField] int defaultOption = 0;

        [Header("Title buttons")]
        [SerializeField] Button playButton;
        [SerializeField] Button parentButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button soundButton;
        [SerializeField] UnityEvent onParentPressed;
        [SerializeField] UnityEvent onSettingsPressed;

        [Header("Sound icon")]
        [SerializeField] Image soundIcon;
        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;

        [Header("Age step")]
        [Tooltip("The age written on each age button, in the same order as the buttons.")]
        [SerializeField] int[] ageValues = { 3, 4, 5, 6, 7, 8, 9 };
        [SerializeField] AgeSlot[] ageSlots;
        [SerializeField] Button nextButton;
        [SerializeField] Button backButton;

        [Header("Look")]
        [SerializeField] Color selectedRingColor = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color normalRingColor = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color selectedNameColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] Color normalNameColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] float avatarSize = 150f;
        [SerializeField] float normalRingWidth = 3f;
        [SerializeField] float selectedRingWidth = 8f;
        [SerializeField] float ageSize = 130f;

        const string OptionKey = "Hiraya.TitleChoice";

        int selectedOption;
        int selectedAge = -1;

        void Start()
        {
            selectedOption = Mathf.Clamp(PlayerPrefs.GetInt(OptionKey, defaultOption), 0, Mathf.Max(0, options.Length - 1));

            for (int i = 0; i < avatars.Length && i < options.Length; i++)
            {
                if (avatars[i].nameLabel != null) avatars[i].nameLabel.text = options[i].label;
            }
            for (int i = 0; i < ageSlots.Length && i < ageValues.Length; i++)
            {
                if (ageSlots[i].label != null) ageSlots[i].label.text = ageValues[i].ToString();
            }

            RefreshAvatars();
            RefreshAges();
            ApplySound();
            ShowTitle();

            // Coming back from a playing scene (pause menu or results screen): open on the world map.
            if (GameSession.ReturnToWorldMap)
            {
                GameSession.ReturnToWorldMap = false;
                selectedAge = Array.IndexOf(ageValues, GameSession.Age);
                RefreshAges();
                if (worldMapPanel != null)
                {
                    if (titlePanel != null) titlePanel.SetActive(false);
                    worldMapPanel.SetActive(true);
                }
            }
        }

        void OnEnable()
        {
            if (avatars != null)
            {
                for (int i = 0; i < avatars.Length; i++)
                {
                    int index = i;
                    if (avatars[i].button != null) avatars[i].button.onClick.AddListener(() => SelectOption(index));
                }
            }
            if (ageSlots != null)
            {
                for (int i = 0; i < ageSlots.Length; i++)
                {
                    int index = i;
                    if (ageSlots[i].button != null) ageSlots[i].button.onClick.AddListener(() => SelectAge(index));
                }
            }
            if (playButton != null) playButton.onClick.AddListener(ShowAge);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            if (parentButton != null) parentButton.onClick.AddListener(OpenParentFromTitle);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettingsFromTitle);
            if (nextButton != null) nextButton.onClick.AddListener(OpenWorldMap);
            if (backButton != null) backButton.onClick.AddListener(ShowTitle);
            if (worldMap != null) worldMap.BackPressed += ReturnToAge;
            if (worldMap != null) worldMap.SettingsPressed += OpenSettingsFromWorldMap;
            if (settings != null) settings.BackPressed += CloseSettings;
            if (worldMap != null) worldMap.WordBookPressed += OpenWordBook;
            if (wordBook != null) wordBook.BackPressed += CloseWordBook;
            if (worldMap != null) worldMap.ParentPressed += OpenParentFromWorldMap;
            if (settings != null) settings.DashboardPressed += OpenParentFromSettings;
            if (parentGate != null) { parentGate.Passed += OpenDashboard; parentGate.BackPressed += CloseParent; }
            if (dashboard != null) dashboard.ExitPressed += CloseParent;
            GameSettings.Changed += ApplySound;
        }

        void OnDisable()
        {
            if (avatars != null)
                for (int i = 0; i < avatars.Length; i++)
                    if (avatars[i].button != null) avatars[i].button.onClick.RemoveAllListeners();
            if (ageSlots != null)
                for (int i = 0; i < ageSlots.Length; i++)
                    if (ageSlots[i].button != null) ageSlots[i].button.onClick.RemoveAllListeners();
            if (playButton != null) playButton.onClick.RemoveListener(ShowAge);
            if (soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
            if (parentButton != null) parentButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (nextButton != null) nextButton.onClick.RemoveListener(OpenWorldMap);
            if (backButton != null) backButton.onClick.RemoveListener(ShowTitle);
            if (worldMap != null) worldMap.BackPressed -= ReturnToAge;
            if (worldMap != null) worldMap.SettingsPressed -= OpenSettingsFromWorldMap;
            if (settings != null) settings.BackPressed -= CloseSettings;
            if (worldMap != null) worldMap.WordBookPressed -= OpenWordBook;
            if (wordBook != null) wordBook.BackPressed -= CloseWordBook;
            if (worldMap != null) worldMap.ParentPressed -= OpenParentFromWorldMap;
            if (settings != null) settings.DashboardPressed -= OpenParentFromSettings;
            if (parentGate != null) { parentGate.Passed -= OpenDashboard; parentGate.BackPressed -= CloseParent; }
            if (dashboard != null) dashboard.ExitPressed -= CloseParent;
            GameSettings.Changed -= ApplySound;
        }

        // ------------------------------------------------------------------ step 1: title

        void ShowTitle()
        {
            if (titlePanel != null) titlePanel.SetActive(true);
            if (agePanel != null) agePanel.SetActive(false);
            if (worldMapPanel != null) worldMapPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (wordBookPanel != null) wordBookPanel.SetActive(false);
            if (parentGatePanel != null) parentGatePanel.SetActive(false);
            if (dashboardPanel != null) dashboardPanel.SetActive(false);
        }

        void SelectOption(int index)
        {
            if (index < 0 || index >= options.Length) return;
            selectedOption = index;
            RefreshAvatars();
        }

        void RefreshAvatars()
        {
            for (int i = 0; i < avatars.Length; i++)
            {
                bool isSelected = i == selectedOption;
                AvatarSlot slot = avatars[i];

                if (slot.ring != null)
                {
                    float width = isSelected ? selectedRingWidth : normalRingWidth;
                    slot.ring.color = isSelected ? selectedRingColor : normalRingColor;
                    slot.ring.rectTransform.sizeDelta = Vector2.one * (avatarSize + 2f * width);
                    slot.ring.pixelsPerUnitMultiplier = 128f / (avatarSize + 2f * width);
                }

                if (slot.nameLabel != null)
                {
                    slot.nameLabel.color = isSelected ? selectedNameColor : normalNameColor;
                    slot.nameLabel.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        // ------------------------------------------------------------------ step 2: age

        /// <summary>MAGLARO: remember the character and ask for the age.</summary>
        void ShowAge()
        {
            GameSession.Character = options[selectedOption].character;
            PlayerPrefs.SetInt(OptionKey, selectedOption);

            selectedAge = -1;
            RefreshAges();
            if (titlePanel != null) titlePanel.SetActive(false);
            if (agePanel != null) agePanel.SetActive(true);
        }

        void SelectAge(int index)
        {
            if (index < 0 || index >= ageValues.Length) return;
            selectedAge = index;
            RefreshAges();
        }

        void RefreshAges()
        {
            for (int i = 0; i < ageSlots.Length; i++)
            {
                bool isSelected = i == selectedAge;
                AgeSlot slot = ageSlots[i];
                if (slot.fill != null) slot.fill.color = isSelected ? selectedRingColor : Color.white;
                if (slot.ring != null)
                {
                    float width = isSelected ? selectedRingWidth : normalRingWidth;
                    slot.ring.color = isSelected ? selectedRingColor : normalRingColor;
                    slot.ring.rectTransform.sizeDelta = Vector2.one * (ageSize + 2f * width);
                    slot.ring.pixelsPerUnitMultiplier = 128f / (ageSize + 2f * width);
                }
                if (slot.label != null) slot.label.color = isSelected ? Color.white : selectedNameColor;
            }
            if (nextButton != null) nextButton.interactable = selectedAge >= 0;
        }

        /// <summary>Susunod: remember the age and open the world map, where a place is chosen and the game starts.</summary>
        void OpenWorldMap()
        {
            if (selectedAge < 0) return;

            GameSession.ChildName = string.Empty;
            GameSession.Age = ageValues[selectedAge];
            GameSession.Character = options[selectedOption].character;
            PlayerPrefs.Save();

            if (agePanel != null) agePanel.SetActive(false);
            if (worldMapPanel != null) worldMapPanel.SetActive(true);
        }

        /// <summary>Back from the world map to the age step (the chosen age stays selected).</summary>
        void ReturnToAge()
        {
            if (worldMapPanel != null) worldMapPanel.SetActive(false);
            if (agePanel != null) agePanel.SetActive(true);
        }

        // ------------------------------------------------------------------ sound

        void ToggleSound()
        {
            GameSettings.Muted = !GameSettings.Muted;   // also raises GameSettings.Changed, which redraws the icon
        }

        void ApplySound()
        {
            GameSettings.ApplyAudio();
            if (soundIcon != null && soundOnSprite != null && soundOffSprite != null)
                soundIcon.sprite = GameSettings.Muted ? soundOffSprite : soundOnSprite;
        }

        // ------------------------------------------------------------------ settings (from the title or from the world map)

        GameObject panelBeforeSettings;

        void OpenSettingsFromTitle()
        {
            onSettingsPressed.Invoke();
            OpenSettings(titlePanel);
        }

        void OpenSettingsFromWorldMap()
        {
            OpenSettings(worldMapPanel);
        }

        void OpenSettings(GameObject from)
        {
            if (settingsPanel == null) return;
            panelBeforeSettings = from;
            if (from != null) from.SetActive(false);
            settingsPanel.SetActive(true);
        }

        // ------------------------------------------------------------------ Parent / Teacher (from the title, the world map or the settings)

        GameObject panelBeforeParent;

        void OpenParentFromTitle()
        {
            onParentPressed.Invoke();
            OpenParent(titlePanel);
        }

        void OpenParentFromWorldMap() => OpenParent(worldMapPanel);

        void OpenParentFromSettings() => OpenParent(settingsPanel);

        void OpenParent(GameObject from)
        {
            if (dashboardPanel == null) return;
            panelBeforeParent = from;
            if (from != null) from.SetActive(false);
            if (requireParentGate && parentGatePanel != null) parentGatePanel.SetActive(true);
            else OpenDashboard();
        }

        /// <summary>The gate was passed (or there is no gate): show the dashboard.</summary>
        void OpenDashboard()
        {
            if (parentGatePanel != null) parentGatePanel.SetActive(false);
            if (dashboardPanel != null) dashboardPanel.SetActive(true);
        }

        /// <summary>Back from the gate or the dashboard to wherever it was opened.</summary>
        void CloseParent()
        {
            if (parentGatePanel != null) parentGatePanel.SetActive(false);
            if (dashboardPanel != null) dashboardPanel.SetActive(false);
            GameObject target = panelBeforeParent != null ? panelBeforeParent : titlePanel;
            if (target != null) target.SetActive(true);
        }

        // ------------------------------------------------------------------ Salita Book (from the world map)

        void OpenWordBook()
        {
            if (wordBookPanel == null) return;
            if (worldMapPanel != null) worldMapPanel.SetActive(false);
            wordBookPanel.SetActive(true);
        }

        /// <summary>Back from the Salita Book to the world map.</summary>
        void CloseWordBook()
        {
            if (wordBookPanel != null) wordBookPanel.SetActive(false);
            if (worldMapPanel != null) worldMapPanel.SetActive(true);
        }

        /// <summary>Back from the settings screen to wherever it was opened.</summary>
        void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            GameObject target = panelBeforeSettings != null ? panelBeforeSettings : titlePanel;
            if (target != null) target.SetActive(true);
            ApplySound();
        }
    }
}
