using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The heads-up display while the child walks around (wireframe 03): who is playing, the goal ("Hanapin ang upuan"),
    /// the stars, the pause button, the word progress dots, the "?" help button, the button that appears when an object is
    /// within reach, and the orange ring that guides to the object to find (only after "?").
    /// It only shows things; GameplayDirector tells it what to show.
    /// The pause button opens the pause menu (GameplayDirector); the On Pause event below is for anything extra you want to run.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public event Action HelpPressed;
        public event Action SpeakPressed;
        /// <summary>Raised by the pause button. GameplayDirector opens the pause menu on it.</summary>
        public event Action PausePressed;
        /// <summary>The button of an object within reach was pressed.</summary>
        public event Action InteractPressed;

        [Header("Who is playing")]
        [SerializeField] TMP_Text profileName;
        [SerializeField] Image portrait;
        [SerializeField] Sprite boyPortrait;
        [SerializeField] Sprite girlPortrait;

        [Header("Stars and pause")]
        [SerializeField] TMP_Text starCount;
        [SerializeField] Button pauseButton;
        [SerializeField] UnityEvent onPause;

        [Header("Goal (top middle)")]
        [SerializeField] GameObject objectiveRoot;
        [SerializeField] TMP_Text objectiveTitle;
        [SerializeField] TMP_Text objectiveSubtitle;
        [SerializeField] Button speakButton;

        [Header("Word progress (bottom middle) and help")]
        [SerializeField] GameObject progressRoot;
        [SerializeField] Image[] dots;
        [SerializeField] TMP_Text progressLabel;
        [SerializeField] Button helpButton;

        [Header("Shown while an object is within reach")]
        [SerializeField] Button interactButton;

        [Header("Ring around the object to find")]
        [SerializeField] RectTransform markerRoot;
        [SerializeField] TMP_Text markerLabel;
        [Tooltip("Shown at the edge of the screen, pointing toward the object, while the object is out of view.")]
        [SerializeField] RectTransform edgeArrow;
        [Tooltip("How far from the screen edges (left, bottom, right, top) the ring may be before the arrow is used. The top bar is tall.")]
        [SerializeField] Vector4 safeMargin = new Vector4(130f, 190f, 130f, 230f);

        [Header("Look")]
        [SerializeField] Color doneDot = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color emptyDot = new Color(0.85f, 0.85f, 0.83f, 1f);

        RectTransform canvasRect;
        Canvas rootCanvas;
        Coroutine pulse;

        void Awake()
        {
            // Positions are measured inside this stretched HUD object (its middle is (0, 0)), the parent of the ring and the arrow.
            canvasRect = transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) rootCanvas = canvas.rootCanvas;
        }

        void OnEnable()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (speakButton != null) speakButton.onClick.AddListener(OnSpeakClicked);
            if (helpButton != null) helpButton.onClick.AddListener(OnHelpClicked);
            if (interactButton != null) interactButton.onClick.AddListener(OnInteractClicked);
            ShowProfile();
        }

        void OnDisable()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (speakButton != null) speakButton.onClick.RemoveListener(OnSpeakClicked);
            if (helpButton != null) helpButton.onClick.RemoveListener(OnHelpClicked);
            if (interactButton != null) interactButton.onClick.RemoveListener(OnInteractClicked);
        }

        void OnPauseClicked()
        {
            onPause.Invoke();
            PausePressed?.Invoke();
        }

        void OnSpeakClicked() => SpeakPressed?.Invoke();

        void OnHelpClicked() => HelpPressed?.Invoke();

        void OnInteractClicked() => InteractPressed?.Invoke();

        /// <summary>Shows or hides the button of the object within reach.</summary>
        public void ShowInteract(bool show)
        {
            if (interactButton != null && interactButton.gameObject.activeSelf != show) interactButton.gameObject.SetActive(show);
        }

        // ------------------------------------------------------------------ what is shown

        /// <summary>Redraws the name and grade, for example after the language was changed.</summary>
        public void RefreshProfile() => ShowProfile();

        void ShowProfile()
        {
            bool girl = GameSession.Character == CharacterChoice.Fiona;
            if (profileName != null) profileName.text = girl ? "Girl" : "Boy";
            if (portrait != null) portrait.sprite = girl ? girlPortrait : boyPortrait;
        }

        public void SetStars(int stars)
        {
            if (starCount != null) starCount.text = stars.ToString();
        }

        /// <summary>The goal banner. Pass null to hide it (a scene without words).</summary>
        public void SetObjective(string title, string subtitle)
        {
            bool show = !string.IsNullOrEmpty(title);
            if (objectiveRoot != null) objectiveRoot.SetActive(show);
            if (!show) return;
            if (objectiveTitle != null) objectiveTitle.text = title;
            if (objectiveSubtitle != null) objectiveSubtitle.text = subtitle;
        }

        /// <summary>The dots and "2 sa 5 salita". Pass total 0 to hide the progress pill and the help button.</summary>
        public void SetProgress(int done, int total)
        {
            bool show = total > 0;
            if (progressRoot != null) progressRoot.SetActive(show);
            if (helpButton != null) helpButton.gameObject.SetActive(show);
            if (!show) return;

            if (dots != null)
            {
                bool useDots = total <= dots.Length;   // with more words than dots only the count is shown
                for (int i = 0; i < dots.Length; i++)
                {
                    if (dots[i] == null) continue;
                    dots[i].gameObject.SetActive(useDots && i < total);
                    dots[i].color = i < done ? doneDot : emptyDot;
                }
            }
            if (progressLabel != null) progressLabel.text = Loc.T(done + " of " + total + " words", done + " sa " + total + " salita");
        }

        // ------------------------------------------------------------------ the ring around the object

        float labelDrop;   // how far below the ring the label was built

        public void HideMarker()
        {
            if (markerRoot != null) markerRoot.gameObject.SetActive(false);
            if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
        }

        /// <summary>Where the word label sits next to the ring: below an object, above a door, beside the exit of a room.</summary>
        public enum LabelSide { Below, Above, Right }

        void PlaceLabel(LabelSide side)
        {
            var pill = markerLabel != null ? markerLabel.transform.parent as RectTransform : null;
            if (pill == null || pill == markerRoot) return;
            if (labelDrop <= 0f) labelDrop = Mathf.Abs(pill.anchoredPosition.y);
            if (side == LabelSide.Right) pill.anchoredPosition = new Vector2((markerRoot.rect.width + pill.rect.width) * 0.5f + 10f, 0f);
            else pill.anchoredPosition = new Vector2(0f, side == LabelSide.Above ? labelDrop : -labelDrop);
        }

        /// <summary>Draws the ring on the object, or an arrow at the screen edge when the object is out of view.</summary>
        public void ShowMarker(Camera camera, Vector3 worldPosition, string label, LabelSide side = LabelSide.Below)
        {
            if (camera == null || canvasRect == null) { HideMarker(); return; }

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            bool behind = screen.z < 0f;
            // An overlay canvas needs no camera; a camera canvas converts through its own camera.
            Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(screen.x, screen.y), uiCamera, out Vector2 local);
            if (behind) local = -local;

            Rect r = canvasRect.rect;
            var safe = Rect.MinMaxRect(r.xMin + safeMargin.x, r.yMin + safeMargin.y, r.xMax - safeMargin.z, r.yMax - safeMargin.w);
            bool inside = !behind && safe.Contains(local);

            if (markerRoot != null)
            {
                markerRoot.gameObject.SetActive(inside);
                if (inside)
                {
                    markerRoot.anchoredPosition = local;
                    if (markerLabel != null) markerLabel.text = label;
                    PlaceLabel(side);
                }
            }

            if (edgeArrow != null)
            {
                edgeArrow.gameObject.SetActive(!inside);
                if (!inside)
                {
                    Vector2 direction = local - safe.center;
                    if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
                    float scale = Mathf.Min(safe.width * 0.5f / Mathf.Max(0.01f, Mathf.Abs(direction.x)),
                                            safe.height * 0.5f / Mathf.Max(0.01f, Mathf.Abs(direction.y)));
                    edgeArrow.anchoredPosition = safe.center + direction * scale;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;   // the arrow picture points up
                    edgeArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        /// <summary>Makes the ring grow and shrink twice, so the child sees where to look (the "?" button).</summary>
        public void Pulse()
        {
            if (markerRoot == null || !isActiveAndEnabled) return;
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(PulseRoutine());
        }

        IEnumerator PulseRoutine()
        {
            const float duration = 1.4f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float wave = Mathf.Abs(Mathf.Sin(t / duration * Mathf.PI * 2f));
                float scale = 1f + 0.35f * wave;
                markerRoot.localScale = new Vector3(scale, scale, 1f);
                if (edgeArrow != null) edgeArrow.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            markerRoot.localScale = Vector3.one;
            if (edgeArrow != null) edgeArrow.localScale = Vector3.one;
            pulse = null;
        }
    }
}
