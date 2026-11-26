using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The word book (wireframe 08): the words of one place at a time, learned ones open and the others locked, with a button
    /// for each place ("School 0/9") and a "n / 9 words" counter. Places marked as locked in the WordCatalog (or without words)
    /// cannot be opened yet. It is opened from the world map card and the back arrow returns there. The cards are built from the
    /// WordCard prefab each time the book opens, so a word that was just learned is already open.
    /// The speaker button of a card plays the recording of the word.
    /// </summary>
    public class SalitaBookController : MonoBehaviour
    {
        [Serializable]
        public class PlaceChip
        {
            public Button button;
            public Image fill;
            public TMP_Text label;
            public GameObject lockIcon;
            public CanvasGroup group;
        }

        public event Action BackPressed;
        public event Action<string> SpeakRequested;

        [Header("Words")]
        [SerializeField] WordCatalog catalog;
        [SerializeField] WordCardView cardPrefab;
        [Tooltip("The grid inside the scroll view that the cards are placed in.")]
        [SerializeField] RectTransform cardParent;
        [SerializeField] ScrollRect scroll;
        [SerializeField] UnityEvent<string> onSpeak;

        [Header("Top bar")]
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text counter;

        [Header("Place buttons, one for each place of the catalog")]
        [SerializeField] PlaceChip[] chips;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);

        readonly List<WordCardView> cards = new List<WordCardView>();
        int selected;

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (chips != null)
            {
                for (int i = 0; i < chips.Length; i++)
                {
                    int index = i;
                    if (chips[i].button != null) chips[i].button.onClick.AddListener(() => Select(index));
                }
            }
            Rebuild();
            Select(StartPlace());
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (chips != null)
                for (int i = 0; i < chips.Length; i++)
                    if (chips[i].button != null) chips[i].button.onClick.RemoveAllListeners();
            AudioManager.StopVoice();
        }

        void OnBack() => BackPressed?.Invoke();

        bool IsOpen(int index)
        {
            if (catalog == null || index < 0 || index >= catalog.Places.Count) return false;
            WordCatalog.Place place = catalog.Places[index];
            catalog.CountPlace(place.id, out _, out int total);
            return !place.locked && total > 0;
        }

        /// <summary>The place picked on the world map if it is open, otherwise the first open place.</summary>
        int StartPlace()
        {
            if (catalog == null) return 0;
            for (int i = 0; i < catalog.Places.Count; i++)
                if (IsOpen(i) && WordCatalog.SamePlace(catalog.Places[i].id, GameSession.Place)) return i;
            for (int i = 0; i < catalog.Places.Count; i++)
                if (IsOpen(i)) return i;
            return 0;
        }

        // ------------------------------------------------------------------ the cards

        void Rebuild()
        {
            foreach (WordCardView card in cards)
            {
                if (card == null) continue;
                card.SpeakPressed -= OnCardSpeak;
                card.gameObject.SetActive(false);   // Destroy only happens at the end of the frame; hide it so the grid ignores it right away
                Destroy(card.gameObject);
            }
            cards.Clear();
            if (catalog == null || cardPrefab == null || cardParent == null) return;

            foreach (WordCatalog.Entry entry in catalog.Words)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.word)) continue;
                WordCardView card = Instantiate(cardPrefab, cardParent, false);
                card.name = "Card_" + entry.word;
                card.Setup(entry, GameProgress.IsWordLearned(entry.word));
                card.SpeakPressed += OnCardSpeak;
                cards.Add(card);
            }
        }

        void OnCardSpeak(string word)
        {
            WordCatalog.Entry entry = catalog != null ? catalog.Find(word) : null;
            if (entry != null) AudioManager.PlayVoice(entry.sound);
            onSpeak.Invoke(word);
            SpeakRequested?.Invoke(word);
        }

        // ------------------------------------------------------------------ places

        /// <summary>Shows the words of one place. A locked place cannot be selected.</summary>
        void Select(int index)
        {
            if (catalog == null) return;
            if (index < 0 || index >= catalog.Places.Count) return;
            if (!IsOpen(index) && index != StartPlace()) return;
            selected = index;
            string place = catalog.Places[index].id;

            foreach (WordCardView card in cards) card.gameObject.SetActive(WordCatalog.SamePlace(card.Place, place));

            if (chips != null)
            {
                for (int i = 0; i < chips.Length; i++)
                {
                    PlaceChip chip = chips[i];
                    bool used = i < catalog.Places.Count;
                    if (chip.button != null) chip.button.gameObject.SetActive(used);
                    if (!used) continue;

                    string id = catalog.Places[i].id;
                    bool open = IsOpen(i);
                    catalog.CountPlace(id, out int learnedThere, out int totalThere);
                    if (chip.label != null)
                    {
                        chip.label.text = open ? Loc.PlaceName(id) + "  " + learnedThere + "/" + totalThere : Loc.PlaceName(id);
                        chip.label.color = i == selected ? Color.white : ink;
                    }
                    if (chip.fill != null) chip.fill.color = i == selected ? orange : Color.white;
                    if (chip.lockIcon != null) chip.lockIcon.SetActive(!open);
                    if (chip.group != null) chip.group.alpha = open ? 1f : 0.5f;
                    if (chip.button != null) chip.button.interactable = open;
                }
            }

            catalog.CountPlace(place, out int learned, out int total);
            if (counter != null) counter.text = Loc.T(learned + " / " + total + " words", learned + " / " + total + " salita");

            if (cardParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cardParent);
            if (chips != null && chips.Length > 0 && chips[0].button != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chips[0].button.transform.parent);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }
    }
}
