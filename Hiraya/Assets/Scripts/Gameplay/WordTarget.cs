using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hiraya
{
    /// <summary>One answer card of the quiz: a Filipino word, a small grey category text and an optional picture.</summary>
    [Serializable]
    public class WordChoice
    {
        public string word;
        [Tooltip("The small grey text under the word on the answer card, in Filipino (for example kagamitan).")]
        public string category;
        [Tooltip("The same in English. Empty = the Filipino one.")]
        public string categoryEnglish;
        public Sprite picture;
    }

    /// <summary>
    /// Put this on any object in the world that the child has to find (the item prefabs in Assets/Prefabs/Items have it).
    /// When the character is close, a button appears; pressing it opens this object's word card, then the quiz with two
    /// wrong answers, then the child spells it. Everything is edited in the Inspector, so every object can teach its own word.
    ///
    /// The place (paaralan, tahanan ...) decides which words belong together: when the child chose a place on the
    /// world map, the objects of that place are the words to find, and the place earns its stars when all are answered.
    /// </summary>
    [DisallowMultipleComponent]
    public class WordTarget : MonoBehaviour
    {
        static readonly List<WordTarget> all = new List<WordTarget>();

        /// <summary>Every WordTarget that is switched on in the scene.</summary>
        public static IReadOnlyList<WordTarget> All => all;

        [Header("The word (Filipino)")]
        [SerializeField] string word = "upuan";
        [Tooltip("Which place of the world map the word belongs to: paaralan, tahanan, palengke, hardin or bundok. Empty = every place.")]
        [SerializeField] string place = "";
        [Tooltip("The recording of the word. Played by the speaker buttons and when the answer is right.")]
        [SerializeField] AudioClip sound;
        [Tooltip("The big picture of the quiz. Leave empty to show the grey placeholder.")]
        [SerializeField] Sprite picture;

        [Header("Small texts (Filipino and English)")]
        [SerializeField] string category = "kagamitan";
        [SerializeField] string categoryEnglish = "thing";

        [Header("Spelling (after the quiz)")]
        [Tooltip("Tick to let the child spell the word after the quiz. Words longer than 10 letters are skipped.")]
        [SerializeField] bool spellingStep = true;
        [Tooltip("The clue on the hint card of the spelling screen, in Filipino. Empty = the category.")]
        [SerializeField] string hint = "";
        [Tooltip("The same in English.")]
        [SerializeField] string hintEnglish = "";

        [Header("The two wrong answers")]
        [SerializeField] WordChoice[] wrongAnswers =
        {
            new WordChoice { word = "hapag", category = "kagamitan", categoryEnglish = "thing" },
            new WordChoice { word = "kama", category = "kagamitan", categoryEnglish = "thing" }
        };

        [Header("When does the quiz open?")]
        [Tooltip("How close the character has to be to the edge of this object (in world units, measured on the ground).")]
        [SerializeField, Min(0.1f)] float interactDistance = 1.3f;
        [Tooltip("The solid part of the object. Empty = the first Collider on this object or below it.")]
        [SerializeField] Collider area;

        Collider cachedArea;

        public string Word => word;
        public string Place => (place ?? string.Empty).Trim().ToLowerInvariant();
        public AudioClip Sound => sound;
        public Sprite Picture => picture;
        public string Category => Loc.English && !string.IsNullOrWhiteSpace(categoryEnglish) ? categoryEnglish : category;
        public WordChoice[] WrongAnswers => wrongAnswers;
        public float InteractDistance => interactDistance;
        public bool SpellingStep => spellingStep;

        /// <summary>The clue shown while spelling: the hint of the current language. Empty = no clue card.</summary>
        public string Hint
        {
            get
            {
                string text = Loc.English && !string.IsNullOrWhiteSpace(hintEnglish) ? hintEnglish : hint;
                return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
            }
        }

        /// <summary>This word as an answer card.</summary>
        public WordChoice AsChoice() => new WordChoice { word = word, category = category, categoryEnglish = categoryEnglish, picture = picture };

        /// <summary>True once the child answered this word correctly in this run of the scene.</summary>
        public bool Completed { get; private set; }

        /// <summary>How many wrong answers the child gave for this word.</summary>
        public int Mistakes { get; private set; }

        /// <summary>Where the orange ring of the HUD is drawn: the middle of the object.</summary>
        public Vector3 MarkerPosition
        {
            get
            {
                Collider c = Area;
                return c != null ? c.bounds.center : transform.position + Vector3.up * 0.5f;
            }
        }

        Collider Area
        {
            get
            {
                if (area != null) return area;
                if (cachedArea == null) cachedArea = GetComponentInChildren<Collider>();
                return cachedArea;
            }
        }

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => all.Clear();

        /// <summary>Distance on the ground from a point (the character's feet) to the edge of this object. 0 = touching or inside.</summary>
        public float DistanceTo(Vector3 point)
        {
            Collider c = Area;
            Vector3 closest;
            if (c != null)
            {
                // Look at the object from the height of its middle, so only the ground distance counts.
                closest = c.ClosestPoint(new Vector3(point.x, c.bounds.center.y, point.z));
            }
            else
            {
                closest = transform.position;
            }
            float dx = closest.x - point.x;
            float dz = closest.z - point.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public bool IsNear(Vector3 point) => DistanceTo(point) <= interactDistance;

        public void Complete(int mistakes)
        {
            Completed = true;
            Mistakes = Mathf.Max(0, mistakes);
        }
    }
}
