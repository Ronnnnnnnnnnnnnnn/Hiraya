using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// The list of every word in the game, grouped by place (Assets/Data/WordCatalog.asset). The Salita Book shows one place
    /// at a time with "n / 9 words". Edit it in the Inspector: add a word, give it its place, its English meaning, a picture and
    /// its recording. A word is unlocked in the book once the child has answered it in a quiz, so the word here must be written
    /// exactly like the Word of the object in the scene (capital letters do not matter).
    /// </summary>
    [CreateAssetMenu(fileName = "WordCatalog", menuName = "Hiraya/Word Catalog")]
    public class WordCatalog : ScriptableObject
    {
        [Serializable]
        public class Place
        {
            [Tooltip("paaralan, tahanan, palengke, hardin or bundok (the ids of the world map).")]
            public string id;
            [Tooltip("A locked place cannot be opened in the book yet.")]
            public bool locked;
        }

        [Serializable]
        public class Entry
        {
            public string word;
            [Tooltip("The id of the place the word belongs to.")]
            public string place;
            [Tooltip("The meaning in English, shown under the word.")]
            public string english;
            [Tooltip("The picture of the word. Empty = the first letter of the word is shown instead.")]
            public Sprite picture;
            [Tooltip("The recording of the word, played by the speaker button.")]
            public AudioClip sound;

            [Header("For 6 years old and up")]
            [Tooltip("What the thing is, in English. Shown on the word card.")]
            [TextArea(1, 3)] public string description;
            [Tooltip("An example sentence in Filipino that uses the word.")]
            public string example;
            public string exampleEnglish;
            [Tooltip("A different Filipino sentence for the fill in the blank, with ___ where the word goes. Empty = no exercise.")]
            public string blank;
            [Tooltip("The same sentence in English (the Hint button), also with ___.")]
            public string blankEnglish;
            [Tooltip("Words that do NOT fit the blank, offered next to the right one.")]
            public string[] blankWrong;
        }

        [Tooltip("The place buttons of the book, in this order.")]
        [SerializeField] Place[] places = new Place[0];

        [SerializeField] Entry[] words = new Entry[0];

        public IReadOnlyList<Place> Places => places;

        public IReadOnlyList<Entry> Words => words;

        public int Count => words != null ? words.Length : 0;

        /// <summary>How many words of the catalog the child has already learned.</summary>
        public int LearnedCount()
        {
            int learned = 0;
            if (words == null) return 0;
            foreach (Entry entry in words)
                if (entry != null && GameProgress.IsWordLearned(entry.word)) learned++;
            return learned;
        }

        /// <summary>How many words a place has and how many of them are learned.</summary>
        public void CountPlace(string placeId, out int learned, out int total)
        {
            learned = 0;
            total = 0;
            if (words == null) return;
            foreach (Entry entry in words)
            {
                if (entry == null || !SamePlace(entry.place, placeId)) continue;
                total++;
                if (GameProgress.IsWordLearned(entry.word)) learned++;
            }
        }

        public static bool SamePlace(string a, string b) => string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>The entry of a word, or null.</summary>
        public Entry Find(string word)
        {
            if (words == null || string.IsNullOrEmpty(word)) return null;
            foreach (Entry entry in words)
                if (entry != null && string.Equals(entry.word, word, StringComparison.OrdinalIgnoreCase)) return entry;
            return null;
        }
    }
}
