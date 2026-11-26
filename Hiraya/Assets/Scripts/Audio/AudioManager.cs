using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hiraya
{
    /// <summary>
    /// Plays the music and the button click. One AudioManager lives for the whole game: it is placed in the Main Menu scene and
    /// in both playing scenes (so any scene can be started on its own), and when a second one shows up it removes itself.
    ///
    /// Music: the Main Menu scene plays the menu music, every other scene the game music. A track that is already playing is not
    /// restarted, so replaying a place does not cut the music.
    /// Volumes come from the settings screen (music, button sounds and narrator voice sliders); "Mute all sounds" silences everything.
    /// The recordings of the words are played with PlayVoice; the music is turned down while a word is being said.
    /// The click is played by the ButtonClickSound component that every button and switch of the screens carries.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] AudioSource musicSource;
        [Tooltip("Plays in the scene called Menu Scene Name (the title screen, the world map, the Salita Book, the settings).")]
        [SerializeField] AudioClip menuMusic;
        [Tooltip("Plays in every other scene (the playing scenes).")]
        [SerializeField] AudioClip gameMusic;
        [SerializeField] string menuSceneName = "MainMenu";

        [Header("Button click")]
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioClip buttonClick;
        [Tooltip("Where the click starts inside the sound file, in seconds. The file has a little silence before the click.")]
        [SerializeField, Min(0f)] float clickStart = 0.15f;
        [Tooltip("How long the click is played, in seconds. The file is longer than the click itself, so it is cut here.")]
        [SerializeField, Min(0.05f)] float clickLength = 0.5f;

        [Header("Word recordings")]
        [SerializeField] AudioSource voiceSource;
        [Tooltip("The music is multiplied by this while a word is being said.")]
        [SerializeField, Range(0f, 1f)] float musicWhileSpeaking = 0.3f;

        Coroutine stopClick;
        bool speaking;

        /// <summary>How many clicks were played since the game started (used by the tests).</summary>
        public int ClicksPlayed { get; private set; }

        public AudioSource Music => musicSource;

        public AudioSource Effects => effectsSource;

        public AudioSource Voice => voiceSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            if (musicSource != null) { musicSource.loop = true; musicSource.playOnAwake = false; }
            if (effectsSource != null) { effectsSource.loop = false; effectsSource.playOnAwake = false; }
            if (voiceSource != null) { voiceSource.loop = false; voiceSource.playOnAwake = false; }
            GameSettings.ApplyAudio();
            ApplyVolumes();
        }

        void OnEnable()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameSettings.Changed += ApplyVolumes;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.Changed -= ApplyVolumes;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (Instance != this) return;
            PlayMusicFor(SceneManager.GetActiveScene().name);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) PlayMusicFor(scene.name);
        }

        // ------------------------------------------------------------------ music

        void PlayMusicFor(string sceneName)
        {
            if (musicSource == null) return;
            AudioClip wanted = sceneName == menuSceneName ? menuMusic : gameMusic;
            if (wanted == null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }
            if (musicSource.clip == wanted && musicSource.isPlaying) return;   // keep it going
            musicSource.clip = wanted;
            musicSource.loop = true;
            musicSource.Play();
        }

        /// <summary>Music, effects and voice follow the sliders of the settings.</summary>
        void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = GameSettings.MusicVolume * (speaking ? musicWhileSpeaking : 1f);
            if (effectsSource != null) effectsSource.volume = GameSettings.SfxVolume;
            if (voiceSource != null) voiceSource.volume = GameSettings.VoiceVolume;
        }

        void Update()
        {
            if (speaking && (voiceSource == null || !voiceSource.isPlaying))
            {
                speaking = false;
                ApplyVolumes();
            }
        }

        // ------------------------------------------------------------------ word recordings

        /// <summary>Says a word (stops the one being said). Does nothing without a recording or an AudioManager.</summary>
        public static void PlayVoice(AudioClip clip)
        {
            if (Instance != null) Instance.Speak(clip);
        }

        public static void StopVoice()
        {
            if (Instance != null && Instance.voiceSource != null) Instance.voiceSource.Stop();
        }

        void Speak(AudioClip clip)
        {
            if (voiceSource == null || clip == null) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
            speaking = true;
            ApplyVolumes();
        }

        // ------------------------------------------------------------------ button click

        /// <summary>Plays the button click (does nothing when there is no AudioManager in the scene).</summary>
        public static void PlayClick()
        {
            if (Instance != null) Instance.Click();
        }

        void Click()
        {
            if (effectsSource == null || buttonClick == null) return;
            ClicksPlayed++;
            effectsSource.Stop();
            effectsSource.clip = buttonClick;
            effectsSource.time = Mathf.Min(clickStart, Mathf.Max(0f, buttonClick.length - 0.05f));
            effectsSource.Play();
            if (stopClick != null) StopCoroutine(stopClick);
            stopClick = StartCoroutine(StopClickAfter());
        }

        IEnumerator StopClickAfter()
        {
            yield return new WaitForSecondsRealtime(clickLength);   // real time, so it also ends while the game is paused
            if (effectsSource != null) effectsSource.Stop();
            stopClick = null;
        }
    }
}
