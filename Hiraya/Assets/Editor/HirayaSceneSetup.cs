using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Puts the prefabs into the two playing scenes,
    /// wires up the Main Menu and fills the Build Profiles scene list.
    /// </summary>
    public static class HirayaSceneSetup
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string KinderScenePath = "Assets/Scenes/PlayingSceneForKinder.unity";
        public const string GradeOneScenePath = "Assets/Scenes/PlayingsceneForGrade1andUp.unity";

        /// <summary>
        /// false = perspective camera (things look bigger when closer, like a 3D game).
        /// true  = orthographic camera (flat, no perspective). Used when the scenes are filled by this tool;
        /// in an existing scene just tick or untick "Projection" on the Main Camera.
        /// </summary>
        const bool OrthographicCamera = false;

        // Camera look: a steep top-down angle, pulled far back with a narrow field of view so there is little
        // perspective stretching and the player is small on screen (the Pokemon-style framing of the reference).
        const float CameraPitch = 62f;            // degrees looking down
        const float PerspectiveFieldOfView = 25f;
        const float PerspectiveDistance = 40f;    // how far the camera is from the player
        const float OrthographicSize = 9f;        // used only when OrthographicCamera is true
        const float OrthographicDistance = 20f;

        // ------------------------------------------------------------------ everything

        public static void BuildEverything()
        {
            HirayaCharacterSetup.SetupSkins();
            HirayaCharacterSetup.CreatePlayerPrefab();
            HirayaCharacterSetup.CreateDPadPrefab();
            HirayaMapSetup.BuildMap();
            SetupMainMenu();
            HirayaTitleSetup.BuildTitleScreen();
            HirayaGameplaySetup.BuildPrefab();
            HirayaAudioSetup.BuildPrefab();
            HirayaItems.BuildAll();
            SetupGameScene(KinderScenePath);
            SetupGameScene(GradeOneScenePath);
            HirayaInteriors.BuildAll();   // doors and rooms (rooms already in a scene are kept)
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Hiraya: build everything finished.");
        }

        // ------------------------------------------------------------------ build settings

        public static void SetupBuildSettings()
        {
            // MainMenu first: the game starts there and Play leads on to the playing scenes.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(KinderScenePath, true),
                new EditorBuildSettingsScene(GradeOneScenePath, true)
            };
        }

        // ------------------------------------------------------------------ main menu

        public static void SetupMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            GameObject canvasGo = GameObject.Find("Main Canvas");
            Transform ageSelect = canvasGo.transform.Find("Age Select");
            Transform mainMenu = canvasGo.transform.Find("Mainmenu");
            var playButton = mainMenu.Find("Play").GetComponent<Button>();
            var ageInput = ageSelect.Find("Age input").GetComponent<TMP_InputField>();
            var header = ageSelect.Find("Ageheader").gameObject;

            // Make this safe to run again.
            foreach (string old in new[] { "Boy Button", "Girl Button", "Continue Button", "Message" })
            {
                Transform t = ageSelect.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            ageSelect.gameObject.SetActive(true);
            var boy = CloneButton(playButton.gameObject, ageSelect, "Boy Button", "Boy", new Vector2(-85f, -45f));
            var girl = CloneButton(playButton.gameObject, ageSelect, "Girl Button", "Girl", new Vector2(85f, -45f));
            var next = CloneButton(playButton.gameObject, ageSelect, "Continue Button", "Continue", new Vector2(0f, -95f));

            var message = Object.Instantiate(header, ageSelect, false);
            message.name = "Message";
            var messageRect = (RectTransform)message.transform;
            messageRect.anchoredPosition = new Vector2(0f, -140f);
            messageRect.sizeDelta = new Vector2(420f, 36f);
            var messageText = message.GetComponent<TMP_Text>();
            messageText.text = string.Empty;
            messageText.fontSize = 20f;
            messageText.color = new Color(0.85f, 0.15f, 0.15f, 1f);

            var controller = canvasGo.GetComponent<MainMenuController>();
            if (controller == null) controller = canvasGo.AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu.gameObject;
            so.FindProperty("ageSelectPanel").objectReferenceValue = ageSelect.gameObject;
            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("ageInput").objectReferenceValue = ageInput;
            so.FindProperty("boyButton").objectReferenceValue = boy;
            so.FindProperty("girlButton").objectReferenceValue = girl;
            so.FindProperty("continueButton").objectReferenceValue = next;
            so.FindProperty("messageText").objectReferenceValue = messageText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The game opens on the main menu; Play reveals the age screen.
            mainMenu.gameObject.SetActive(true);
            ageSelect.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Button CloneButton(GameObject source, Transform parent, string name, string label, Vector2 position)
        {
            var go = Object.Instantiate(source, parent, false);
            go.name = name;
            go.SetActive(true);
            ((RectTransform)go.transform).anchoredPosition = position;
            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            return go.GetComponent<Button>();
        }

        // ------------------------------------------------------------------ playing scenes

        public static void SetupBothGameScenes()
        {
            SetupGameScene(KinderScenePath);
            SetupGameScene(GradeOneScenePath);
        }

        public static void SetupGameScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Make this safe to run again: remove what an earlier run added.
            foreach (string old in new[] { "HirayaMap", "Buildings", "Player", "DPadCanvas", "GameplayUI", "AudioManager", "EventSystem" })
            {
                GameObject existing = GameObject.Find(old);
                while (existing != null)
                {
                    Object.DestroyImmediate(existing);
                    existing = GameObject.Find(old);
                }
            }

            var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaMapSetup.MapPrefabPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaCharacterSetup.PlayerPrefabPath);
            var dpadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaCharacterSetup.DPadPrefabPath);

            var map = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
            map.transform.position = Vector3.zero;

            var buildings = new GameObject("Buildings");
            buildings.transform.position = Vector3.zero;

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = HirayaMapSetup.SpawnWorldPosition();

            var dpad = (GameObject)PrefabUtility.InstantiatePrefab(dpadPrefab);

            // The heads-up display and the quiz (Hiraya > Gameplay builds this prefab).
            var gameplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaGameplaySetup.PrefabPath);
            if (gameplayPrefab != null) PrefabUtility.InstantiatePrefab(gameplayPrefab);

            // The music and the button click (Hiraya > Audio builds this prefab).
            var audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HirayaAudioSetup.PrefabPath);
            if (audioPrefab != null) PrefabUtility.InstantiatePrefab(audioPrefab);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            // Camera: angled, follows the player.
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            cam.orthographic = OrthographicCamera;
            if (OrthographicCamera) cam.orthographicSize = OrthographicSize;   // half the height of the view in world units (the zoom)
            else cam.fieldOfView = PerspectiveFieldOfView;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f, 1f);
            cam.transform.rotation = Quaternion.Euler(CameraPitch, 0f, 0f);

            var follow = cam.GetComponent<FollowCamera>();
            if (follow == null) follow = cam.gameObject.AddComponent<FollowCamera>();
            // Where the camera sits relative to the player: straight back along the tilted view direction.
            float distance = OrthographicCamera ? OrthographicDistance : PerspectiveDistance;
            float pitch = CameraPitch * Mathf.Deg2Rad;
            var offset = new Vector3(0f, Mathf.Sin(pitch) * distance, -Mathf.Cos(pitch) * distance);
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.FindProperty("offset").vector3Value = offset;
            so.FindProperty("mapHalfSize").vector2Value = new Vector2(HirayaMapSetup.MapHalfSize, HirayaMapSetup.MapHalfSize);
            so.ApplyModifiedPropertiesWithoutUndo();
            cam.transform.position = player.transform.position + offset;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
