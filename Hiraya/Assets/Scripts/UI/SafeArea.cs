using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Keeps a full-screen UI object inside the part of the screen the phone can show completely: away from the camera
    /// notch or punch-hole (the game itself fills the whole screen, with the status and navigation bars hidden).
    /// Put it on a RectTransform that is stretched over its canvas; the HUD and the D-pad have one.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int size;

        void OnEnable() => Apply();

        void Update()
        {
            // The safe area changes when the phone is turned to the other landscape side.
            if (Screen.safeArea != applied || Screen.width != size.x || Screen.height != size.y) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            size = new Vector2Int(Screen.width, Screen.height);
            if (size.x <= 0 || size.y <= 0) return;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(Mathf.Clamp01(applied.xMin / size.x), Mathf.Clamp01(applied.yMin / size.y));
            rect.anchorMax = new Vector2(Mathf.Clamp01(applied.xMax / size.x), Mathf.Clamp01(applied.yMax / size.y));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
