using TMPro;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// "Large text" of the settings screen: lets this text grow up to 20% bigger, as far as its box has room (it never gets
    /// smaller than normal and does not grow out of its box). The screen builders put it on every text of the screens;
    /// add it to a text you make yourself if it should follow the setting too.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class LargeTextSupport : MonoBehaviour
    {
        [SerializeField, Range(1f, 1.6f)] float bigger = 1.2f;

        TMP_Text label;
        bool changed;
        bool autoSizing;
        float size, minSize, maxSize;

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDisable() => GameSettings.Changed -= Apply;

        void Apply()
        {
            bool large = GameSettings.LargeText;
            if (large == changed) return;
            if (label == null) label = GetComponent<TMP_Text>();

            if (large)
            {
                autoSizing = label.enableAutoSizing;
                size = label.fontSize;
                minSize = label.fontSizeMin;
                maxSize = label.fontSizeMax;

                label.fontSizeMin = autoSizing ? minSize : size;
                label.fontSizeMax = (autoSizing ? maxSize : size) * bigger;
                label.enableAutoSizing = true;
            }
            else
            {
                label.enableAutoSizing = autoSizing;
                label.fontSizeMin = minSize;
                label.fontSizeMax = maxSize;
                if (!autoSizing) label.fontSize = size;
            }
            changed = large;
        }
    }
}
