using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Hiraya
{
    /// <summary>
    /// The spelling screen (wireframe 05), shown after the quiz of a word: "Ispel ang Salita". The picture of the word, empty letter
    /// boxes and a bank of letter tiles (the letters of the word and up to two extra ones). Tap a tile to put its letter in the next
    /// box, tap a filled box to take its letter back, Burahin takes back the last letter and Suriin checks the word.
    /// A wrong word makes the boxes shake and turn red for a moment; the right word turns them green and Suriin becomes Magpatuloy.
    /// The speaker buttons only raise SpeakRequested for now; connect a voice-over later.
    /// </summary>
    public class SpellingScreen : MonoBehaviour
    {
        [Serializable]
        public class LetterBox
        {
            public Button button;
            public Image border;
            public Image fill;
            public Image dashedGray;
            public Image dashedOrange;
            public TMP_Text letter;
        }

        [Serializable]
        public class LetterTile
        {
            public Button button;
            public TMP_Text letter;
        }

        /// <summary>Raised when the word was spelled right and Magpatuloy was pressed. Passes how many wrong words were checked before.</summary>
        public event Action<int> Completed;
        /// <summary>Raised by the back arrow before the word was spelled right.</summary>
        public event Action Skipped;
        /// <summary>Raised by the speaker buttons with the text to read aloud.</summary>
        public event Action<string> SpeakRequested;

        [Header("Top bar")]
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text progress;

        [Header("Picture and hint")]
        [SerializeField] Image pictureImage;
        [SerializeField] GameObject picturePlaceholder;
        [SerializeField] Button listenButton;
        [SerializeField] TMP_Text hintText;

        [Header("Letters")]
        [SerializeField] RectTransform boxesRow;
        [Tooltip("The \"-\" shown between the boxes of a word like hapag-sulatan.")]
        [SerializeField] RectTransform dash;
        [SerializeField] LetterBox[] boxes;
        [SerializeField] LetterTile[] tiles;
        [SerializeField] TMP_Text status;

        [Header("Buttons")]
        [SerializeField] Button speakButton;
        [SerializeField] Button eraseButton;
        [SerializeField] Button submitButton;
        [SerializeField] Image submitFill;
        [SerializeField] TMP_Text submitLabel;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color submitReadyFill = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color submitWaitingFill = new Color(0.96f, 0.80f, 0.74f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] Color emptyFill = new Color(0.97f, 0.97f, 0.95f, 1f);
        [SerializeField] Color normalBorder = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color rightBorder = new Color(0.16f, 0.58f, 0.33f, 1f);
        [SerializeField] Color rightFill = new Color(0.90f, 0.97f, 0.92f, 1f);
        [SerializeField] Color wrongBorder = new Color(0.80f, 0.25f, 0.15f, 1f);
        [SerializeField] Color wrongFill = new Color(0.99f, 0.91f, 0.89f, 1f);

        const string ExtraLetters = "AEIOULNGKTSPBDMRYWH";

        WordTarget target;
        string answer = string.Empty;
        readonly List<char> tileLetters = new List<char>();
        readonly List<int> placed = new List<int>();   // tile numbers, in the order they were put in the boxes
        int wrongChecks;
        bool solved;
        bool flashing;

        /// <summary>The word as it is spelled on the screen: capital letters only, no spaces or hyphens.</summary>
        public static string Letters(string word)
        {
            var sb = new StringBuilder();
            if (word == null) return string.Empty;
            foreach (char c in word)
                if (char.IsLetter(c)) sb.Append(char.ToUpperInvariant(c));
            return sb.ToString();
        }

        /// <summary>True if the word can be spelled here: at least one letter and not more letters than there are boxes.</summary>
        public bool CanSpell(string word)
        {
            int n = Letters(word).Length;
            return n >= 1 && boxes != null && n <= boxes.Length;
        }

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (listenButton != null) listenButton.onClick.AddListener(OnListen);
            if (speakButton != null) speakButton.onClick.AddListener(OnSpeakQuestion);
            if (eraseButton != null) eraseButton.onClick.AddListener(OnErase);
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
            if (boxes != null)
            {
                for (int i = 0; i < boxes.Length; i++)
                {
                    int index = i;
                    if (boxes[i].button != null) boxes[i].button.onClick.AddListener(() => OnBox(index));
                }
            }
            if (tiles != null)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    int index = i;
                    if (tiles[i].button != null) tiles[i].button.onClick.AddListener(() => OnTile(index));
                }
            }
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (listenButton != null) listenButton.onClick.RemoveListener(OnListen);
            if (speakButton != null) speakButton.onClick.RemoveListener(OnSpeakQuestion);
            if (eraseButton != null) eraseButton.onClick.RemoveListener(OnErase);
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
            if (boxes != null)
                for (int i = 0; i < boxes.Length; i++)
                    if (boxes[i].button != null) boxes[i].button.onClick.RemoveAllListeners();
            if (tiles != null)
                for (int i = 0; i < tiles.Length; i++)
                    if (tiles[i].button != null) tiles[i].button.onClick.RemoveAllListeners();
            flashing = false;
        }

        // ------------------------------------------------------------------ open

        /// <summary>Opens the screen for a word. position is its place in the word set (0 = first), total the number of words.</summary>
        public void Open(WordTarget word, int position, int total)
        {
            target = word;
            answer = Letters(word.Word);
            wrongChecks = 0;
            solved = false;
            flashing = false;
            placed.Clear();
            gameObject.SetActive(true);

            if (subtitle != null) subtitle.text = Loc.PlaceName(GameSession.Place);
            if (progress != null) progress.text = Loc.T("Word " + (position + 1) + " of " + total, "Salita " + (position + 1) + " sa " + total);
            if (hintText != null)
            {
                hintText.text = word.Hint;
                hintText.transform.parent.gameObject.SetActive(word.Hint.Length > 0);   // the whole clue card
            }

            bool hasPicture = word.Picture != null;
            if (pictureImage != null) { pictureImage.sprite = word.Picture; pictureImage.gameObject.SetActive(hasPicture); }
            if (picturePlaceholder != null) picturePlaceholder.SetActive(!hasPicture);

            // The tiles: every letter of the word plus up to two extra letters, mixed up.
            tileLetters.Clear();
            foreach (char c in answer) tileLetters.Add(c);
            int extras = answer.Length >= 10 ? 0 : Mathf.Clamp(tiles.Length - answer.Length, 0, 2);   // long words: no extra letters
            for (int i = 0; i < extras; i++)
            {
                char extra;
                int guard = 0;
                do { extra = ExtraLetters[Random.Range(0, ExtraLetters.Length)]; guard++; }
                while ((answer.IndexOf(extra) >= 0 || tileLetters.Contains(extra)) && guard < 50);
                tileLetters.Add(extra);
            }
            for (int i = tileLetters.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                char swap = tileLetters[i];
                tileLetters[i] = tileLetters[j];
                tileLetters[j] = swap;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                bool used = i < tileLetters.Count;
                tiles[i].button.gameObject.SetActive(used);
                if (used) tiles[i].letter.text = tileLetters[i].ToString();
            }
            for (int i = 0; i < boxes.Length; i++) boxes[i].button.gameObject.SetActive(i < answer.Length);
            int hyphenAfter = LettersBeforeHyphen(word.Word);
            if (dash != null)
            {
                dash.gameObject.SetActive(hyphenAfter > 0);
                dash.SetAsLastSibling();
                if (hyphenAfter > 0) dash.SetSiblingIndex(hyphenAfter);
            }
            float dashWidth = dash != null && hyphenAfter > 0 ? dash.rect.width : 0f;
            if (boxes.Length > 0) FitRow(boxesRow, (RectTransform)boxes[0].button.transform, answer.Length, dashWidth);
            if (tiles.Length > 0) FitRow(tiles[0].button.transform.parent as RectTransform, (RectTransform)tiles[0].button.transform, tileLetters.Count, 0f);

            Refresh(string.Empty);
        }

        static int LettersBeforeHyphen(string word)
        {
            int at = word == null ? -1 : word.IndexOf('-');
            return at <= 0 ? 0 : Letters(word.Substring(0, at)).Length;
        }

        /// <summary>A long word gets closer boxes, and the whole row is made smaller if it still does not fit.</summary>
        static void FitRow(RectTransform row, RectTransform item, int count, float extra)
        {
            if (row == null || item == null || count <= 0) return;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            float gap = count > 10 ? 12f : 22f;
            if (layout != null) layout.spacing = gap;
            float needed = count * item.rect.width + (count - 1) * gap + (extra > 0f ? extra + gap : 0f);
            float scale = needed > row.rect.width && needed > 0f ? row.rect.width / needed : 1f;
            row.localScale = new Vector3(scale, scale, 1f);
        }

        // ------------------------------------------------------------------ drawing

        string Spelled()
        {
            var sb = new StringBuilder();
            foreach (int tile in placed) sb.Append(tileLetters[tile]);
            return sb.ToString();
        }

        void Refresh(string message)
        {
            // Boxes: a filled box shows its letter; the next empty box is outlined in orange.
            for (int i = 0; i < answer.Length; i++)
            {
                LetterBox box = boxes[i];
                bool filled = i < placed.Count;
                bool next = !solved && i == placed.Count;
                box.letter.text = filled ? tileLetters[placed[i]].ToString() : string.Empty;
                box.letter.color = ink;
                box.border.gameObject.SetActive(filled);
                box.dashedGray.gameObject.SetActive(!filled && !next);
                box.dashedOrange.gameObject.SetActive(next);
                box.border.color = solved ? rightBorder : normalBorder;
                box.fill.color = solved ? rightFill : (filled ? Color.white : emptyFill);
            }

            // Tiles: a tile that is in a box leaves the bank.
            for (int i = 0; i < tileLetters.Count; i++)
                tiles[i].button.gameObject.SetActive(!placed.Contains(i) && !solved);

            if (status != null)
            {
                status.text = message;
                status.color = solved ? rightBorder : orange;
            }

            bool complete = placed.Count == answer.Length;
            if (eraseButton != null) eraseButton.gameObject.SetActive(!solved);
            if (submitFill != null) submitFill.color = (complete || solved) ? submitReadyFill : submitWaitingFill;
            if (submitLabel != null) submitLabel.text = solved ? Loc.T("Continue", "Magpatuloy") : Loc.T("Check", "Suriin");
        }

        // ------------------------------------------------------------------ taps

        void OnTile(int index)
        {
            if (solved || flashing || index < 0 || index >= tileLetters.Count || placed.Contains(index)) return;
            if (placed.Count >= answer.Length) return;
            placed.Add(index);
            Refresh(string.Empty);
        }

        void OnBox(int index)
        {
            if (solved || flashing || index < 0 || index >= placed.Count) return;
            placed.RemoveAt(index);   // the letters after it move one box to the left
            Refresh(string.Empty);
        }

        void OnErase()
        {
            if (solved || flashing || placed.Count == 0) return;
            placed.RemoveAt(placed.Count - 1);
            Refresh(string.Empty);
        }

        void OnSubmit()
        {
            if (flashing) return;
            if (solved)
            {
                Completed?.Invoke(wrongChecks);
                return;
            }
            if (placed.Count < answer.Length)
            {
                Refresh(Loc.T("Fill all the boxes first.", "Kumpletuhin muna ang mga kahon."));
                return;
            }

            if (string.Equals(Spelled(), answer, StringComparison.Ordinal))
            {
                solved = true;
                Refresh(Loc.T("Correct! ", "Tama! ") + Loc.Capitalize(target.Word));
                SpeakRequested?.Invoke(target.Word);
            }
            else
            {
                wrongChecks++;
                StartCoroutine(WrongFlash());
            }
        }

        IEnumerator WrongFlash()
        {
            flashing = true;
            Refresh(Loc.T("Try again!", "Subukan muli!"));
            for (int i = 0; i < answer.Length; i++)
            {
                boxes[i].border.color = wrongBorder;
                boxes[i].fill.color = wrongFill;
            }

            Vector2 home = boxesRow != null ? boxesRow.anchoredPosition : Vector2.zero;
            const float duration = 0.6f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (boxesRow != null) boxesRow.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 14f * (1f - t / duration), 0f);
                yield return null;
            }
            if (boxesRow != null) boxesRow.anchoredPosition = home;

            flashing = false;
            Refresh(Loc.T("Try again!", "Subukan muli!"));
        }

        void OnBack()
        {
            if (solved) Completed?.Invoke(wrongChecks);
            else Skipped?.Invoke();
        }

        void OnListen()
        {
            if (target != null) SpeakRequested?.Invoke(target.Word);
        }

        void OnSpeakQuestion()
        {
            SpeakRequested?.Invoke(Loc.T("What word is in the picture?", "Anong salita ang nasa larawan?"));
        }
    }
}
