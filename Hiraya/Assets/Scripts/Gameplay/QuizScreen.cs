using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Hiraya
{
    /// <summary>
    /// The quiz screen (wireframe 04): "Ano ang tawag dito?", the picture of the object and three answer cards.
    /// A wrong card turns red and cannot be tapped again, the right card turns green, then "Magpatuloy" goes back to the world.
    /// A card is marked with a check or a cross as well as its colour, so it does not depend on colour alone.
    /// The speaker buttons only raise SpeakRequested for now; connect a voice-over to it later.
    /// </summary>
    public class QuizScreen : MonoBehaviour
    {
        [Serializable]
        public class ChoiceCard
        {
            public Button button;
            public CanvasGroup group;
            public Image border;
            public Image fill;
            public Image picture;
            public TMP_Text word;
            public GameObject badge;
            public Image badgeCircle;
            public Image badgeIcon;
        }

        /// <summary>Raised when the right card was tapped. Passes how many wrong cards were tapped before it.</summary>
        public event Action<int> Answered;
        /// <summary>Raised by "Magpatuloy" (or the back arrow after the right answer).</summary>
        public event Action Finished;
        /// <summary>Raised by the back arrow before the question was answered.</summary>
        public event Action Cancelled;
        /// <summary>Raised by the speaker buttons with the text to read aloud.</summary>
        public event Action<string> SpeakRequested;

        [Header("Top bar")]
        [SerializeField] Button backButton;
        [SerializeField] Image[] dots;
        [SerializeField] TMP_Text progressLabel;
        [SerializeField] Button soundButton;
        [SerializeField] Image soundIcon;
        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;

        [Header("Question and picture")]
        [SerializeField] TMP_Text questionText;
        [SerializeField] Button speakButton;
        [SerializeField] Image pictureImage;
        [SerializeField] GameObject picturePlaceholder;

        [Header("Answers")]
        [SerializeField] ChoiceCard[] cards;
        [SerializeField] Sprite checkSprite;
        [SerializeField] Sprite crossSprite;

        [Header("After the right answer")]
        [SerializeField] Button nextButton;
        [SerializeField] GameObject starsRoot;
        [SerializeField] Image[] stars;
        [SerializeField] TMP_Text starsLabel;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color doneDot = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color emptyDot = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color normalBorder = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color normalFill = Color.white;
        [SerializeField] Color rightBorder = new Color(0.16f, 0.58f, 0.33f, 1f);
        [SerializeField] Color rightFill = new Color(0.90f, 0.97f, 0.92f, 1f);
        [SerializeField] Color wrongBorder = new Color(0.80f, 0.25f, 0.15f, 1f);
        [SerializeField] Color wrongFill = new Color(0.99f, 0.91f, 0.89f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);

        WordTarget target;
        readonly List<WordChoice> shown = new List<WordChoice>(3);
        readonly bool[] tried = new bool[3];
        int mistakes;
        bool answered;
        Coroutine shake;

        public bool IsOpen => gameObject.activeSelf;

        static string Question => Loc.T("What is this called?", "Ano ang tawag dito?");

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (speakButton != null) speakButton.onClick.AddListener(OnSpeak);
            if (soundButton != null) soundButton.onClick.AddListener(OnSound);
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    int index = i;
                    if (cards[i].button != null) cards[i].button.onClick.AddListener(() => Choose(index));
                }
            }
            GameSettings.Changed += RefreshSound;
            RefreshSound();
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (speakButton != null) speakButton.onClick.RemoveListener(OnSpeak);
            if (soundButton != null) soundButton.onClick.RemoveListener(OnSound);
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                    if (cards[i].button != null) cards[i].button.onClick.RemoveAllListeners();
            }
            GameSettings.Changed -= RefreshSound;
            shake = null;
        }

        // ------------------------------------------------------------------ open

        /// <summary>Opens the quiz for an object. position is its place in the word set (0 = first), total the number of words.</summary>
        public void Open(WordTarget word, int position, int total)
        {
            target = word;
            mistakes = 0;
            answered = false;
            Array.Clear(tried, 0, tried.Length);
            gameObject.SetActive(true);

            // Progress dots and "1 sa 5".
            if (dots != null)
            {
                for (int i = 0; i < dots.Length; i++)
                {
                    if (dots[i] == null) continue;
                    dots[i].gameObject.SetActive(i < total);
                    dots[i].color = i <= position ? doneDot : emptyDot;
                }
            }
            if (progressLabel != null) progressLabel.text = (position + 1) + Loc.T(" of ", " sa ") + total;

            if (questionText != null) { questionText.text = Question; questionText.color = ink; }
            bool hasPicture = word.Picture != null;
            if (pictureImage != null) { pictureImage.sprite = word.Picture; pictureImage.gameObject.SetActive(hasPicture); }
            if (picturePlaceholder != null) picturePlaceholder.SetActive(!hasPicture);

            if (nextButton != null) nextButton.gameObject.SetActive(false);
            if (starsRoot != null) starsRoot.SetActive(false);

            BuildChoices(word);
        }

        void BuildChoices(WordTarget word)
        {
            // The right word and up to two wrong ones, in a random order.
            shown.Clear();
            var wrong = new List<WordChoice>();
            if (word.WrongAnswers != null)
                foreach (WordChoice choice in word.WrongAnswers)
                    if (choice != null && !string.IsNullOrWhiteSpace(choice.word)) wrong.Add(choice);

            int wrongCount = Mathf.Min(wrong.Count, cards != null ? cards.Length - 1 : 0);
            for (int i = 0; i < wrongCount; i++)
            {
                int pick = Random.Range(0, wrong.Count);
                shown.Add(wrong[pick]);
                wrong.RemoveAt(pick);
            }
            shown.Insert(Random.Range(0, shown.Count + 1), word.AsChoice());

            for (int i = 0; i < cards.Length; i++)
            {
                ChoiceCard card = cards[i];
                bool used = i < shown.Count;
                card.button.gameObject.SetActive(used);
                if (!used) continue;

                WordChoice choice = shown[i];
                card.word.text = choice.word;
                card.word.color = ink;
                card.picture.sprite = choice.picture;
                card.picture.gameObject.SetActive(choice.picture != null);
                card.border.color = normalBorder;
                card.fill.color = normalFill;
                card.badge.SetActive(false);
                card.group.alpha = 1f;
                card.button.interactable = true;
                ((RectTransform)card.button.transform).localScale = Vector3.one;
            }
        }

        bool IsRight(int index)
        {
            return index >= 0 && index < shown.Count && shown[index].word == target.Word;
        }

        // ------------------------------------------------------------------ answering

        void Choose(int index)
        {
            if (answered || target == null || index < 0 || index >= shown.Count || tried[index]) return;
            tried[index] = true;
            ChoiceCard card = cards[index];

            if (IsRight(index))
            {
                answered = true;
                Mark(card, true);
                for (int i = 0; i < shown.Count; i++)
                {
                    cards[i].button.interactable = false;
                    if (i != index) cards[i].group.alpha = 0.45f;
                }
                if (questionText != null) questionText.text = Loc.T("Great! This is called " + target.Word + ".", "Magaling! " + Loc.Capitalize(target.Word) + " ang tawag dito.");
                if (nextButton != null) nextButton.gameObject.SetActive(true);
                SpeakRequested?.Invoke(target.Word);
                Answered?.Invoke(mistakes);
            }
            else
            {
                mistakes++;
                Mark(card, false);
                card.button.interactable = false;
                if (questionText != null) questionText.text = Loc.T("Try again!", "Subukan muli!");
                if (shake != null) StopCoroutine(shake);
                shake = StartCoroutine(Shake((RectTransform)card.button.transform));
            }
        }

        void Mark(ChoiceCard card, bool right)
        {
            card.border.color = right ? rightBorder : wrongBorder;
            card.fill.color = right ? rightFill : wrongFill;
            card.badge.SetActive(true);
            card.badgeCircle.color = right ? rightBorder : wrongBorder;
            card.badgeIcon.sprite = right ? checkSprite : crossSprite;
            if (!right) card.group.alpha = 0.8f;
        }

        IEnumerator Shake(RectTransform card)
        {
            Vector2 home = card.anchoredPosition;
            const float duration = 0.35f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - t / duration;
                card.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 14f * fade, 0f);
                yield return null;
            }
            card.anchoredPosition = home;
            shake = null;
        }

        /// <summary>After the last word of the set: shows the stars the place earned.</summary>
        public void ShowCompletion(int earnedStars)
        {
            if (starsRoot != null) starsRoot.SetActive(true);
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                    if (stars[i] != null) stars[i].color = i < earnedStars ? orange : normalBorder;
            }
            if (starsLabel != null) starsLabel.text = Loc.T("You got " + earnedStars + (earnedStars == 1 ? " star!" : " stars!"), "Nakuha mo ang " + earnedStars + " bituin!");
        }

        // ------------------------------------------------------------------ buttons

        void OnBack()
        {
            if (answered) Finished?.Invoke();
            else Cancelled?.Invoke();
        }

        void OnNext()
        {
            if (answered) Finished?.Invoke();
        }

        void OnSpeak()
        {
            SpeakRequested?.Invoke(Question);
        }

        void OnSound()
        {
            GameSettings.Muted = !GameSettings.Muted;
        }

        void RefreshSound()
        {
            if (soundIcon != null) soundIcon.sprite = GameSettings.Muted ? soundOffSprite : soundOnSprite;
        }
    }
}
