using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The card that introduces a word before its quiz: the picture of the object, the Filipino word, what it means, and a
    /// Listen button. Children of 6 and up also get a description in English and an example sentence (both written in the
    /// WordCatalog). The recording is played by GameplayDirector when the card opens. "Next" goes on to the quiz, the back
    /// arrow returns to the world. Lives on the "WordPanel" of the GameplayUI prefab.
    /// </summary>
    public class WordIntroScreen : MonoBehaviour
    {
        public event Action Continued;
        public event Action Cancelled;
        public event Action SpeakRequested;

        [Header("Parts of the card")]
        [SerializeField] TMP_Text heading;
        [SerializeField] TMP_Text wordText;
        [SerializeField] TMP_Text meaningText;
        [SerializeField] Image pictureImage;
        [SerializeField] GameObject picturePlaceholder;

        [Header("Description and example (6 and up)")]
        [Tooltip("The word and its meaning. It moves down to the middle when there is no description to show.")]
        [SerializeField] RectTransform wordGroup;
        [SerializeField] float dropWithoutDetails = 170f;
        [SerializeField] GameObject details;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text exampleLabel;
        [SerializeField] TMP_Text exampleText;
        [SerializeField] TMP_Text exampleEnglishText;

        [Header("Buttons")]
        [SerializeField] Button backButton;
        [SerializeField] Button listenButton;
        [SerializeField] Button nextButton;

        WordTarget target;
        WordCatalog.Entry entry;
        bool withDetails;
        Vector2 groupHome;
        bool homeKnown;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>True when the open card shows the description and the example.</summary>
        public bool ShowsDetails => IsOpen && details != null && details.activeSelf;

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (listenButton != null) listenButton.onClick.AddListener(OnListen);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            GameSettings.Changed += Refresh;
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (listenButton != null) listenButton.onClick.RemoveListener(OnListen);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            GameSettings.Changed -= Refresh;
        }

        /// <summary>Shows the card of an object. listed is its line in the word list (may be null), older = 6 and up.</summary>
        public void Open(WordTarget word, WordCatalog.Entry listed, bool older)
        {
            target = word;
            entry = listed;
            withDetails = older && listed != null && !string.IsNullOrWhiteSpace(listed.description);
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        void Refresh()
        {
            if (target == null) return;
            if (heading != null) heading.text = Loc.T("New word", "Bagong salita");
            if (wordText != null) wordText.text = target.Word;
            if (meaningText != null) meaningText.text = Meaning();

            bool hasPicture = target.Picture != null;
            if (pictureImage != null) { pictureImage.sprite = target.Picture; pictureImage.gameObject.SetActive(hasPicture); }
            if (picturePlaceholder != null) picturePlaceholder.SetActive(!hasPicture);

            // A word without a recording has nothing to listen to.
            if (listenButton != null) listenButton.gameObject.SetActive(target.Sound != null);

            if (wordGroup != null)
            {
                if (!homeKnown) { groupHome = wordGroup.anchoredPosition; homeKnown = true; }
                wordGroup.anchoredPosition = groupHome + (withDetails ? Vector2.zero : Vector2.down * dropWithoutDetails);
            }
            if (details != null) details.SetActive(withDetails);
            if (!withDetails) return;

            // The description is in English in both languages of the game; only the small headings follow the language.
            if (descriptionLabel != null) descriptionLabel.text = Loc.T("What is it?", "Ano ito?");
            if (descriptionText != null) descriptionText.text = entry.description;
            if (exampleLabel != null) exampleLabel.text = Loc.T("Example", "Halimbawa");
            if (exampleText != null) exampleText.text = entry.example;
            if (exampleEnglishText != null) exampleEnglishText.text = entry.exampleEnglish;
        }

        /// <summary>The English meaning from the word list; nothing in Filipino or for a word that is not listed.</summary>
        string Meaning()
        {
            if (Loc.English && entry != null && !string.IsNullOrWhiteSpace(entry.english)) return Loc.Capitalize(entry.english);
            return string.Empty;
        }

        void OnBack() => Cancelled?.Invoke();

        void OnListen() => SpeakRequested?.Invoke();

        void OnNext() => Continued?.Invoke();
    }
}
