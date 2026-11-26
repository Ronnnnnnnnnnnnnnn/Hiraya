using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// What the child has achieved: stars per place (0 to 3) and how many words are in the Salita Book.
    /// Saved with PlayerPrefs. The quiz and results screens will call SetStars / AddWord when they exist.
    /// </summary>
    public static class GameProgress
    {
        public const int MaxStarsPerPlace = 3;

        /// <summary>The ids of the places on the world map, in the same order as the list there.</summary>
        public static readonly string[] PlaceIds = { "tahanan", "palengke", "paaralan", "hardin", "bundok" };

        const string StarsKey = "Hiraya.Stars.";
        const string WordsKey = "Hiraya.WordsLearned";
        const string WordKey = "Hiraya.Word.";

        /// <summary>All the stars earned, added up over every place.</summary>
        public static int TotalStars()
        {
            int total = 0;
            foreach (string id in PlaceIds) total += GetStars(id);
            return total;
        }

        public static bool IsWordLearned(string word)
        {
            return PlayerPrefs.GetInt(WordKey + Normalize(word), 0) == 1;
        }

        /// <summary>Adds a word to the Salita Book (only once per word). Returns true when the word was new.</summary>
        public static bool MarkWordLearned(string word)
        {
            string key = WordKey + Normalize(word);
            if (string.IsNullOrEmpty(word) || PlayerPrefs.GetInt(key, 0) == 1) return false;
            PlayerPrefs.SetInt(key, 1);
            WordsLearned++;
            return true;
        }

        static string Normalize(string word)
        {
            return (word ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static int GetStars(string placeId)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarsKey + placeId, 0), 0, MaxStarsPerPlace);
        }

        /// <summary>Keeps the best result: a lower score never replaces a higher one.</summary>
        public static void SetStars(string placeId, int stars)
        {
            stars = Mathf.Clamp(stars, 0, MaxStarsPerPlace);
            if (stars > GetStars(placeId)) PlayerPrefs.SetInt(StarsKey + placeId, stars);
        }

        public static int WordsLearned
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(WordsKey, 0));
            set => PlayerPrefs.SetInt(WordsKey, Mathf.Max(0, value));
        }
    }
}
