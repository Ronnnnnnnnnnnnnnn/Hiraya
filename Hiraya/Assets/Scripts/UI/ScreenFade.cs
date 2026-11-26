using System.Collections;
using UnityEngine;

namespace Hiraya
{
    /// <summary>A black sheet over the whole screen that fades in and out, for walking through a door.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFade : MonoBehaviour
    {
        CanvasGroup group;

        CanvasGroup Group
        {
            get
            {
                if (group == null) group = GetComponent<CanvasGroup>();
                return group;
            }
        }

        public float Alpha => Group.alpha;

        /// <summary>Fades to the given darkness (0 clear, 1 black). While dark it also blocks taps.</summary>
        public IEnumerator FadeTo(float target, float seconds)
        {
            gameObject.SetActive(true);
            Group.blocksRaycasts = true;
            float start = Group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            Group.alpha = target;
            if (target <= 0f)
            {
                Group.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }
    }
}
