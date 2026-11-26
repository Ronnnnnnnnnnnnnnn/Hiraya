using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The results screen (wireframe 07), shown when every word of a place has been answered:
    /// "Magaling, Fin!", the stars, three numbers (Tamang sagot / Bagong salita / Pagsubok), the words that were learned
    /// and the buttons Uliting (play the place again) and Susunod (back to the world map).
    /// A chip speaks its word: it only raises SpeakRequested for now, so connect a voice-over later.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Serializable]
        public class WordChip
        {
            public GameObject root;
            public Button button;
            public TMP_Text label;
        }

        public event Action ReplayRequested;
        public event Action NextRequested;
        public event Action<string> SpeakRequested;

        [Header("Heading")]
        [SerializeField] TMP_Text caption;
        [SerializeField] TMP_Text heading;
        [SerializeField] TMP_Text subtitle;

        [Header("Stars")]
        [SerializeField] Image[] stars;
        [SerializeField] TMP_Text starsLabel;

        [Header("Numbers")]
        [SerializeField] TMP_Text correctValue;
        [SerializeField] TMP_Text newWordsValue;
        [SerializeField] TMP_Text attemptsValue;

        [Header("Words learned")]
        [SerializeField] RectTransform chipRow;
        [SerializeField] WordChip[] chips;
        [SerializeField] GameObject moreChip;
        [SerializeField] TMP_Text moreLabel;

        [Header("Buttons")]
        [SerializeField] Button replayButton;
        [SerializeField] Button nextButton;

        [Header("Look")]
        [SerializeField] Color filledStar = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color emptyStar = new Color(0.85f, 0.85f, 0.83f, 1f);

        readonly List<string> shownWords = new List<string>();

        void OnEnable()
        {
            if (replayButton != null) replayButton.onClick.AddListener(OnReplay);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (chips != null)
            {
                for (int i = 0; i < chips.Length; i++)
                {
                    int index = i;
                    if (chips[i].button != null) chips[i].button.onClick.AddListener(() => OnChip(index));
                }
            }
        }

        void OnDisable()
        {
            if (replayButton != null) replayButton.onClick.RemoveListener(OnReplay);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (chips != null)
                for (int i = 0; i < chips.Length; i++)
                    if (chips[i].button != null) chips[i].button.onClick.RemoveAllListeners();
        }

        void OnReplay() => ReplayRequested?.Invoke();

        void OnNext() => NextRequested?.Invoke();

        void OnChip(int index)
        {
            if (index >= 0 && index < shownWords.Count) SpeakRequested?.Invoke(shownWords[index]);
        }

        /// <summary>Opens the screen and fills it in.</summary>
        /// <param name="correct">Words answered right at the first try.</param>
        /// <param name="total">Words in the place.</param>
        /// <param name="newWords">Words that are new in the Salita Book.</param>
        /// <param name="attempt">How many times the place was played (1 = first time).</param>
        /// <param name="words">All the words of the place.</param>
        public void Show(string captionText, string headingText, string subtitleText, int starCount,
                         int correct, int total, int newWords, int attempt, IList<string> words)
        {
            gameObject.SetActive(true);

            if (caption != null) caption.text = captionText;
            if (heading != null) heading.text = headingText;
            if (subtitle != null) subtitle.text = subtitleText;

            starCount = Mathf.Clamp(starCount, 0, GameProgress.MaxStarsPerPlace);
            if (stars != null)
                for (int i = 0; i < stars.Length; i++)
                    if (stars[i] != null) stars[i].color = i < starCount ? filledStar : emptyStar;
            if (starsLabel != null) starsLabel.text = Loc.T(starCount + " of " + GameProgress.MaxStarsPerPlace + " stars", starCount + " sa " + GameProgress.MaxStarsPerPlace + " bituin");

            if (correctValue != null) correctValue.text = correct + Loc.T(" of ", " sa ") + total;
            if (newWordsValue != null) newWordsValue.text = newWords.ToString();
            if (attemptsValue != null) attemptsValue.text = attempt.ToString();

            // The first words get a chip each; the rest are counted in the dashed chip.
            shownWords.Clear();
            int slots = chips != null ? chips.Length : 0;
            int count = words != null ? words.Count : 0;
            for (int i = 0; i < slots; i++)
            {
                bool used = i < count;
                chips[i].root.SetActive(used);
                if (!used) continue;
                chips[i].label.text = words[i];
                shownWords.Add(words[i]);
            }
            int more = Mathf.Max(0, count - slots);
            if (moreChip != null) moreChip.SetActive(more > 0);
            if (moreLabel != null) moreLabel.text = Loc.T("+ " + more + " more words in the Word Book", "+ " + more + " pang salita sa Salita Book");
            if (chipRow != null) LayoutRebuilder.ForceRebuildLayoutImmediate(chipRow);
        }
    }
}
