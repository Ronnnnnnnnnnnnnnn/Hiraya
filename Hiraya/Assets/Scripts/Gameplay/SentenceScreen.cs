using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Hiraya
{
    /// <summary>
    /// Fill in the blank, for children of 6 and up, after the quiz and the spelling of a word: a Filipino sentence with a gap
    /// and three words to choose from. A wrong word turns red and can be tried again; the right one goes into the gap. The Hint
    /// button shows the sentence in English. The sentences are in the WordCatalog (Blank, Blank English, Blank Wrong).
    /// Lives on the "SentencePanel" of the GameplayUI prefab.
    /// </summary>
    public class SentenceScreen : MonoBehaviour
    {
        /// <summary>How the gap is written in the word list.</summary>
        public const string Gap = "___";

        [Serializable]
        public class Choice
        {
            public Button button;
            public Image fill;
            public Image border;
            public TMP_Text label;
        }

        /// <summary>Done: how many words were tapped until the right one (1 = right away).</summary>
        public event Action<int> Completed;
        /// <summary>Left with the back arrow before the right word was found.</summary>
        public event Action Skipped;
        /// <summary>The right word was found: it can be said aloud.</summary>
        public event Action SpeakRequested;

        [Header("Texts")]
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text progress;
        [SerializeField] TMP_Text sentenceText;
        [SerializeField] TMP_Text hintText;
        [SerializeField] TMP_Text status;

        [Header("Buttons")]
        [SerializeField] Button backButton;
        [SerializeField] Button hintButton;
        [SerializeField] Button nextButton;
        [SerializeField] Choice[] choices;

        [Header("Colours")]
        [SerializeField] Color gapColor = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color rightBorder = new Color(0.16f, 0.58f, 0.33f, 1f);
        [SerializeField] Color rightFill = new Color(0.90f, 0.97f, 0.92f, 1f);
        [SerializeField] Color wrongBorder = new Color(0.80f, 0.25f, 0.15f, 1f);
        [SerializeField] Color wrongFill = new Color(0.99f, 0.91f, 0.89f, 1f);
        [SerializeField] Color normalBorder = new Color(0.85f, 0.85f, 0.83f, 1f);

        WordCatalog.Entry entry;
        readonly List<string> offered = new List<string>();
        int tries;
        bool solved;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>True when the word list has a sentence with a gap and at least one other word for it.</summary>
        public static bool CanAsk(WordCatalog.Entry listed)
        {
            if (listed == null || string.IsNullOrWhiteSpace(listed.blank) || !listed.blank.Contains(Gap) || listed.blankWrong == null) return false;
            foreach (string other in listed.blankWrong)
                if (!string.IsNullOrWhiteSpace(other)) return true;
            return false;
        }

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (hintButton != null) hintButton.onClick.AddListener(OnHint);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (choices != null)
            {
                for (int i = 0; i < choices.Length; i++)
                {
                    int index = i;
                    if (choices[i].button != null) choices[i].button.onClick.AddListener(() => Choose(index));
                }
            }
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (hintButton != null) hintButton.onClick.RemoveListener(OnHint);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (choices != null)
                foreach (Choice choice in choices)
                    if (choice.button != null) choice.button.onClick.RemoveAllListeners();
        }

        /// <summary>Opens the exercise of a word. position is its place in the word set (0 = first), place the line under the title.</summary>
        public void Open(WordCatalog.Entry listed, int position, int total, string place)
        {
            entry = listed;
            tries = 0;
            solved = false;
            gameObject.SetActive(true);

            if (title != null) title.text = Loc.T("Fill in the Blank", "Punan ang Patlang");
            if (subtitle != null) subtitle.text = place;
            if (progress != null) progress.text = Loc.T("Word " + (position + 1) + " of " + total, "Salita " + (position + 1) + " sa " + total);
            if (status != null) status.text = string.Empty;
            if (hintText != null) { hintText.text = listed.blankEnglish; hintText.gameObject.SetActive(false); }
            if (hintButton != null) hintButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(listed.blankEnglish));
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            ShowSentence();

            // The right word and the others, in a random order.
            offered.Clear();
            offered.Add(listed.word);
            foreach (string other in listed.blankWrong)
                if (!string.IsNullOrWhiteSpace(other) && offered.Count < choices.Length) offered.Add(other.Trim());
            for (int i = offered.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                string swap = offered[i];
                offered[i] = offered[j];
                offered[j] = swap;
            }

            for (int i = 0; i < choices.Length; i++)
            {
                Choice choice = choices[i];
                bool used = i < offered.Count;
                if (choice.button != null) { choice.button.gameObject.SetActive(used); choice.button.interactable = true; }
                if (!used) continue;
                if (choice.label != null) choice.label.text = offered[i];
                Paint(choice, Color.white, normalBorder);
            }
        }

        public void Close() => gameObject.SetActive(false);

        void ShowSentence()
        {
            if (sentenceText == null || entry == null) return;
            string colour = "#" + ColorUtility.ToHtmlStringRGB(solved ? rightBorder : gapColor);
            string filling = solved ? "<u>" + entry.word + "</u>" : "______";
            sentenceText.text = entry.blank.Replace(Gap, "<color=" + colour + ">" + filling + "</color>");
        }

        void Choose(int index)
        {
            if (solved || entry == null || index < 0 || index >= offered.Count) return;
            tries++;
            Choice choice = choices[index];

            if (!string.Equals(offered[index], entry.word, StringComparison.OrdinalIgnoreCase))
            {
                Paint(choice, wrongFill, wrongBorder);
                if (choice.button != null) choice.button.interactable = false;
                if (status != null) { status.text = Loc.T("Try again!", "Subukan muli!"); status.color = wrongBorder; }
                return;
            }

            solved = true;
            Paint(choice, rightFill, rightBorder);
            foreach (Choice other in choices)
                if (other.button != null) other.button.interactable = false;
            if (status != null) { status.text = Loc.T("Correct!", "Tama!"); status.color = rightBorder; }
            if (hintText != null) hintText.gameObject.SetActive(false);
            if (hintButton != null) hintButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            ShowSentence();
            SpeakRequested?.Invoke();
        }

        static void Paint(Choice choice, Color fill, Color border)
        {
            if (choice.fill != null) choice.fill.color = fill;
            if (choice.border != null) choice.border.color = border;
        }

        void OnHint()
        {
            if (hintText != null) hintText.gameObject.SetActive(true);
        }

        void OnBack()
        {
            if (solved) Completed?.Invoke(tries);
            else Skipped?.Invoke();
        }

        void OnNext()
        {
            if (solved) Completed?.Invoke(tries);
        }
    }
}
