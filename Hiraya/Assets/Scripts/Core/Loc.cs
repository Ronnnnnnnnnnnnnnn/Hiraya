using System;

namespace Hiraya
{
    /// <summary>
    /// The two languages of the screens. The game is in English; the settings screen can switch it to Filipino.
    /// The Filipino words the child learns (the words of the objects and of the Salita Book) are never translated.
    /// Texts that never change are on a LocalizedText component of the object; texts made in code use Loc.T(english, filipino).
    /// </summary>
    public static class Loc
    {
        public static bool English => GameSettings.Language == GameSettings.English;

        public static string T(string english, string filipino) => English ? english : filipino;

        /// <summary>The name of a place of the world map, from its id ("paaralan" becomes "School" or "Paaralan"). Unknown ids get a capital letter.</summary>
        public static string PlaceName(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            switch (id.Trim().ToLowerInvariant())
            {
                case "tahanan": return T("Home", "Tahanan");
                case "palengke": return T("Market", "Palengke");
                case "paaralan": return T("School", "Paaralan");
                case "hardin": return T("Park", "Hardin");
                case "bundok": return T("Mountain", "Bundok");
                default: return char.ToUpperInvariant(id[0]) + id.Substring(1);
            }
        }

        /// <summary>The category names of the word list are written in Filipino; this gives the name for the current language.</summary>
        public static string BookCategory(string filipino)
        {
            if (!English || string.IsNullOrEmpty(filipino)) return filipino;
            switch (filipino.Trim().ToLowerInvariant())
            {
                case "prutas": return "Fruits";
                case "hayop": return "Animals";
                case "kulay": return "Colours";
                case "numero": return "Numbers";
                case "katawan": return "Body";
                case "kagamitan": return "Things";
                default: return filipino;
            }
        }

        public static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
