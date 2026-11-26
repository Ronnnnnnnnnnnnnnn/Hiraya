using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Builds the AudioManager prefab (Assets/Prefabs/AudioManager.prefab) from the
    /// three sounds in Assets/Sound and puts one copy of it into the Main Menu scene and both playing scenes.
    ///   AudioManager > Music (AudioSource), Effects (AudioSource), Voice (AudioSource, the word recordings)
    /// Clips (change them on the AudioManager object if you rename or replace the files):
    ///   main menu.wav       menu music (loops)
    ///   Background Music.mp3  game music (loops; set to Streaming so it is not loaded whole into memory)
    ///   For button.wav      the button click (only its first half second is played)
    /// The screens' prefabs get a ButtonClickSound on every button and switch when they are built (AddClickSounds).
    /// Adding to a scene only replaces an older AudioManager; nothing else in the scene is touched.
    /// </summary>
    public static class HirayaAudioSetup
    {
        public const string SoundFolder = "Assets/Sound";
        public const string MenuMusicPath = SoundFolder + "/main menu.wav";
        public const string GameMusicPath = SoundFolder + "/Background Music.mp3";
        public const string ButtonClickPath = SoundFolder + "/For button.wav";
        public const string PrefabPath = "Assets/Prefabs/AudioManager.prefab";

        public static void BuildAndAddToScenes()
        {
            BuildPrefab();
            AddToScene(HirayaSceneSetup.MainMenuScenePath);
            AddToScene(HirayaSceneSetup.KinderScenePath);
            AddToScene(HirayaSceneSetup.GradeOneScenePath);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ the prefab

        public static GameObject BuildPrefab()
        {
            HirayaCharacterSetup.EnsureFolder(HirayaTitleSetup.PrefabFolder);
            StreamTheMusic();

            var root = new GameObject("AudioManager");
            var manager = root.AddComponent<AudioManager>();

            AudioSource music = AddSource(root.transform, "Music", true);
            AudioSource effects = AddSource(root.transform, "Effects", false);
            AudioSource voice = AddSource(root.transform, "Voice", false);

            var so = new SerializedObject(manager);
            so.FindProperty("musicSource").objectReferenceValue = music;
            so.FindProperty("effectsSource").objectReferenceValue = effects;
            so.FindProperty("voiceSource").objectReferenceValue = voice;
            so.FindProperty("menuMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
            so.FindProperty("gameMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GameMusicPath);
            so.FindProperty("buttonClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ButtonClickPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static AudioSource AddSource(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;   // plain 2D sound
            return source;
        }

        /// <summary>The game music is almost three minutes long: stream it instead of unpacking it into memory (important on a phone).</summary>
        static void StreamTheMusic()
        {
            var importer = AssetImporter.GetAtPath(GameMusicPath) as AudioImporter;
            if (importer == null) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming) return;
            settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ the scenes

        /// <summary>Puts (a fresh copy of) the AudioManager prefab into a scene.</summary>
        public static void AddToScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject old = GameObject.Find("AudioManager");
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find("AudioManager");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ the button click

        /// <summary>Gives every Button and Toggle below root a ButtonClickSound. Called by the screen builders just before a prefab is saved.</summary>
        internal static void AddClickSounds(GameObject root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.GetComponent<ButtonClickSound>() == null) button.gameObject.AddComponent<ButtonClickSound>();
            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
                if (toggle.GetComponent<ButtonClickSound>() == null) toggle.gameObject.AddComponent<ButtonClickSound>();
        }
    }
}
