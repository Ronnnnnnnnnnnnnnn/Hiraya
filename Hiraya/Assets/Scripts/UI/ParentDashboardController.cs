using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hiraya
{
    /// <summary>
    /// The parent / teacher dashboard (wireframe 10): who is playing and how they are doing, the quiz accuracy of the last four
    /// weeks, the skills (vocabulary, spelling) and the recent lessons. All numbers come from ProgressLog, GameProgress and GameSession,
    /// so they are real; with nothing played yet the screen shows dashes.
    /// "I-export ang Ulat" saves a report (a CSV file you can open in Excel) in the game's data folder and raises On Export with its path,
    /// so a share button can be connected later. "Lumabas" and the back arrow leave the dashboard.
    /// </summary>
    public class ParentDashboardController : MonoBehaviour
    {
        [Serializable]
        public class WeekColumn
        {
            public RectTransform bar;
            public Image barImage;
            public TMP_Text percent;
            public TMP_Text label;
        }

        [Serializable]
        public class SkillRow
        {
            public TMP_Text value;
            public RectTransform fill;
        }

        [Serializable]
        public class LessonRow
        {
            public GameObject root;
            public TMP_Text date;
            public TMP_Text place;
            public TMP_Text words;
            public TMP_Text correct;
            public TMP_Text spelling;
            public TMP_Text stars;
            public TMP_Text time;
        }

        public event Action ExitPressed;

        [Header("Top bar")]
        [SerializeField] Button backButton;
        [SerializeField] Button exitButton;
        [SerializeField] Button exportButton;
        [SerializeField] TMP_Text exportStatus;
        [SerializeField] UnityEvent<string> onExport;

        [Header("The child")]
        [SerializeField] TMP_Text childName;
        [SerializeField] TMP_Text childInfo;
        [SerializeField] Image portrait;
        [SerializeField] Sprite boyPortrait;
        [SerializeField] Sprite girlPortrait;
        [SerializeField] TMP_Text statusTag;
        [SerializeField] Image statusTagFill;
        [SerializeField] TMP_Text starsValue;
        [SerializeField] TMP_Text wordsValue;
        [SerializeField] TMP_Text streakValue;
        [SerializeField] TMP_Text lastPlayed;

        [Header("Last four weeks (the last one is this week)")]
        [SerializeField] WeekColumn[] weeks;
        [SerializeField] float tallestBar = 200f;
        [SerializeField] float shortestBar = 12f;

        [Header("Skills: vocabulary (quiz), spelling, sentences (fill in the blank, 6 and up)")]
        [SerializeField] SkillRow[] skills;
        [SerializeField] TMP_Text focusNote;
        [Tooltip("A skill below this share (0 to 1) is shown as the one that needs help.")]
        [SerializeField, Range(0f, 1f)] float needsHelpBelow = 0.6f;

        [Header("Recent lessons (newest first)")]
        [SerializeField] LessonRow[] lessons;
        [SerializeField] GameObject noLessonsText;

        [Header("Look")]
        [SerializeField] Color orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        [SerializeField] Color lightOrange = new Color(0.96f, 0.80f, 0.72f, 1f);
        [SerializeField] Color grey = new Color(0.85f, 0.85f, 0.83f, 1f);
        [SerializeField] Color good = new Color(0.16f, 0.58f, 0.33f, 1f);
        [SerializeField] Color goodFill = new Color(0.90f, 0.97f, 0.92f, 1f);
        [SerializeField] Color warning = new Color(0.80f, 0.25f, 0.15f, 1f);
        [SerializeField] Color warningFill = new Color(0.99f, 0.91f, 0.89f, 1f);
        [SerializeField] Color softInk = new Color(0.43f, 0.43f, 0.43f, 1f);

        void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnExit);
            if (exitButton != null) exitButton.onClick.AddListener(OnExit);
            if (exportButton != null) exportButton.onClick.AddListener(Export);
            ProgressLog.Reload();
            Refresh();
        }

        void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnExit);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
            if (exportButton != null) exportButton.onClick.RemoveListener(Export);
        }

        void OnExit() => ExitPressed?.Invoke();

        // ------------------------------------------------------------------ drawing

        void Refresh()
        {
            if (exportStatus != null) exportStatus.text = string.Empty;
            RefreshChild();
            RefreshWeeks();
            RefreshSkills();
            RefreshLessons();
        }

        void RefreshChild()
        {
            bool girl = GameSession.Character == CharacterChoice.Fiona;
            if (childName != null) childName.text = GameSession.DisplayName;
            if (portrait != null) portrait.sprite = girl ? girlPortrait : boyPortrait;
            if (childInfo != null)
            {
                int age = GameSession.Age;
                childInfo.text = age > 0 ? Loc.T("Age ", "Edad ") + GameSession.AgeGroup : string.Empty;
            }

            if (starsValue != null) starsValue.text = GameProgress.TotalStars().ToString();
            if (wordsValue != null) wordsValue.text = GameProgress.WordsLearned.ToString();
            if (streakValue != null) streakValue.text = Loc.T(ProgressLog.Streak() + (ProgressLog.Streak() == 1 ? " day" : " days"), ProgressLog.Streak() + " araw");

            DateTime? last = ProgressLog.LastPlayedLocal();
            if (lastPlayed != null) lastPlayed.text = Loc.T("Last played: ", "Huling laro: ") + (last.HasValue ? ProgressLog.FriendlyTime(last.Value) : Loc.T("never", "wala pa"));

            // The tag under the name: which skill needs help.
            string weakest = WeakestSkill(out float weakestValue);
            if (statusTag == null) return;
            if (weakest == null)
            {
                statusTag.text = Loc.T("No data yet", "Wala pang datos");
                statusTag.color = softInk;
                if (statusTagFill != null) statusTagFill.color = grey;
            }
            else if (weakestValue < needsHelpBelow)
            {
                statusTag.text = Loc.T("Needs help with ", "Nahihirapan sa ") + weakest.ToLowerInvariant();
                statusTag.color = warning;
                if (statusTagFill != null) statusTagFill.color = warningFill;
            }
            else
            {
                statusTag.text = Loc.T("Doing well", "Maganda ang progreso");
                statusTag.color = good;
                if (statusTagFill != null) statusTagFill.color = goodFill;
            }
        }

        static string[] SkillNames => new[] { Loc.T("Vocabulary", "Bokabularyo"), Loc.T("Spelling", "Pagbaybay"), Loc.T("Sentences", "Pangungusap") };

        string WeakestSkill(out float value)
        {
            string name = null;
            value = 1f;
            float? quiz = ProgressLog.QuizAccuracy();
            float? spelling = ProgressLog.SpellingAccuracy();
            if (quiz.HasValue) { name = SkillNames[0]; value = quiz.Value; }
            if (spelling.HasValue && spelling.Value < value) { name = SkillNames[1]; value = spelling.Value; }
            float? sentences = ProgressLog.SentenceAccuracy();
            if (sentences.HasValue && sentences.Value < value) { name = SkillNames[2]; value = sentences.Value; }
            return name;
        }

        void RefreshWeeks()
        {
            if (weeks == null) return;
            for (int i = 0; i < weeks.Length; i++)
            {
                WeekColumn week = weeks[i];
                if (week.label != null) week.label.text = Loc.T("Week ", "Linggo ") + (i + 1);
                // The last column is this week (0 weeks ago), the first one is three weeks ago.
                float? accuracy = ProgressLog.WeekAccuracy(weeks.Length - 1 - i);
                float height = accuracy.HasValue ? Mathf.Max(shortestBar, accuracy.Value * tallestBar) : shortestBar;
                if (week.bar != null) week.bar.sizeDelta = new Vector2(week.bar.sizeDelta.x, height);
                if (week.barImage != null)
                {
                    int fromEnd = weeks.Length - 1 - i;
                    week.barImage.color = fromEnd == 0 ? orange : (fromEnd == 1 ? lightOrange : grey);
                }
                if (week.percent != null)
                {
                    week.percent.text = accuracy.HasValue ? Mathf.RoundToInt(accuracy.Value * 100f) + "%" : "—";
                    // The number sits just above the bar (its pivot is the bottom middle).
                    var rect = (RectTransform)week.percent.transform;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, (week.bar != null ? week.bar.anchoredPosition.y : 0f) + height + 8f);
                }
            }
        }

        void RefreshSkills()
        {
            if (skills == null) return;
            float?[] values = { ProgressLog.QuizAccuracy(), ProgressLog.SpellingAccuracy(), ProgressLog.SentenceAccuracy() };
            for (int i = 0; i < skills.Length && i < values.Length; i++)
            {
                SkillRow row = skills[i];
                if (row.value != null) row.value.text = values[i].HasValue ? Mathf.RoundToInt(values[i].Value * 100f) + "%" : "—";
                if (row.fill != null)
                {
                    row.fill.anchorMax = new Vector2(values[i].HasValue ? Mathf.Clamp01(values[i].Value) : 0f, 1f);
                    row.fill.gameObject.SetActive(values[i].HasValue && values[i].Value > 0f);
                }
            }

            if (focusNote != null)
            {
                string weakest = WeakestSkill(out _);
                focusNote.text = weakest == null ? Loc.T("Not enough data yet", "Wala pang sapat na datos") : Loc.T("Main focus: ", "Pangunahing pokus: ") + weakest;
            }
        }

        void RefreshLessons()
        {
            var records = ProgressLog.Lessons;
            if (lessons != null)
            {
                for (int i = 0; i < lessons.Length; i++)
                {
                    int index = records.Count - 1 - i;   // newest first
                    LessonRow row = lessons[i];
                    row.root.SetActive(index >= 0);
                    if (index < 0) continue;

                    ProgressLog.LessonRecord record = records[index];
                    row.date.text = ProgressLog.ShortDate(ProgressLog.ToLocal(record.utc));
                    row.place.text = Loc.PlaceName(record.place);
                    row.words.text = record.words;
                    row.correct.text = record.correct + " / " + record.total;
                    row.spelling.text = record.spelled > 0 ? record.spelledRight + " / " + record.spelled : "—";
                    row.stars.text = record.stars + " / " + GameProgress.MaxStarsPerPlace;
                    row.time.text = ProgressLog.Duration(record.seconds);
                }
            }
            if (noLessonsText != null) noLessonsText.SetActive(records.Count == 0);
        }

        // ------------------------------------------------------------------ export

        /// <summary>The report as CSV text (opens in Excel or Google Sheets).</summary>
        public string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Loc.T("Hiraya - Progress Report", "Hiraya - Ulat ng Progreso"));
            sb.AppendLine(Loc.T("Child,", "Bata,") + Quote(GameSession.DisplayName));
            sb.AppendLine(Loc.T("Age,", "Edad,") + GameSession.AgeGroup);
            sb.AppendLine(Loc.T("Total stars,", "Kabuuang bituin,") + GameProgress.TotalStars());
            sb.AppendLine(Loc.T("Words learned,", "Natutunang salita,") + GameProgress.WordsLearned);
            sb.AppendLine(Loc.T("Days in a row,", "Sunod-sunod na araw,") + ProgressLog.Streak());
            float? quiz = ProgressLog.QuizAccuracy();
            float? spelling = ProgressLog.SpellingAccuracy();
            sb.AppendLine(Loc.T("Vocabulary,", "Bokabularyo,") + (quiz.HasValue ? Mathf.RoundToInt(quiz.Value * 100f) + "%" : ""));
            sb.AppendLine(Loc.T("Spelling,", "Pagbaybay,") + (spelling.HasValue ? Mathf.RoundToInt(spelling.Value * 100f) + "%" : ""));
            float? sentences = ProgressLog.SentenceAccuracy();
            sb.AppendLine(Loc.T("Sentences,", "Pangungusap,") + (sentences.HasValue ? Mathf.RoundToInt(sentences.Value * 100f) + "%" : ""));
            sb.AppendLine();
            sb.AppendLine(Loc.T("Date,Place,Words,Correct answers,Spelling,Stars,Time", "Petsa,Lugar,Mga salita,Tamang sagot,Pagbaybay,Bituin,Tagal"));
            var records = ProgressLog.Lessons;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                ProgressLog.LessonRecord r = records[i];
                sb.AppendLine(ProgressLog.ShortDate(ProgressLog.ToLocal(r.utc)) + "," + Quote(Loc.PlaceName(r.place)) + "," + Quote(r.words) + "," + r.correct + "/" + r.total + "," +
                              (r.spelled > 0 ? r.spelledRight + "/" + r.spelled : "") + "," + r.stars + "/" + GameProgress.MaxStarsPerPlace + "," + ProgressLog.Duration(r.seconds));
            }
            return sb.ToString();
        }

        static string Quote(string text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        void Export()
        {
            string path = Path.Combine(Application.persistentDataPath, "hiraya_ulat.csv");
            try
            {
                File.WriteAllText(path, BuildReport(), new UTF8Encoding(true));
                if (exportStatus != null) exportStatus.text = Loc.T("Report saved: ", "Na-save ang ulat: ") + path;
                onExport.Invoke(path);
            }
            catch (Exception)
            {
                if (exportStatus != null) exportStatus.text = Loc.T("The report could not be saved.", "Hindi na-save ang ulat.");
            }
        }
    }
}
