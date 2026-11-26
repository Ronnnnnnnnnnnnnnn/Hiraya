using TMPro;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// A text with an English and a Filipino version. It shows the one of the current language (the settings screen can change it).
    /// Edit both versions here in the Inspector. Texts that scripts fill in themselves do not use this.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField, TextArea] string english;
        [SerializeField, TextArea] string filipino;

        TMP_Text label;

        public string English => english;
        public string Filipino => filipino;

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            GameSettings.Changed -= Apply;
        }

        void Apply()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            string text = Loc.T(english, filipino);
            if (label != null && !string.IsNullOrEmpty(text)) label.text = text;
        }
    }
}
