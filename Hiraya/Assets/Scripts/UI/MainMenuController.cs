using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// Main Menu flow:  Main menu (Play)  ->  Age Select (age + Boy/Girl + Continue)  ->  playing scene.
    /// Age 5 and below loads the Kinder scene, age 6 and up loads the Grade 1 and up scene.
    /// All references are assigned in the Inspector (Main Canvas > MainMenuController).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject mainMenuPanel;
        [SerializeField] GameObject ageSelectPanel;

        [Header("Main menu")]
        [SerializeField] Button playButton;

        [Header("Age select")]
        [SerializeField] TMP_InputField ageInput;
        [SerializeField] Button boyButton;
        [SerializeField] Button girlButton;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text messageText;

        [Header("Look")]
        [SerializeField] Color boySelectedColor = new Color(0.55f, 0.78f, 1f, 1f);
        [SerializeField] Color girlSelectedColor = new Color(1f, 0.68f, 0.84f, 1f);

        CharacterChoice? chosenCharacter;
        Color boyNormalColor = Color.white;
        Color girlNormalColor = Color.white;

        void Awake()
        {
            if (boyButton != null && boyButton.targetGraphic != null) boyNormalColor = boyButton.targetGraphic.color;
            if (girlButton != null && girlButton.targetGraphic != null) girlNormalColor = girlButton.targetGraphic.color;

            if (ageInput != null)
            {
                ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                ageInput.characterLimit = 2;
            }

            ShowMainMenu();
        }

        void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(ShowAgeSelect);
            if (boyButton != null) boyButton.onClick.AddListener(ChooseBoy);
            if (girlButton != null) girlButton.onClick.AddListener(ChooseGirl);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
        }

        void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(ShowAgeSelect);
            if (boyButton != null) boyButton.onClick.RemoveListener(ChooseBoy);
            if (girlButton != null) girlButton.onClick.RemoveListener(ChooseGirl);
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }

        void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (ageSelectPanel != null) ageSelectPanel.SetActive(false);
        }

        void ShowAgeSelect()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (ageSelectPanel != null) ageSelectPanel.SetActive(true);
            chosenCharacter = null;
            RefreshCharacterButtons();
            ShowMessage(string.Empty);
        }

        void ChooseBoy()
        {
            chosenCharacter = CharacterChoice.Fin;
            RefreshCharacterButtons();
            ShowMessage(string.Empty);
        }

        void ChooseGirl()
        {
            chosenCharacter = CharacterChoice.Fiona;
            RefreshCharacterButtons();
            ShowMessage(string.Empty);
        }

        void RefreshCharacterButtons()
        {
            SetButtonColor(boyButton, chosenCharacter == CharacterChoice.Fin ? boySelectedColor : boyNormalColor);
            SetButtonColor(girlButton, chosenCharacter == CharacterChoice.Fiona ? girlSelectedColor : girlNormalColor);
        }

        static void SetButtonColor(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null) button.targetGraphic.color = color;
        }

        void Continue()
        {
            if (!TryReadAge(out int age))
            {
                ShowMessage("Please enter your age (1 - 99).");
                return;
            }

            if (chosenCharacter == null)
            {
                ShowMessage("Please choose Boy or Girl.");
                return;
            }

            GameSession.Age = age;
            GameSession.Character = chosenCharacter.Value;
            PlayerPrefs.Save();

            SceneManager.LoadScene(GameSession.SceneForAge(age));
        }

        bool TryReadAge(out int age)
        {
            age = 0;
            if (ageInput == null) return false;
            string text = ageInput.text == null ? string.Empty : ageInput.text.Trim();
            return int.TryParse(text, out age) && age >= 1 && age <= 99;
        }

        void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }
    }
}
