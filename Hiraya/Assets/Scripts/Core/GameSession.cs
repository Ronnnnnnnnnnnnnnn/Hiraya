using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hiraya
{
    /// <summary>Which character the player picked on the age screen. Fin = boy, Fiona = girl.</summary>
    public enum CharacterChoice
    {
        Fin = 0,
        Fiona = 1
    }

    /// <summary>
    /// Remembers what the player chose in the Main Menu (age + character) and decides which
    /// playing scene to load. Values are kept in PlayerPrefs so a playing scene can also be
    /// started directly from the editor and still use the last choice.
    /// </summary>
    public static class GameSession
    {
        // Scene file names (without .unity). They must also be listed in the Build Profiles scene list.
        public const string MainMenuScene = "MainMenu";
        public const string KinderScene = "PlayingSceneForKinder";
        public const string GradeOneAndUpScene = "PlayingsceneForGrade1andUp";

        /// <summary>Ages up to and including this value (3 to 5, the preschoolers of the study) go to the Kinder scene.</summary>
        public const int MaxKinderAge = 5;

        const string AgeKey = "Hiraya.Age";
        const string CharacterKey = "Hiraya.Character";
        const string ChildNameKey = "Hiraya.ChildName";
        const string PlaceKey = "Hiraya.Place";
        const string PlaceBuildingKey = "Hiraya.PlaceBuilding";

        /// <summary>The place picked on the world map (for example "paaralan").</summary>
        public static string Place
        {
            get => PlayerPrefs.GetString(PlaceKey, string.Empty);
            set => PlayerPrefs.SetString(PlaceKey, value ?? string.Empty);
        }

        /// <summary>Name of the building object of that place in the playing scene (for example "Building_School"). The player starts in front of it.</summary>
        public static string PlaceBuilding
        {
            get => PlayerPrefs.GetString(PlaceBuildingKey, string.Empty);
            set => PlayerPrefs.SetString(PlaceBuildingKey, value ?? string.Empty);
        }

        /// <summary>Name of the child who is playing (chosen on the title screen), for the HUD and results screens.</summary>
        public static string ChildName
        {
            get => PlayerPrefs.GetString(ChildNameKey, string.Empty);
            set => PlayerPrefs.SetString(ChildNameKey, value ?? string.Empty);
        }

        public static int Age
        {
            get => PlayerPrefs.GetInt(AgeKey, 0);
            set => PlayerPrefs.SetInt(AgeKey, value);
        }

        /// <summary>The age choice as the screens show it: "3+" or "6+".</summary>
        public static string AgeGroup => Age > MaxKinderAge ? "6+" : "3+";

        public static CharacterChoice Character
        {
            get => PlayerPrefs.GetInt(CharacterKey, (int)CharacterChoice.Fin) == (int)CharacterChoice.Fiona
                ? CharacterChoice.Fiona
                : CharacterChoice.Fin;
            set => PlayerPrefs.SetInt(CharacterKey, (int)value);
        }

        /// <summary>The name used in greetings ("Magaling, Fin!"): the child's name if one was given, otherwise the character's name (Fin or Fiona).</summary>
        public static string DisplayName => string.IsNullOrWhiteSpace(ChildName) ? Character.ToString() : ChildName;

        /// <summary>
        /// Set just before the Main Menu scene is loaded from a playing scene (pause menu or results screen):
        /// the title screen then opens straight on the world map. Not saved; it is used once.
        /// </summary>
        public static bool ReturnToWorldMap;

        /// <summary>How many times the current place has been played in this session (1 = first time). "Uliting" adds one; choosing a place on the world map starts again at 1.</summary>
        public static int Attempt = 1;

        /// <summary>Loads the Main Menu scene, which opens on the world map.</summary>
        public static void OpenWorldMap()
        {
            ReturnToWorldMap = true;
            SceneManager.LoadScene(MainMenuScene);
        }

        /// <summary>Plays the current playing scene again from the start.</summary>
        public static void RestartPlace()
        {
            Attempt++;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 6 and up: besides the word, the quiz and the spelling, these children read a description and an example sentence and
        /// fill in the blank of a Filipino sentence. With no age chosen (a scene started on its own) the scene decides.
        /// </summary>
        public static bool IsOlderLearner
        {
            get
            {
                int age = Age;
                return age > 0 ? age > MaxKinderAge : SceneManager.GetActiveScene().name == GradeOneAndUpScene;
            }
        }

        /// <summary>5 and below -> Kinder scene, 6 and up -> Grade 1 and up scene.</summary>
        public static string SceneForAge(int age)
        {
            return age <= MaxKinderAge ? KinderScene : GradeOneAndUpScene;
        }
    }
}
