using UnityEngine;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only: makes the screens fit every shape of phone and tablet. The screens are laid out for 1920 x 1080;
    /// the canvas is set to "Expand", so that whole design is always on the screen and a longer or taller screen only adds
    /// room around it. Then:
    ///   - the parts of a menu screen stay together in the middle (backgrounds and dimmers still fill the screen),
    ///   - the parts of the in-game HUD stick to their own edge or corner (pause stays top right, and so on).
    /// Called by the builders just before a screen prefab is saved. Run it once per freshly built screen.
    /// </summary>
    internal static class HirayaLayout
    {
        static readonly Vector2 Design = new Vector2(1920f, 1080f);

        /// <param name="root">The canvas object of a screen prefab.</param>
        /// <param name="hudName">The child that is the in-game HUD, or null when the canvas has none.</param>
        public static void FitAllScreens(GameObject root, string hudName)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            foreach (Transform child in root.transform)
            {
                var panel = child as RectTransform;
                if (panel == null || !FillsParent(panel)) continue;
                Arrange(panel, Design, child.name == hudName);
            }
        }

        static bool FillsParent(RectTransform rect) =>
            rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.GetComponent<AspectRatioFitter>() == null;

        /// <summary>size = how big this full-screen sheet is on a 1920 x 1080 screen.</summary>
        static void Arrange(RectTransform sheet, Vector2 size, bool hud)
        {
            foreach (Transform child in sheet)
            {
                var rect = child as RectTransform;
                if (rect == null || rect.GetComponent<AspectRatioFitter>() != null) continue;
                if (FillsParent(rect))
                {
                    Arrange(rect, size + rect.offsetMax - rect.offsetMin, hud);
                    continue;
                }
                if (rect.anchorMin != rect.anchorMax) continue;   // already stretches along an edge

                Vector2 anchor = new Vector2(0.5f, 0.5f);
                if (hud)
                {
                    // Where the middle of the part is on a 1920 x 1080 screen decides which edge it belongs to.
                    Vector2 middle = Vector2.Scale(rect.anchorMin, size) + rect.anchoredPosition +
                                     Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, rect.sizeDelta);
                    anchor = new Vector2(Edge(middle.x / size.x), Edge(middle.y / size.y));
                }
                Vector2 shift = Vector2.Scale(rect.anchorMin - anchor, size);
                rect.anchorMin = rect.anchorMax = anchor;
                rect.anchoredPosition += shift;
            }
        }

        static float Edge(float share) => share < 0.38f ? 0f : share > 0.62f ? 1f : 0.5f;
    }
}
