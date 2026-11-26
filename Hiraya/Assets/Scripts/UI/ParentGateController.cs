using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Hiraya
{
    /// <summary>
    /// A small gate in front of the parent dashboard: "7 + 5 = ?" with four numbers to tap. A preschooler cannot do the sum,
    /// so the dashboard stays with the parent or teacher. A wrong number shows a message and asks a new sum.
    /// The gate is switched on or off with "Require Parent Gate" on the TitleScreen object.
    /// </summary>
    public class ParentGateController : MonoBehaviour
    {
        public event Action Passed;
        public event Action BackPressed;

        [SerializeField] Button backButton;
        [SerializeField] TMP_Text question;
        [SerializeField] TMP_Text status;
        [SerializeField] Button[] answerButtons;
        [SerializeField] TMP_Text[] answerLabels;
        int correct;
        readonly int[] shown = new int[4];

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (answerButtons != null)
            {
                for (int i = 0; i < answerButtons.Length; i++)
                {
                    int index = i;
                    if (answerButtons[i] != null) answerButtons[i].onClick.AddListener(() => Choose(index));
                }
            }
            NewSum(string.Empty);
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (answerButtons != null)
                for (int i = 0; i < answerButtons.Length; i++)
                    if (answerButtons[i] != null) answerButtons[i].onClick.RemoveAllListeners();
        }

        void OnBack() => BackPressed?.Invoke();

        /// <summary>The number that is the right answer right now (used by the tests).</summary>
        public int CorrectAnswer => correct;

        void NewSum(string message)
        {
            int a = Random.Range(3, 10);
            int b = Random.Range(3, 10);
            correct = a + b;
            if (question != null) question.text = a + " + " + b + " = ?";
            if (status != null) status.text = message;

            // The right number and three other numbers close to it, in a random order.
            var numbers = new List<int> { correct };
            int guard = 0;
            while (numbers.Count < shown.Length && guard++ < 100)
            {
                int candidate = correct + Random.Range(-4, 5);
                if (candidate >= 2 && !numbers.Contains(candidate)) numbers.Add(candidate);
            }
            for (int i = numbers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
            }
            for (int i = 0; i < shown.Length; i++)
            {
                shown[i] = i < numbers.Count ? numbers[i] : -1;
                if (answerButtons != null && i < answerButtons.Length && answerButtons[i] != null)
                    answerButtons[i].gameObject.SetActive(shown[i] >= 0);
                if (answerLabels != null && i < answerLabels.Length && answerLabels[i] != null) answerLabels[i].text = shown[i].ToString();
            }
        }

        void Choose(int index)
        {
            if (index < 0 || index >= shown.Length) return;
            if (shown[index] == correct) Passed?.Invoke();
            else NewSum(Loc.T("Not quite. Try again.", "Hindi tama. Subukan muli."));
        }
    }
}
