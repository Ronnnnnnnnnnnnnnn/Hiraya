using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>One place on the world map. Edit the list in the Inspector (WorldMapPanel > Places).</summary>
    [Serializable]
    public class WorldPlace
    {
        public string id;
        public string displayName;
        [Tooltip("Name of the building object in the playing scene. The player starts in front of it. Empty if the place is not built yet.")]
        public string buildingName;
        [Tooltip("Stars needed (added up over all places) before this place opens.")]
        public int requiredStars;
        [Tooltip("Tick to keep a place locked (not built yet, or it has no words yet), whatever the stars.")]
        public bool comingSoon;
    }

    /// <summary>
    /// The world map screen (wireframe 02): choose a place, see the stars, press play.
    /// The Settings, Salita Book, Pahinga and Parent buttons only raise events for now; connect those screens in the Inspector.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Serializable]
        public class PlaceCard
        {
            [Header("Card in the list")]
            public CanvasGroup group;
            public Button button;
            public Image border;
            public Image fill;
            public TMP_Text status;
            public TMP_Text starsText;
            public Image starIcon;
            public Image lockIcon;
            public GameObject playPill;
            public Button playPillButton;

            [Header("Marker on the map (empty if the place is not built)")]
            public GameObject markerRoot;
            public Button markerButton;
            public Image markerFill;
            public Image markerIcon;
            public Sprite markerLockSprite;
            public Sprite markerPlaceSprite;
        }

        public event Action BackPressed;
        public event Action SettingsPressed;
        /// <summary>Raised by the "Parent" button. TitleScreenController opens the parent dashboard on it.</summary>
        public event Action ParentPressed;
        /// <summary>Raised by the "Aking Salita Book" card. TitleScreenController opens the Salita Book on it.</summary>
        public event Action WordBookPressed;

        [SerializeField] WorldPlace[] places =
        {
            new WorldPlace { id = "tahanan", displayName = "Tahanan", buildingName = "Building_House_2", requiredStars = 0 },
            new WorldPlace { id = "palengke", displayName = "Palengke", buildingName = "", requiredStars = 0, comingSoon = true },
            new WorldPlace { id = "paaralan", displayName = "Paaralan", buildingName = "Building_School", requiredStars = 0 },
            new WorldPlace { id = "hardin", displayName = "Hardin", buildingName = "Building_Park", requiredStars = 6, comingSoon = true },   // no words yet
            new WorldPlace { id = "bundok", displayName = "Bundok", buildingName = "", requiredStars = 12, comingSoon = true }
        };
        [SerializeField] PlaceCard[] cards;
        [SerializeField] string defaultPlaceId = "paaralan";

        [Header("Top bar")]
        [SerializeField] TMP_Text profileName;
        [SerializeField] Image profilePortrait;
        [SerializeField] Sprite boyPortrait;
        [SerializeField] Sprite girlPortrait;
        [SerializeField] TMP_Text starTotal;
        [SerializeField] Button backButton;
        [SerializeField] Button settingsButton;

        [Header("Places card")]
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text progressValue;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Button wordBookButton;
        [SerializeField] TMP_Text wordBookInfo;
        [SerializeField] int totalWords = 40;
        [Tooltip("The word list: the count of the Salita Book card comes from it.")]
        [SerializeField] WordCatalog catalog;
        [SerializeField] Button restButton;
        [SerializeField] Button parentButton;

        [Header("Bottom bar")]
        [SerializeField] TMP_Text nextPlaceText;
        [SerializeField] Button playButton;

        [Header("Connect the other screens here")]
        [SerializeField] UnityEvent onSettings;
        [SerializeField] UnityEvent onWordBook;
        [SerializeField] UnityEvent onRest;
        [SerializeField] UnityEvent onParent;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color normalBorder = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color normalFill = new Color(0.98f, 0.98f, 0.96f, 1f);
        [SerializeField] Color selectedFill = new Color(1f, 0.93f, 0.89f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] Color softInk = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] Color warning = new Color(0.80f, 0.25f, 0.15f, 1f);

        int selected = -1;

        void OnEnable()
        {
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    int index = i;
                    if (cards[i].button != null) cards[i].button.onClick.AddListener(() => Select(index));
                    if (cards[i].markerButton != null) cards[i].markerButton.onClick.AddListener(() => Select(index));
                    if (cards[i].playPillButton != null) cards[i].playPillButton.onClick.AddListener(StartGame);
                }
            }
            if (playButton != null) playButton.onClick.AddListener(StartGame);
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (wordBookButton != null) wordBookButton.onClick.AddListener(OnWordBookClicked);
            if (restButton != null) restButton.onClick.AddListener(onRest.Invoke);
            if (parentButton != null) parentButton.onClick.AddListener(OnParentClicked);

            Refresh();
        }

        void OnDisable()
        {
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i].button != null) cards[i].button.onClick.RemoveAllListeners();
                    if (cards[i].markerButton != null) cards[i].markerButton.onClick.RemoveAllListeners();
                    if (cards[i].playPillButton != null) cards[i].playPillButton.onClick.RemoveAllListeners();
                }
            }
            if (playButton != null) playButton.onClick.RemoveListener(StartGame);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (wordBookButton != null) wordBookButton.onClick.RemoveAllListeners();
            if (restButton != null) restButton.onClick.RemoveAllListeners();
            if (parentButton != null) parentButton.onClick.RemoveAllListeners();
        }

        void Back()
        {
            BackPressed?.Invoke();
        }

        void OnSettingsClicked()
        {
            onSettings.Invoke();
            SettingsPressed?.Invoke();
        }

        void OnParentClicked()
        {
            onParent.Invoke();
            ParentPressed?.Invoke();
        }

        void OnWordBookClicked()
        {
            onWordBook.Invoke();
            WordBookPressed?.Invoke();
        }

        // ------------------------------------------------------------------ what is open

        int TotalStars()
        {
            int total = 0;
            foreach (WorldPlace place in places) total += GameProgress.GetStars(place.id);
            return total;
        }

        bool IsPlayable(WorldPlace place, int totalStars)
        {
            return !place.comingSoon && totalStars >= place.requiredStars;
        }

        void Select(int index)
        {
            if (index < 0 || index >= places.Length) return;
            if (!IsPlayable(places[index], TotalStars())) return;
            selected = index;
            Refresh();
        }

        int DefaultSelection(int totalStars)
        {
            for (int i = 0; i < places.Length; i++)
                if (places[i].id == defaultPlaceId && IsPlayable(places[i], totalStars)) return i;
            for (int i = 0; i < places.Length; i++)
                if (IsPlayable(places[i], totalStars)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ drawing

        void Refresh()
        {
            int total = TotalStars();
            int maxStars = places.Length * GameProgress.MaxStarsPerPlace;
            int unlocked = 0;
            foreach (WorldPlace place in places) if (IsPlayable(place, total)) unlocked++;

            if (selected < 0 || selected >= places.Length || !IsPlayable(places[selected], total))
                selected = DefaultSelection(total);

            // Top bar: the child who is playing.
            bool girl = GameSession.Character == CharacterChoice.Fiona;
            if (profileName != null) profileName.text = girl ? "Girl" : "Boy";
            if (profilePortrait != null) profilePortrait.sprite = girl ? girlPortrait : boyPortrait;
            if (starTotal != null) starTotal.text = total.ToString();

            // Places card.
            if (subtitle != null) subtitle.text = Loc.T(unlocked + " of " + places.Length + " unlocked · choose a place to play", unlocked + " sa " + places.Length + " na-unlock · piliin ang lugar na lalaruin");
            if (progressValue != null) progressValue.text = total + " / " + maxStars;
            if (progressFill != null)
            {
                float ratio = maxStars > 0 ? Mathf.Clamp01(total / (float)maxStars) : 0f;
                progressFill.anchorMax = new Vector2(ratio, 1f);
                progressFill.gameObject.SetActive(ratio > 0f);
            }
            if (wordBookInfo != null)
            {
                int learned = catalog != null ? catalog.LearnedCount() : GameProgress.WordsLearned;
                int total2 = catalog != null ? catalog.Count : totalWords;
                wordBookInfo.text = Loc.T(learned + " of " + total2 + " words", learned + " sa " + total2 + " salita");
            }

            for (int i = 0; i < cards.Length && i < places.Length; i++) RefreshCard(cards[i], places[i], i == selected, total);

            // Bottom bar.
            if (nextPlaceText != null)
                nextPlaceText.text = selected >= 0 ? Loc.PlaceName(places[selected].id) + Loc.T(" — New Words", " — Mga Bagong Salita") : Loc.T("No place is open", "Walang bukas na lugar");
            if (playButton != null) playButton.interactable = selected >= 0;
        }

        void RefreshCard(PlaceCard card, WorldPlace place, bool isSelected, int totalStars)
        {
            bool playable = IsPlayable(place, totalStars);
            bool lockedForStars = !place.comingSoon && !playable;
            int stars = GameProgress.GetStars(place.id);

            if (card.group != null) card.group.alpha = place.comingSoon ? 0.55f : 1f;
            if (card.border != null) card.border.color = isSelected ? orange : normalBorder;
            if (card.fill != null) card.fill.color = isSelected ? selectedFill : normalFill;

            if (card.status != null)
            {
                if (isSelected) { card.status.text = Loc.T("Current place", "Kasalukuyang lugar"); card.status.color = orange; }
                else if (place.comingSoon) { card.status.text = Loc.T("Locked", "Naka-lock"); card.status.color = softInk; }
                else if (lockedForStars) { card.status.text = Loc.T("Needs " + place.requiredStars + " stars", "Kailangan ng " + place.requiredStars + " bituin"); card.status.color = warning; }
                else if (stars >= GameProgress.MaxStarsPerPlace) { card.status.text = Loc.T("Finished", "Tapos na"); card.status.color = softInk; }
                else { card.status.text = Loc.T("Play", "Maglaro"); card.status.color = softInk; }
            }

            bool showStars = playable && !isSelected;
            if (card.starsText != null)
            {
                card.starsText.gameObject.SetActive(showStars);
                card.starsText.text = stars + " / " + GameProgress.MaxStarsPerPlace;
            }
            if (card.starIcon != null) card.starIcon.gameObject.SetActive(showStars);
            if (card.lockIcon != null) card.lockIcon.gameObject.SetActive(lockedForStars);
            if (card.playPill != null) card.playPill.SetActive(isSelected);

            if (card.markerRoot != null)
            {
                if (card.markerFill != null) card.markerFill.color = isSelected ? orange : Color.white;
                if (card.markerIcon != null)
                {
                    // The place pictures are white and get tinted; the lock picture is already dark.
                    card.markerIcon.sprite = playable ? card.markerPlaceSprite : card.markerLockSprite;
                    card.markerIcon.color = (!playable || isSelected) ? Color.white : new Color(0.25f, 0.25f, 0.27f, 1f);
                }
            }
        }

        // ------------------------------------------------------------------ play

        void StartGame()
        {
            if (selected < 0 || !IsPlayable(places[selected], TotalStars())) return;

            GameSession.Attempt = 1;
            GameSession.Place = places[selected].id;
            GameSession.PlaceBuilding = places[selected].buildingName;
            PlayerPrefs.Save();

            SceneManager.LoadScene(GameSession.SceneForAge(GameSession.Age));
        }
    }
}
