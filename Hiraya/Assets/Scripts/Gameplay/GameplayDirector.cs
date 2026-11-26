using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Hiraya
{
    /// <summary>
    /// Runs the "find the word" game in a playing scene: it looks at the word objects (WordTarget) of the chosen place,
    /// tells the HUD what to show, and when the character is within reach of one a button appears; pressing it (or E, Space,
    /// Enter on a keyboard) shows the word (picture and recording), then the quiz and the spelling. The ring that guides to
    /// the next word shows only after the "?" button, until that word is answered. Children of 6 and up (GameSession.IsOlderLearner) also get a description and an example
    /// on the word card, and a fill in the blank after the spelling. It saves the stars of the place when every word has
    /// been answered and shows the results. It also runs the pause menu, plays the word recordings, and walks the
    /// character through building doors into the rooms (Interior) and back out.
    ///
    /// Which words: the objects whose Place is the place chosen on the world map. If there are none (or no place was chosen,
    /// for example when the scene is started on its own) every word object in the scene counts.
    /// Stars of a place (best result is kept): no wrong answers = 3, one or two wrong = 2, more = 1.
    ///
    /// Sits on the root of the GameplayUI prefab, which is placed in both playing scenes.
    /// </summary>
    public class GameplayDirector : MonoBehaviour
    {
        [SerializeField] GameplayHUD hud;
        [SerializeField] WordIntroScreen intro;
        [SerializeField] QuizScreen quiz;
        [SerializeField] SpellingScreen spelling;
        [SerializeField] SentenceScreen sentence;
        [Tooltip("The word list: the English meaning, and the description and sentences for 6 and up.")]
        [SerializeField] WordCatalog catalog;
        [SerializeField] PauseScreen pause;
        [SerializeField] ResultsScreen results;
        [SerializeField] ScreenFade fade;
        [Tooltip("The character. Empty = the PlayerController found in the scene.")]
        [SerializeField] PlayerController player;
        [Tooltip("Show the word with its picture and recording before the quiz.")]
        [SerializeField] bool showWordFirst = true;
        [Tooltip("The most words one place can ask for.")]
        [SerializeField, Range(1, 20)] int wordsPerSet = 20;
        [Tooltip("How long the screen takes to turn black (and back) when going through a door.")]
        [SerializeField, Min(0f)] float doorFadeSeconds = 0.25f;

        [Header("Extra: called with the text a speaker button asks for")]
        [SerializeField] UnityEvent<string> onSpeak;

        readonly List<WordTarget> set = new List<WordTarget>();
        Camera viewCamera;
        FollowCamera followCamera;
        WordTarget active;
        WordTarget reach;       // the object within reach: its button is shown
        WordTarget guided;      // the word the "?" button was pressed for
        Interior room;          // the room the character is in, null outside
        int mistakesInSet;
        int newWords;
        float lessonSeconds;    // play time of this lesson, paused time not counted
        int spelledWords;
        int spelledRight;
        bool introOpen;
        bool quizOpen;
        bool spellingOpen;
        bool sentenceOpen;
        int spellingResult;     // of the active word, until it is written down
        bool paused;
        bool resultsOpen;
        bool travelling;        // going through a door

        /// <summary>Stars earned by finishing the set in this run (0 until every word is answered).</summary>
        public int StarsEarned { get; private set; }

        public bool SetComplete { get; private set; }

        public bool IsPaused => paused;

        /// <summary>The object whose button is showing, or null.</summary>
        public WordTarget InReach => ScreenOpen || travelling ? null : reach;

        /// <summary>True while the ring guides to the next word (after "?").</summary>
        public bool GuideOn => guided != null && guided == CurrentGoal();

        public Interior CurrentRoom => room;

        public bool IsTravelling => travelling;

        public IReadOnlyList<WordTarget> WordSet => set;

        bool ScreenOpen => introOpen || quizOpen || spellingOpen || sentenceOpen || paused || resultsOpen;

        void Start()
        {
            Time.timeScale = 1f;
            ProgressLog.MarkPlayed();   // for the streak and "Last played" of the parent dashboard
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            viewCamera = Camera.main;
            if (viewCamera != null) followCamera = viewCamera.GetComponent<FollowCamera>();
            BuildSet();
            if (fade != null) fade.gameObject.SetActive(false);

            if (intro != null)
            {
                intro.gameObject.SetActive(false);
                intro.Continued += OnIntroContinued;
                intro.Cancelled += OnIntroCancelled;
                intro.SpeakRequested += OnIntroSpeak;
            }
            if (quiz != null)
            {
                quiz.gameObject.SetActive(false);
                quiz.Answered += OnAnswered;
                quiz.Finished += OnFinished;
                quiz.Cancelled += OnCancelled;
                quiz.SpeakRequested += OnScreenSpeak;
            }
            if (spelling != null)
            {
                spelling.gameObject.SetActive(false);
                spelling.Completed += OnSpellingCompleted;
                spelling.Skipped += OnSpellingSkipped;
                spelling.SpeakRequested += OnScreenSpeak;
            }
            if (sentence != null)
            {
                sentence.gameObject.SetActive(false);
                sentence.Completed += OnSentenceCompleted;
                sentence.Skipped += OnSentenceSkipped;
                sentence.SpeakRequested += OnIntroSpeak;
            }
            if (pause != null)
            {
                pause.gameObject.SetActive(false);
                pause.Resumed += ClosePause;
                pause.RestartRequested += RestartPlace;
                pause.MapRequested += GoToMap;
            }
            if (results != null)
            {
                results.gameObject.SetActive(false);
                results.ReplayRequested += RestartPlace;
                results.NextRequested += GoToMap;
                results.SpeakRequested += OnWordSpeak;
            }
            if (hud != null)
            {
                hud.HelpPressed += OnHelp;
                hud.SpeakPressed += OnObjectiveSpeak;
                hud.PausePressed += OpenPause;
                hud.InteractPressed += Interact;
            }
            GameSettings.Changed += OnSettingsChanged;
            RefreshHud();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            GameSettings.Changed -= OnSettingsChanged;
            if (intro != null)
            {
                intro.Continued -= OnIntroContinued;
                intro.Cancelled -= OnIntroCancelled;
                intro.SpeakRequested -= OnIntroSpeak;
            }
            if (quiz != null)
            {
                quiz.Answered -= OnAnswered;
                quiz.Finished -= OnFinished;
                quiz.Cancelled -= OnCancelled;
                quiz.SpeakRequested -= OnScreenSpeak;
            }
            if (spelling != null)
            {
                spelling.Completed -= OnSpellingCompleted;
                spelling.Skipped -= OnSpellingSkipped;
                spelling.SpeakRequested -= OnScreenSpeak;
            }
            if (sentence != null)
            {
                sentence.Completed -= OnSentenceCompleted;
                sentence.Skipped -= OnSentenceSkipped;
                sentence.SpeakRequested -= OnIntroSpeak;
            }
            if (pause != null)
            {
                pause.Resumed -= ClosePause;
                pause.RestartRequested -= RestartPlace;
                pause.MapRequested -= GoToMap;
            }
            if (results != null)
            {
                results.ReplayRequested -= RestartPlace;
                results.NextRequested -= GoToMap;
                results.SpeakRequested -= OnWordSpeak;
            }
            if (hud != null)
            {
                hud.HelpPressed -= OnHelp;
                hud.SpeakPressed -= OnObjectiveSpeak;
                hud.PausePressed -= OpenPause;
                hud.InteractPressed -= Interact;
            }
            if (player != null) player.InputLocked = false;
        }

        // ------------------------------------------------------------------ which words

        /// <summary>The word objects of the chosen place (or all of them), in hierarchy order, at most wordsPerSet.</summary>
        void BuildSet()
        {
            set.Clear();
            var found = new List<WordTarget>(FindObjectsByType<WordTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            found.Sort((a, b) => string.CompareOrdinal(HierarchyKey(a.transform), HierarchyKey(b.transform)));

            string place = GameSession.Place;
            if (!string.IsNullOrEmpty(place))
            {
                List<WordTarget> ofPlace = found.FindAll(w => w.Place.Length == 0 || WordCatalog.SamePlace(w.Place, place));
                if (ofPlace.Count > 0) found = ofPlace;
            }
            for (int i = 0; i < found.Count && set.Count < wordsPerSet; i++) set.Add(found[i]);
        }

        static string HierarchyKey(Transform t)
        {
            string key = string.Empty;
            while (t != null)
            {
                key = t.GetSiblingIndex().ToString("D5") + "/" + key;
                t = t.parent;
            }
            return key;
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            lessonSeconds += Time.deltaTime;

            // Escape (the Android back button): pause, or go back one step in the pause menu.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) TogglePause();

            if (ScreenOpen || travelling || player == null)
            {
                reach = null;
                return;
            }

            Vector3 feet = player.transform.position;
            if (CheckDoors(feet)) return;

            reach = NearestInReach(feet);
            if (reach != null && keyboard != null &&
                (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                Interact();
        }

        /// <summary>The closest unanswered object the character can reach from where it stands, or null.</summary>
        WordTarget NearestInReach(Vector3 feet)
        {
            WordTarget nearest = null;
            float best = float.MaxValue;
            foreach (WordTarget word in set)
            {
                if (word == null || word.Completed) continue;
                float distance = word.DistanceTo(feet);
                if (distance <= word.InteractDistance && distance < best)
                {
                    best = distance;
                    nearest = word;
                }
            }
            return nearest;
        }

        /// <summary>The button of the object within reach: show its word.</summary>
        void Interact()
        {
            WordTarget word = InReach;
            if (word == null || word.Completed || player == null) return;
            reach = null;
            Meet(word);
        }

        void LateUpdate()
        {
            if (hud == null) return;
            hud.ShowInteract(InReach != null);
            WordTarget goal = ScreenOpen || travelling || !GuideOn ? null : CurrentGoal();
            if (goal == null || player == null)
            {
                hud.HideMarker();
                return;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            Vector3 position;
            string label;
            GameplayHUD.LabelSide side;
            GoalMarker(goal, out position, out label, out side);
            hud.ShowMarker(viewCamera, position, label, side);
        }

        // ------------------------------------------------------------------ doors

        /// <summary>Walking up into a door goes inside; walking down into the exit of a room goes back out.</summary>
        bool CheckDoors(Vector3 feet)
        {
            if (!player.IsMoving) return false;

            if (room == null)
            {
                if (player.Facing != Facing.Up) return false;
                foreach (BuildingDoor door in BuildingDoor.All)
                {
                    if (!door.IsAtDoor(feet)) continue;
                    Interior inside = door.Room;
                    if (inside == null) continue;
                    StartCoroutine(Travel(inside.SpawnPosition, inside));
                    return true;
                }
            }
            else if (player.Facing == Facing.Down && room.IsAtExit(feet))
            {
                BuildingDoor door = BuildingDoor.ForBuilding(room.BuildingName);
                Vector3 outside = door != null ? door.OutsidePosition : Vector3.zero;
                StartCoroutine(Travel(outside, null));
                return true;
            }
            return false;
        }

        IEnumerator Travel(Vector3 destination, Interior into)
        {
            travelling = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            if (fade != null) yield return fade.FadeTo(1f, doorFadeSeconds);

            destination.y = player.transform.position.y;
            player.transform.position = destination;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = destination;
                body.linearVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            room = into;
            if (followCamera != null) followCamera.ShowRoom(into != null ? into.Centre : (Vector3?)null);
            RefreshHud();

            if (fade != null) yield return fade.FadeTo(0f, doorFadeSeconds);
            travelling = false;
            player.InputLocked = ScreenOpen;
        }

        /// <summary>The ring goes on the goal itself, or on the door (or the exit) that leads to it.</summary>
        void GoalMarker(WordTarget goal, out Vector3 position, out string label, out GameplayHUD.LabelSide side)
        {
            Interior goalRoom = Interior.At(goal.transform.position);
            position = goal.MarkerPosition;
            label = goal.Word;
            side = GameplayHUD.LabelSide.Below;
            if (goalRoom == room) return;

            if (room != null)
            {
                position = room.ExitMarkerPosition;
                label = Loc.T("Exit", "Labas");
                side = GameplayHUD.LabelSide.Right;
                return;
            }
            BuildingDoor door = BuildingDoor.ForBuilding(goalRoom.BuildingName);
            if (door == null) return;
            position = door.DoorPosition;
            side = GameplayHUD.LabelSide.Above;
            label = goalRoom.IsSchool ? Loc.T("School", "Paaralan") : Loc.T("House", "Bahay");
        }

        // ------------------------------------------------------------------ the HUD

        WordTarget CurrentGoal()
        {
            foreach (WordTarget word in set)
                if (word != null && !word.Completed) return word;
            return null;
        }

        int DoneCount()
        {
            int done = 0;
            foreach (WordTarget word in set) if (word != null && word.Completed) done++;
            return done;
        }

        void RefreshHud()
        {
            if (hud == null) return;
            hud.SetStars(GameProgress.TotalStars());
            hud.SetProgress(DoneCount(), set.Count);

            WordTarget goal = CurrentGoal();
            if (set.Count == 0)
            {
                hud.SetObjective(null, null);
                return;
            }
            if (goal == null)
            {
                hud.SetObjective(Loc.T("Great! All done!", "Magaling! Tapos na!"), Loc.T("You found every word", "Nahanap mo na ang lahat ng salita"));
                return;
            }

            Interior goalRoom = Interior.At(goal.transform.position);
            string hint;
            if (!GuideOn) hint = Loc.T("Press ? for a guide", "Pindutin ang ? para sa gabay");
            else if (goalRoom == room) hint = Loc.T("Walk to it", "Lumakad papunta sa " + goal.Word);
            else if (room != null) hint = Loc.T("Go back outside", "Lumabas muna");
            else hint = goalRoom.IsSchool ? Loc.T("Go inside the school", "Pumasok sa paaralan") : Loc.T("Go inside the house", "Pumasok sa bahay");
            hud.SetObjective(Loc.T("Find the " + goal.Word, "Hanapin ang " + goal.Word), hint);
        }

        void OnSettingsChanged()
        {
            if (hud != null) hud.RefreshProfile();
            RefreshHud();
        }

        // ------------------------------------------------------------------ word recordings

        void OnHelp()
        {
            guided = CurrentGoal();
            RefreshHud();
            if (hud != null) hud.Pulse();
            OnObjectiveSpeak();
        }

        void OnObjectiveSpeak()
        {
            WordTarget goal = CurrentGoal();
            if (goal == null) return;
            AudioManager.PlayVoice(goal.Sound);
            onSpeak.Invoke(goal.Word);
        }

        /// <summary>A speaker button of the quiz or the spelling: the word of the object being asked.</summary>
        void OnScreenSpeak(string text)
        {
            if (active != null) AudioManager.PlayVoice(active.Sound);
            onSpeak.Invoke(text);
        }

        /// <summary>A word chip of the results.</summary>
        void OnWordSpeak(string word)
        {
            foreach (WordTarget target in set)
                if (target != null && target.Word == word) { AudioManager.PlayVoice(target.Sound); break; }
            onSpeak.Invoke(word);
        }

        // ------------------------------------------------------------------ the word card

        /// <summary>The child pressed the button of an object: show its word first (or go straight to the quiz).</summary>
        void Meet(WordTarget word)
        {
            if (!showWordFirst || intro == null)
            {
                OpenQuiz(word);
                return;
            }
            active = word;
            introOpen = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            intro.Open(word, Listed(word), GameSession.IsOlderLearner);
            AudioManager.PlayVoice(word.Sound);
        }

        void CloseIntro()
        {
            if (intro != null) intro.Close();
            introOpen = false;
            AudioManager.StopVoice();
        }

        void OnIntroContinued()
        {
            if (!introOpen) return;
            CloseIntro();
            OpenQuiz(active);
        }

        /// <summary>Back arrow of the word card: the same as closing the quiz without answering.</summary>
        void OnIntroCancelled()
        {
            CloseIntro();
            if (player != null) player.InputLocked = false;
            RefreshHud();
        }

        void OnIntroSpeak()
        {
            if (active == null) return;
            AudioManager.PlayVoice(active.Sound);
            onSpeak.Invoke(active.Word);
        }

        // ------------------------------------------------------------------ the quiz

        void OpenQuiz(WordTarget word)
        {
            if (quiz == null) return;
            active = word;
            quizOpen = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            quiz.Open(word, set.IndexOf(word), set.Count);
        }

        void CloseQuiz()
        {
            if (quiz != null) quiz.gameObject.SetActive(false);
            quizOpen = false;
            if (player != null) player.InputLocked = false;
            RefreshHud();
        }

        void OnAnswered(int mistakes)
        {
            if (active == null) return;
            active.Complete(mistakes);
            if (GameProgress.MarkWordLearned(active.Word)) newWords++;
            mistakesInSet += mistakes;

            if (DoneCount() >= set.Count)
            {
                SetComplete = true;
                StarsEarned = mistakesInSet == 0 ? 3 : (mistakesInSet <= 2 ? 2 : 1);
                string place = GameSession.Place;
                if (!string.IsNullOrEmpty(place)) GameProgress.SetStars(place, StarsEarned);
                PlayerPrefs.Save();
            }
            RefreshHud();
        }

        /// <summary>"Continue" on the quiz: the spelling screen of the word, then back to the world (or the results after the last word).</summary>
        void OnFinished()
        {
            WordTarget word = active;
            CloseQuiz();
            if (!OpenSpelling(word)) AfterSpelling(word, -1);
        }

        /// <summary>
        /// Saves the finished word for the parent dashboard. spelling: -1 not spelled, 0 right at the first check, n right after
        /// n wrong checks. tries: the fill in the blank, 0 not asked or skipped, 1 right away, more = tapped wrong words first.
        /// </summary>
        void RecordWord(WordTarget word, int spelled, int tries)
        {
            if (word == null) return;
            ProgressLog.AddWord(word.Word, word.Mistakes == 0, spelled, tries);
        }

        WordCatalog.Entry Listed(WordTarget word) => catalog != null && word != null ? catalog.Find(word.Word) : null;

        /// <summary>The spelling is over (or there was none): 6 and up fill in the blank, then the word is written down.</summary>
        void AfterSpelling(WordTarget word, int spelled)
        {
            spellingResult = spelled;
            if (OpenSentence(word)) return;
            RecordWord(word, spelled, 0);
            AfterWord();
        }

        // ------------------------------------------------------------------ fill in the blank (6 and up)

        bool OpenSentence(WordTarget word)
        {
            WordCatalog.Entry listed = Listed(word);
            if (sentence == null || word == null || !GameSession.IsOlderLearner || !SentenceScreen.CanAsk(listed)) return false;
            sentenceOpen = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            sentence.Open(listed, set.IndexOf(word), set.Count, Loc.PlaceName(GameSession.Place));
            return true;
        }

        void CloseSentence()
        {
            if (sentence != null) sentence.Close();
            sentenceOpen = false;
            AudioManager.StopVoice();
            if (player != null) player.InputLocked = false;
        }

        void OnSentenceCompleted(int tries)
        {
            RecordWord(active, spellingResult, tries);
            CloseSentence();
            AfterWord();
        }

        void OnSentenceSkipped()
        {
            RecordWord(active, spellingResult, 0);
            CloseSentence();
            AfterWord();
        }

        void AfterWord()
        {
            if (SetComplete) ShowResults();
        }

        // ------------------------------------------------------------------ spelling

        bool OpenSpelling(WordTarget word)
        {
            if (spelling == null || word == null || !word.SpellingStep || !spelling.CanSpell(word.Word)) return false;
            spellingOpen = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            spelling.Open(word, set.IndexOf(word), set.Count);
            return true;
        }

        void CloseSpelling()
        {
            if (spelling != null) spelling.gameObject.SetActive(false);
            spellingOpen = false;
            AudioManager.StopVoice();
            if (player != null) player.InputLocked = false;
        }

        void OnSpellingCompleted(int wrongChecks)
        {
            spelledWords++;
            if (wrongChecks == 0) spelledRight++;
            CloseSpelling();
            AfterSpelling(active, wrongChecks);
        }

        void OnSpellingSkipped()
        {
            CloseSpelling();
            AfterSpelling(active, -1);
        }

        void OnCancelled()
        {
            AudioManager.StopVoice();
            CloseQuiz();
        }

        // ------------------------------------------------------------------ results

        void ShowResults()
        {
            if (results == null) return;

            int correctFirstTry = 0;
            var words = new List<string>();
            foreach (WordTarget word in set)
            {
                if (word == null) continue;
                words.Add(word.Word);
                if (word.Mistakes == 0) correctFirstTry++;
            }

            string place = GameSession.Place;
            string placeName = Loc.PlaceName(place);
            string caption = placeName.ToUpperInvariant();
            string heading = (StarsEarned >= 2 ? Loc.T("Great job, ", "Magaling, ") : Loc.T("Good job, ", "Mahusay, ")) + GameSession.DisplayName + "!";
            string subtitle = string.IsNullOrEmpty(place)
                ? Loc.T("You finished the lesson", "Natapos mo ang aralin")
                : Loc.T("You finished the lesson at the " + placeName, "Natapos mo ang aralin sa " + placeName);

            ProgressLog.AddLesson(string.IsNullOrEmpty(place) ? "—" : place, correctFirstTry, words.Count, StarsEarned, Mathf.RoundToInt(lessonSeconds),
                                  spelledRight, spelledWords, string.Join(", ", words));

            resultsOpen = true;
            player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            results.Show(caption, heading, subtitle, StarsEarned, correctFirstTry, words.Count, newWords, GameSession.Attempt, words);
        }

        // ------------------------------------------------------------------ pause

        /// <summary>Opens the pause menu, or goes back one step in it. Used by Escape and the Android back button.</summary>
        public void TogglePause()
        {
            if (pause == null) return;
            if (paused) pause.Back();
            else if (!introOpen && !quizOpen && !spellingOpen && !sentenceOpen && !resultsOpen && !travelling) OpenPause();
        }

        void OpenPause()
        {
            if (pause == null || ScreenOpen || travelling) return;
            paused = true;
            Time.timeScale = 0f;
            AudioManager.StopVoice();
            if (player != null) player.InputLocked = true;
            DPadState.Clear();
            if (hud != null) hud.HideMarker();
            pause.gameObject.SetActive(true);
        }

        void ClosePause()
        {
            paused = false;
            Time.timeScale = 1f;
            if (pause != null) pause.gameObject.SetActive(false);
            if (player != null && !introOpen && !quizOpen && !spellingOpen && !sentenceOpen && !resultsOpen) player.InputLocked = false;
            RefreshHud();
        }

        // ------------------------------------------------------------------ leaving the scene

        void RestartPlace()
        {
            Time.timeScale = 1f;
            AudioManager.StopVoice();
            GameSession.RestartPlace();
        }

        void GoToMap()
        {
            Time.timeScale = 1f;
            AudioManager.StopVoice();
            GameSession.OpenWorldMap();
        }
    }
}
