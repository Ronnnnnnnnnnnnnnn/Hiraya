using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// One card of the word book (the WordCard prefab). A learned word shows its picture (or its first letter), the Filipino word,
    /// its English meaning and a speaker button. A word that is not learned yet is locked: dashed border, a lock and "Locked".
    /// </summary>
    public class WordCardView : MonoBehaviour
    {
        /// <summary>Raised by the speaker button with the word to read aloud.</summary>
        public event Action<string> SpeakPressed;

        [Header("Look of the card")]
        [SerializeField] Image fill;
        [SerializeField] Image border;
        [Tooltip("The dashed outline used instead of the solid border while the word is locked.")]
        [SerializeField] Image dashedBorder;

        [Header("Picture")]
        [SerializeField] Image picture;
        [Tooltip("The big first letter, shown when the word has no picture.")]
        [SerializeField] TMP_Text letter;
        [SerializeField] GameObject lockIcon;

        [Header("Texts and speaker")]
        [SerializeField] TMP_Text word;
        [Tooltip("The small line under the word: the English meaning.")]
        [SerializeField] TMP_Text category;
        [SerializeField] Button speakButton;

        [Header("Colours")]
        [SerializeField] Color openFill = Color.white;
        [SerializeField] Color lockedFill = new Color(0.97f, 0.97f, 0.95f, 1f);
        [SerializeField] Color ink = new Color(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] Color softInk = new Color(0.43f, 0.43f, 0.43f, 1f);
        [SerializeField] Color lockedInk = new Color(0.66f, 0.66f, 0.64f, 1f);

        public string Word { get; private set; }

        public string Place { get; private set; }

        public bool Learned { get; private set; }

        void OnEnable()
        {
            if (speakButton != null) speakButton.onClick.AddListener(OnSpeak);
        }

        void OnDisable()
        {
            if (speakButton != null) speakButton.onClick.RemoveListener(OnSpeak);
        }

        void OnSpeak()
        {
            if (Learned) SpeakPressed?.Invoke(Word);
        }

        public void Setup(WordCatalog.Entry entry, bool learned)
        {
            Word = entry.word;
            Place = entry.place;
            Learned = learned;

            bool hasPicture = entry.picture != null;
            if (fill != null) fill.color = learned ? openFill : lockedFill;
            if (border != null) border.gameObject.SetActive(learned);
            if (dashedBorder != null) dashedBorder.gameObject.SetActive(!learned);

            if (picture != null)
            {
                picture.sprite = entry.picture;
                picture.gameObject.SetActive(learned && hasPicture);
            }
            if (letter != null)
            {
                letter.gameObject.SetActive(learned && !hasPicture);
                letter.text = string.IsNullOrEmpty(entry.word) ? string.Empty : char.ToUpperInvariant(entry.word[0]).ToString();
            }
            if (lockIcon != null) lockIcon.SetActive(!learned);

            if (word != null)
            {
                word.text = entry.word;
                word.color = learned ? ink : lockedInk;
            }
            if (category != null)
            {
                category.text = learned ? Loc.T(entry.english, Loc.PlaceName(entry.place)) : Loc.T("Locked", "Naka-lock");
                category.color = learned ? softInk : lockedInk;
            }
            if (speakButton != null) speakButton.gameObject.SetActive(learned);
        }
    }
}
