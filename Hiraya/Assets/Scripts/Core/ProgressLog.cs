using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// The history the parent dashboard shows: every word the child finished (was the quiz right at the first try, how was the
    /// spelling), every finished lesson (place, score, stars, time) and the days the game was played.
    /// It is saved in PlayerPrefs as one small JSON text and only keeps the latest entries.
    /// The game calls AddWord / AddLesson / MarkPlayed (GameplayDirector); ParentDashboardController reads the numbers.
    /// </summary>
    public static class ProgressLog
    {
        /// <summary>One word the child finished.</summary>
        [Serializable]
        public class WordRecord
        {
            public long utc;              // UTC ticks of the moment it was finished
            public string word;
            public bool firstTry;         // the quiz was answered right at the first tap
            public int spelling = -1;     // -1 = not spelled (skipped), 0 = spelled right at the first check, n = right after n wrong checks
            public int sentence;          // fill in the blank: 0 = not asked or skipped, 1 = right at the first tap, n = right at the n-th tap
        }

        /// <summary>One finished lesson (every word of a place answered).</summary>
        [Serializable]
        public class LessonRecord
        {
            public long utc;
            public string place;
            public int correct;           // words answered right at the first try
            public int total;             // words in the lesson
            public int stars;             // 0 to 3
            public int seconds;           // how long the lesson took (paused time not counted)
            public int spelledRight;      // words spelled right at the first check
            public int spelled;           // words that were spelled
            public string words;          // "upuan, hapag"
        }

        [Serializable]
        class Store
        {
            public List<WordRecord> words = new List<WordRecord>();
            public List<LessonRecord> lessons = new List<LessonRecord>();
            public List<string> days = new List<string>();   // yyyy-MM-dd of the days the game was played
            public long lastPlayed;                          // UTC ticks
        }

        const string Key = "Hiraya.Progress";
        const int MaxWords = 400;
        const int MaxLessons = 60;
        const int MaxDays = 120;

        static readonly string[] MonthNames = { "Ene", "Peb", "Mar", "Abr", "May", "Hun", "Hul", "Ago", "Set", "Okt", "Nob", "Dis" };
        static readonly string[] EnglishMonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        static Store store;

        static Store Data
        {
            get
            {
                if (store == null) Load();
                return store;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => store = null;

        /// <summary>Forget what is in memory and read it again from PlayerPrefs (after the saved text was changed from outside).</summary>
        public static void Reload() => store = null;

        static void Load()
        {
            store = null;
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { store = JsonUtility.FromJson<Store>(json); }
                catch (Exception) { store = null; }
            }
            if (store == null) store = new Store();
            if (store.words == null) store.words = new List<WordRecord>();
            if (store.lessons == null) store.lessons = new List<LessonRecord>();
            if (store.days == null) store.days = new List<string>();
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }

        /// <summary>Deletes the whole history.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            store = null;
        }

        // ------------------------------------------------------------------ writing

        /// <summary>
        /// A word was finished. spelling: -1 not spelled, 0 right at the first check, n right after n wrong checks.
        /// sentence: the fill in the blank, 0 not done, 1 right away, n right at the n-th tap.
        /// </summary>
        public static void AddWord(string word, bool firstTry, int spelling, int sentence = 0, DateTime? whenUtc = null)
        {
            DateTime now = whenUtc ?? DateTime.UtcNow;
            Data.words.Add(new WordRecord { utc = now.Ticks, word = word, firstTry = firstTry, spelling = spelling, sentence = sentence });
            while (Data.words.Count > MaxWords) Data.words.RemoveAt(0);
            MarkPlayed(now);
            Save();
        }

        /// <summary>A lesson (every word of a place) was finished.</summary>
        public static void AddLesson(string place, int correct, int total, int stars, int seconds, int spelledRight, int spelled, string words, DateTime? whenUtc = null)
        {
            DateTime now = whenUtc ?? DateTime.UtcNow;
            Data.lessons.Add(new LessonRecord
            {
                utc = now.Ticks, place = place, correct = correct, total = total, stars = stars, seconds = seconds,
                spelledRight = spelledRight, spelled = spelled, words = words
            });
            while (Data.lessons.Count > MaxLessons) Data.lessons.RemoveAt(0);
            MarkPlayed(now);
            Save();
        }

        /// <summary>The game was played now: remembers the day (for the streak) and the time of the last game. Does not save by itself unless it is a new day.</summary>
        public static void MarkPlayed(DateTime? whenUtc = null)
        {
            DateTime now = whenUtc ?? DateTime.UtcNow;
            Data.lastPlayed = Math.Max(Data.lastPlayed, now.Ticks);
            string day = DayKey(ToLocal(now.Ticks));
            if (!Data.days.Contains(day))
            {
                Data.days.Add(day);
                Data.days.Sort(StringComparer.Ordinal);
                while (Data.days.Count > MaxDays) Data.days.RemoveAt(0);
            }
            Save();
        }

        // ------------------------------------------------------------------ reading

        public static IReadOnlyList<WordRecord> Words => Data.words;

        public static IReadOnlyList<LessonRecord> Lessons => Data.lessons;

        /// <summary>Share of the words that were answered right at the first tap of the quiz (0 to 1), or null if nothing was played yet.</summary>
        public static float? QuizAccuracy()
        {
            if (Data.words.Count == 0) return null;
            int right = 0;
            foreach (WordRecord w in Data.words) if (w.firstTry) right++;
            return right / (float)Data.words.Count;
        }

        /// <summary>Share of the spelled words that were spelled right at the first check (0 to 1), or null if no word was spelled yet.</summary>
        public static float? SpellingAccuracy()
        {
            int spelled = 0, right = 0;
            foreach (WordRecord w in Data.words)
            {
                if (w.spelling < 0) continue;
                spelled++;
                if (w.spelling == 0) right++;
            }
            return spelled == 0 ? (float?)null : right / (float)spelled;
        }

        /// <summary>Share of the fill in the blanks answered right at the first tap (0 to 1), or null if none was done yet.</summary>
        public static float? SentenceAccuracy()
        {
            int done = 0, right = 0;
            foreach (WordRecord w in Data.words)
            {
                if (w.sentence <= 0) continue;
                done++;
                if (w.sentence == 1) right++;
            }
            return done == 0 ? (float?)null : right / (float)done;
        }

        /// <summary>
        /// Quiz accuracy of one week: weeksAgo 0 = the last 7 days up to now, 1 = the 7 days before that, and so on.
        /// Null when nothing was played in that week.
        /// </summary>
        public static float? WeekAccuracy(int weeksAgo, DateTime? nowUtc = null)
        {
            DateTime now = nowUtc ?? DateTime.UtcNow;
            long end = now.AddDays(-7 * weeksAgo).Ticks;
            long start = now.AddDays(-7 * (weeksAgo + 1)).Ticks;
            int count = 0, right = 0;
            foreach (WordRecord w in Data.words)
            {
                if (w.utc <= start || w.utc > end) continue;
                count++;
                if (w.firstTry) right++;
            }
            return count == 0 ? (float?)null : right / (float)count;
        }

        /// <summary>How many days in a row the game was played, up to today (or up to yesterday if it was not played yet today).</summary>
        public static int Streak(DateTime? nowUtc = null)
        {
            DateTime day = ToLocal((nowUtc ?? DateTime.UtcNow).Ticks).Date;
            if (!Data.days.Contains(DayKey(day))) day = day.AddDays(-1);
            int streak = 0;
            while (Data.days.Contains(DayKey(day)))
            {
                streak++;
                day = day.AddDays(-1);
            }
            return streak;
        }

        /// <summary>The time of the last game, or null if the game was never played.</summary>
        public static DateTime? LastPlayedLocal()
        {
            return Data.lastPlayed == 0 ? (DateTime?)null : ToLocal(Data.lastPlayed);
        }

        // ------------------------------------------------------------------ text for the screen

        public static DateTime ToLocal(long utcTicks) => new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();

        static string DayKey(DateTime local) => local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>"Nob 22".</summary>
        public static string ShortDate(DateTime local) => (Loc.English ? EnglishMonthNames : MonthNames)[local.Month - 1] + " " + local.Day;

        /// <summary>"Ngayon, 4:20 PM", "Kahapon, 4:20 PM" or "Nob 20, 4:20 PM".</summary>
        public static string FriendlyTime(DateTime local, DateTime? nowUtc = null)
        {
            DateTime today = ToLocal((nowUtc ?? DateTime.UtcNow).Ticks).Date;
            string time = local.ToString("h:mm tt", CultureInfo.InvariantCulture);
            if (local.Date == today) return Loc.T("Today, ", "Ngayon, ") + time;
            if (local.Date == today.AddDays(-1)) return Loc.T("Yesterday, ", "Kahapon, ") + time;
            return ShortDate(local) + ", " + time;
        }

        /// <summary>"3m 18s".</summary>
        public static string Duration(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            return (seconds / 60) + "m " + (seconds % 60).ToString("00") + "s";
        }
    }
}
