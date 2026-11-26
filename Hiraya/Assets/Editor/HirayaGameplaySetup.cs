using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static Hiraya.EditorTools.HirayaTitleSetup;
using static Hiraya.EditorTools.HirayaWorldMapSetup;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Builds the heads-up display (wireframe 03) and the quiz screen
    /// (wireframe 04) as one normal UI prefab, Assets/Prefabs/UI/GameplayUI.prefab, and puts one copy of it into both playing scenes.
    ///   GameplayUI > HUD        (profile, goal, stars, pause, word dots, "?" help, ring around the object)
    ///              > QuizPanel  (hidden until the character walks up to a word)
    /// The items to find are the prefabs in Assets/Prefabs/Items (Hiraya > Items), placed by hand in the scenes.
    /// The small pictures are drawn into Assets/UI/Gameplay so you can swap in your own art (the word pictures are in Words).
    /// Adding to a scene only replaces an older GameplayUI; nothing else in the scene is touched.
    /// </summary>
    public static class HirayaGameplaySetup
    {
        public const string IconFolder = "Assets/UI/Gameplay";
        public const string WordFolder = IconFolder + "/Words";
        public const string PrefabPath = HirayaTitleSetup.PrefabFolder + "/GameplayUI.prefab";

        static readonly Color PictureGray = new Color(0.93f, 0.93f, 0.91f, 1f);
        static readonly Color RightGreen = new Color(0.16f, 0.58f, 0.33f, 1f);

        class GameIcons
        {
            public Sprite speaker, pause, check, cross, star, arrow, ring, restart, upuan, mesa, kama;
        }

        // ------------------------------------------------------------------ menu entry points

        public static void BuildPrefabOnly()
        {
            BuildPrefab();
            AssetDatabase.SaveAssets();
        }

        public static void AddToBothScenes()
        {
            BuildPrefab();
            AddToScene(HirayaSceneSetup.KinderScenePath);
            AddToScene(HirayaSceneSetup.GradeOneScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Puts (a fresh copy of) the GameplayUI prefab into a scene.</summary>
        public static void AddToScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject old = GameObject.Find("GameplayUI");
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find("GameplayUI");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ the prefab

        public static GameObject BuildPrefab()
        {
            HirayaCharacterSetup.EnsureFolder(IconFolder);
            HirayaCharacterSetup.EnsureFolder(WordFolder);
            HirayaCharacterSetup.EnsureFolder(HirayaTitleSetup.PrefabFolder);

            Sprites sprites = LoadTitleSprites();
            GameIcons icons = MakeIcons();
            var orangeStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/WorldMap/StarIcon.png");
            var fin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(HirayaCharacterSetup.CharactersFolder + "/Fin.asset");
            var fiona = AssetDatabase.LoadAssetAtPath<CharacterSkin>(HirayaCharacterSetup.CharactersFolder + "/Fiona.asset");

            var rootGo = new GameObject("GameplayUI", typeof(RectTransform));
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;   // above the on-screen D-pad (10)
            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            rootGo.AddComponent<GraphicRaycaster>();
            var director = rootGo.AddComponent<GameplayDirector>();

            RectTransform hudRect = NewStretched("HUD", rootGo.transform);
            hudRect.gameObject.AddComponent<SafeArea>();   // away from a camera notch
            GameplayHUD hud = BuildHud(hudRect, sprites, icons, orangeStar, fin != null ? fin.front : null, fiona != null ? fiona.front : null);

            RectTransform wordRect = NewStretched("WordPanel", rootGo.transform);
            WordIntroScreen intro = BuildWordIntro(wordRect, sprites, icons);
            wordRect.gameObject.SetActive(false);

            RectTransform quizRect = NewStretched("QuizPanel", rootGo.transform);
            QuizScreen quiz = BuildQuiz(quizRect, sprites, icons);
            quizRect.gameObject.SetActive(false);

            RectTransform spellingRect = NewStretched("SpellingPanel", rootGo.transform);
            SpellingScreen spelling = BuildSpelling(spellingRect, sprites, icons);
            spellingRect.gameObject.SetActive(false);

            RectTransform sentenceRect = NewStretched("SentencePanel", rootGo.transform);
            SentenceScreen sentence = BuildSentence(sentenceRect, sprites, icons);
            sentenceRect.gameObject.SetActive(false);

            RectTransform resultsRect = NewStretched("ResultsPanel", rootGo.transform);
            ResultsScreen results = BuildResults(resultsRect, sprites, icons, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/WorldMap/ChevronIcon.png"));
            resultsRect.gameObject.SetActive(false);

            // The pause menu is last so it is drawn above everything else.
            RectTransform pauseRect = NewStretched("PausePanel", rootGo.transform);
            PauseScreen pause = BuildPause(pauseRect, sprites, icons);
            pauseRect.gameObject.SetActive(false);

            var so = new SerializedObject(director);
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("intro").objectReferenceValue = intro;
            so.FindProperty("sentence").objectReferenceValue = sentence;
            so.FindProperty("catalog").objectReferenceValue = HirayaSalitaBookSetup.EnsureCatalog();
            so.FindProperty("quiz").objectReferenceValue = quiz;
            so.FindProperty("spelling").objectReferenceValue = spelling;
            so.FindProperty("results").objectReferenceValue = results;
            so.FindProperty("pause").objectReferenceValue = pause;
            so.FindProperty("fade").objectReferenceValue = BuildFade(rootGo.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            HirayaAudioSetup.AddClickSounds(rootGo);   // every button plays the click
            HirayaLanguage.Apply(rootGo);               // English, with the Filipino as the other language
            HirayaSkin.ApplyToGameplay(rootGo);         // your button art
            HirayaLanguage.AddLargeText(rootGo);        // the Large text switch
            HirayaLayout.FitAllScreens(rootGo, "HUD");  // nothing cut off on long phones or on tablets
            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);
            return prefab;
        }

        /// <summary>The black sheet that fades over the screen when the character walks through a door.</summary>
        static ScreenFade BuildFade(Transform root)
        {
            RectTransform rect = NewStretched("Fade", root);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var fade = rect.gameObject.AddComponent<ScreenFade>();
            rect.gameObject.SetActive(false);
            return fade;
        }

        static Sprites LoadTitleSprites()
        {
            Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(HirayaTitleSetup.IconFolder + "/" + file);
            return new Sprites
            {
                rounded = Load("RoundedShape.png"), play = Load("PlayIcon.png"), lockIcon = Load("LockIcon.png"),
                soundOn = Load("SoundOnIcon.png"), soundOff = Load("SoundOffIcon.png"), settings = Load("SettingsIcon.png"), back = Load("BackIcon.png")
            };
        }

        // ------------------------------------------------------------------ HUD (wireframe 03)

        static GameplayHUD BuildHud(RectTransform hudRect, Sprites sprites, GameIcons icons, Sprite orangeStar, Sprite boyPortrait, Sprite girlPortrait)
        {
            var hud = hudRect.gameObject.AddComponent<GameplayHUD>();

            // Who is playing (top left).
            RectTransform profile = Card(hudRect, "ProfilePill", new Vector2(42f, 40f), new Vector2(250f, 100f), 50f, Color.white, LineGray, sprites.rounded);
            RoundedAt(profile, "PortraitCircle", new Vector2(10f, 10f), new Vector2(80f, 80f), PictureGray, sprites.rounded);
            Image portrait = IconAt(profile, "Portrait", new Vector2(10f, 6f), new Vector2(80f, 88f), boyPortrait, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.rectTransform.sizeDelta = new Vector2(104f, 104f);
            portrait.rectTransform.anchoredPosition = new Vector2(50f, -50f);
            TextMeshProUGUI profileName = TextAt(profile, "Name", "Boy", 36f, Ink, FontStyles.Bold, new Vector2(112f, 26f), new Vector2(120f, 48f), TextAlignmentOptions.MidlineLeft);

            // The goal (top middle): speaker, "Hanapin ang upuan", "Lumakad papunta sa upuan".
            RectTransform objective = Card(hudRect, "ObjectivePill", new Vector2(562f, 34f), new Vector2(798f, 126f), 36f, Color.white, LineGray, sprites.rounded);
            Image speakerCircle = RoundedAt(objective, "SpeakerCircle", new Vector2(28f, 23f), new Vector2(80f, 80f), new Color(Orange.r, Orange.g, Orange.b, 0.15f), sprites.rounded);
            IconAt(speakerCircle.transform, "Icon", Vector2.zero, Vector2.one * 46f, icons.speaker, Orange, true).raycastTarget = false;
            var speakButton = speakerCircle.gameObject.AddComponent<Button>();
            speakButton.transition = Selectable.Transition.None;
            speakButton.targetGraphic = speakerCircle;
            TextMeshProUGUI objectiveTitle = TextAt(objective, "Title", "Hanapin ang upuan", 42f, Ink, FontStyles.Bold, new Vector2(130f, 20f), new Vector2(640f, 56f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI objectiveSubtitle = TextAt(objective, "Subtitle", "Lumakad papunta sa upuan", 26f, SoftInk, FontStyles.Normal, new Vector2(130f, 76f), new Vector2(640f, 36f), TextAlignmentOptions.MidlineLeft);

            // Stars (top right, left of pause).
            RectTransform starPill = Card(hudRect, "StarPill", new Vector2(1418f, 40f), new Vector2(200f, 92f), 46f, Color.white, LineGray, sprites.rounded);
            IconAt(starPill, "StarIcon", new Vector2(28f, 26f), new Vector2(40f, 40f), orangeStar, Color.white).raycastTarget = false;
            TextMeshProUGUI starCount = TextAt(starPill, "Count", "0", 42f, Ink, FontStyles.Bold, new Vector2(84f, 20f), new Vector2(100f, 52f), TextAlignmentOptions.MidlineLeft);

            // Pause.
            RectTransform pause = Card(hudRect, "PauseButton", new Vector2(1766f, 36f), new Vector2(108f, 108f), 30f, Color.white, LineGray, sprites.rounded);
            IconAt(pause, "Icon", Vector2.zero, Vector2.one * 54f, icons.pause, Ink, true).raycastTarget = false;
            var pauseButton = pause.gameObject.AddComponent<Button>();
            pauseButton.transition = Selectable.Transition.None;
            pauseButton.targetGraphic = pause.Find("Fill").GetComponent<Image>();

            // Word progress (bottom middle).
            RectTransform progress = Card(hudRect, "ProgressPill", new Vector2(700f, 960f), new Vector2(523f, 84f), 42f, Color.white, LineGray, sprites.rounded);
            // The dots and the text sit in one row that stays centred however many dots are shown.
            RectTransform progressRow = NewStretched("Row", progress);
            SetupRow(progressRow, 16f);
            var dots = new Image[5];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = AddRounded(progressRow, "Dot_" + (i + 1), Vector2.one * 30f, LineGray, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
                FixedSize(dots[i].gameObject, 30f, 30f);
            }
            TextMeshProUGUI progressLabel = AddText(progressRow, "Label", "0 sa 5 salita", 26f, Ink, FontStyles.Normal, new Vector2(190f, 40f),
                                                    new Vector2(0.5f, 0.5f), Vector2.zero, 0f, TextAlignmentOptions.MidlineLeft);
            FixedSize(progressLabel.gameObject, 190f, 40f);

            // "?" help, next to the pause button.
            RectTransform help = Card(hudRect, "HelpButton", new Vector2(1638f, 36f), new Vector2(108f, 108f), 30f, Color.white, LineGray, sprites.rounded);
            TextAt(help, "Label", "?", 62f, Orange, FontStyles.Bold, new Vector2(0f, 4f), new Vector2(108f, 100f), TextAlignmentOptions.Center);
            var helpButton = help.gameObject.AddComponent<Button>();
            helpButton.transition = Selectable.Transition.None;
            helpButton.targetGraphic = help.Find("Fill").GetComponent<Image>();

            // Appears while an object is within reach; the word only opens when it is pressed.
            Button interact = AddPillButton(hudRect, "InteractButton", new Vector2(340f, 120f), Orange, sprites.rounded, new Vector2(1f, 0f), new Vector2(-219f, 204f));
            AddText(interact.transform, "Label", "Tingnan", 46f, Color.white, FontStyles.Bold, new Vector2(320f, 80f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);
            interact.gameObject.SetActive(false);

            // The dashed orange ring and the word label around the object to find.
            RectTransform marker = NewRect("TargetMarker", hudRect);
            Place(marker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 190f);
            AddIcon(marker, "Ring", Vector2.one * 190f, icons.ring, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero).raycastTarget = false;
            Image labelPill = AddRounded(marker, "LabelPill", new Vector2(240f, 60f), Orange, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(0f, -135f));
            labelPill.raycastTarget = false;
            TextMeshProUGUI markerLabel = AddText(labelPill.transform, "Label", "upuan", 32f, Color.white, FontStyles.Bold, new Vector2(224f, 52f),
                                                  new Vector2(0.5f, 0.5f), Vector2.zero, 0f, TextAlignmentOptions.Center);
            markerLabel.enableAutoSizing = true;
            markerLabel.fontSizeMin = 20f;
            markerLabel.fontSizeMax = 32f;
            marker.gameObject.SetActive(false);

            // The arrow at the screen edge while the object is out of view.
            RectTransform arrow = NewRect("EdgeArrow", hudRect);
            Place(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 96f);
            AddRounded(arrow, "Circle", Vector2.one * 96f, Orange, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero).raycastTarget = false;
            AddIcon(arrow, "Icon", Vector2.one * 58f, icons.arrow, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero).raycastTarget = false;
            arrow.gameObject.SetActive(false);

            var so = new SerializedObject(hud);
            so.FindProperty("profileName").objectReferenceValue = profileName;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("boyPortrait").objectReferenceValue = boyPortrait;
            so.FindProperty("girlPortrait").objectReferenceValue = girlPortrait;
            so.FindProperty("starCount").objectReferenceValue = starCount;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("objectiveRoot").objectReferenceValue = objective.gameObject;
            so.FindProperty("objectiveTitle").objectReferenceValue = objectiveTitle;
            so.FindProperty("objectiveSubtitle").objectReferenceValue = objectiveSubtitle;
            so.FindProperty("speakButton").objectReferenceValue = speakButton;
            so.FindProperty("progressRoot").objectReferenceValue = progress.gameObject;
            so.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("helpButton").objectReferenceValue = helpButton;
            so.FindProperty("interactButton").objectReferenceValue = interact;
            so.FindProperty("markerRoot").objectReferenceValue = marker;
            so.FindProperty("markerLabel").objectReferenceValue = markerLabel;
            so.FindProperty("edgeArrow").objectReferenceValue = arrow;
            SerializedProperty dotsProperty = so.FindProperty("dots");
            dotsProperty.arraySize = dots.Length;
            for (int i = 0; i < dots.Length; i++) dotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        // ------------------------------------------------------------------ the word card (before the quiz)

        static WordIntroScreen BuildWordIntro(RectTransform panel, Sprites sprites, GameIcons icons)
        {
            var intro = panel.gameObject.AddComponent<WordIntroScreen>();

            var background = NewStretched("Background", panel).gameObject.AddComponent<Image>();
            background.color = PaperColor;

            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);

            TextMeshProUGUI heading = TextAt(panel, "Heading", "Bagong salita", 46f, SoftInk, FontStyles.Bold, new Vector2(560f, 50f), new Vector2(800f, 70f), TextAlignmentOptions.Center);

            // Left: the picture and the Listen button.
            RectTransform box = Card(panel, "PictureBox", new Vector2(140f, 170f), new Vector2(640f, 520f), 36f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI placeholder = TextAt(box, "Placeholder", "LARAWAN", 26f, SoftInk, FontStyles.Normal, new Vector2(0f, 230f), new Vector2(640f, 60f), TextAlignmentOptions.Center, 3f);
            Image picture = IconAt(box, "Picture", new Vector2(50f, 40f), new Vector2(540f, 440f), null, Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            picture.gameObject.SetActive(false);
            Button listen = MenuButton(panel, "ListenButton", new Vector2(280f, 730f), new Vector2(360f, 105f), "Pakinggan", icons.speaker, Orange, false, Ink, Color.white, LineGray, sprites, 40f);

            // Right: the word and its meaning, then (6 and up) the description and the example.
            RectTransform group = NewRect("WordGroup", panel);
            Place(group, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(850f, -170f), new Vector2(930f, 230f));
            TextMeshProUGUI word = TextAt(group, "Word", "upuan", 120f, Ink, FontStyles.Bold, new Vector2(0f, 0f), new Vector2(930f, 150f), TextAlignmentOptions.Center);
            word.enableAutoSizing = true;
            word.fontSizeMin = 60f;
            word.fontSizeMax = 120f;
            TextMeshProUGUI meaning = TextAt(group, "Meaning", "Chair", 46f, SoftInk, FontStyles.Normal, new Vector2(0f, 150f), new Vector2(930f, 64f), TextAlignmentOptions.Center);

            RectTransform details = NewStretched("Details", panel);
            RectTransform about = Card(details, "DescriptionCard", new Vector2(850f, 420f), new Vector2(930f, 190f), 32f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI aboutLabel = TextAt(about, "Label", "Ano ito?", 24f, Orange, FontStyles.Bold, new Vector2(36f, 18f), new Vector2(860f, 34f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI aboutText = TextAt(about, "Text", "A chair is a seat with a back, made for one person.", 36f, Ink, FontStyles.Normal, new Vector2(36f, 58f), new Vector2(860f, 112f), TextAlignmentOptions.TopLeft);
            aboutText.enableAutoSizing = true;
            aboutText.fontSizeMin = 24f;
            aboutText.fontSizeMax = 36f;
            RectTransform sample = Card(details, "ExampleCard", new Vector2(850f, 630f), new Vector2(930f, 220f), 32f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI sampleLabel = TextAt(sample, "Label", "Halimbawa", 24f, Orange, FontStyles.Bold, new Vector2(36f, 18f), new Vector2(860f, 34f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI sampleText = TextAt(sample, "Sentence", "Umupo si Ana sa upuan.", 46f, Ink, FontStyles.Bold, new Vector2(36f, 60f), new Vector2(860f, 80f), TextAlignmentOptions.MidlineLeft);
            sampleText.enableAutoSizing = true;
            sampleText.fontSizeMin = 28f;
            sampleText.fontSizeMax = 46f;
            TextMeshProUGUI sampleEnglish = TextAt(sample, "English", "Ana sat on the chair.", 30f, SoftInk, FontStyles.Italic, new Vector2(36f, 148f), new Vector2(860f, 50f), TextAlignmentOptions.MidlineLeft);
            sampleEnglish.enableAutoSizing = true;
            sampleEnglish.fontSizeMin = 20f;
            sampleEnglish.fontSizeMax = 30f;

            // Bottom right: nothing of the quiz is there, so a quick second tap does no harm.
            Button next = AddPillButton(panel, "NextButton", new Vector2(260f, 105f), Orange, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(785f, -430f));
            AddText(next.transform, "Label", "Susunod", 40f, Color.white, FontStyles.Bold, new Vector2(240f, 70f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);

            var so = new SerializedObject(intro);
            so.FindProperty("heading").objectReferenceValue = heading;
            so.FindProperty("wordText").objectReferenceValue = word;
            so.FindProperty("meaningText").objectReferenceValue = meaning;
            so.FindProperty("pictureImage").objectReferenceValue = picture;
            so.FindProperty("picturePlaceholder").objectReferenceValue = placeholder.gameObject;
            so.FindProperty("wordGroup").objectReferenceValue = group;
            so.FindProperty("details").objectReferenceValue = details.gameObject;
            so.FindProperty("descriptionLabel").objectReferenceValue = aboutLabel;
            so.FindProperty("descriptionText").objectReferenceValue = aboutText;
            so.FindProperty("exampleLabel").objectReferenceValue = sampleLabel;
            so.FindProperty("exampleText").objectReferenceValue = sampleText;
            so.FindProperty("exampleEnglishText").objectReferenceValue = sampleEnglish;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("listenButton").objectReferenceValue = listen;
            so.FindProperty("nextButton").objectReferenceValue = next;
            so.ApplyModifiedPropertiesWithoutUndo();
            return intro;
        }

        // ------------------------------------------------------------------ fill in the blank (6 and up)

        static SentenceScreen BuildSentence(RectTransform panel, Sprites sprites, GameIcons icons)
        {
            var screen = panel.gameObject.AddComponent<SentenceScreen>();

            var background = NewStretched("Background", panel).gameObject.AddComponent<Image>();
            background.color = PaperColor;

            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);

            TextMeshProUGUI title = TextAt(panel, "Title", "Punan ang Patlang", 58f, Ink, FontStyles.Bold, new Vector2(162f, 34f), new Vector2(900f, 72f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI subtitle = TextAt(panel, "Subtitle", "Paaralan", 26f, SoftInk, FontStyles.Normal, new Vector2(162f, 104f), new Vector2(900f, 36f), TextAlignmentOptions.MidlineLeft);
            RectTransform pill = Card(panel, "ProgressPill", new Vector2(1580f, 48f), new Vector2(264f, 76f), 38f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI progress = TextAt(pill, "Label", "Salita 1 sa 3", 28f, Ink, FontStyles.Bold, Vector2.zero, new Vector2(264f, 76f), TextAlignmentOptions.Center);

            TextAt(panel, "Instruction", "Piliin ang salitang bubuo sa pangungusap", 34f, SoftInk, FontStyles.Normal, new Vector2(160f, 180f), new Vector2(1600f, 50f), TextAlignmentOptions.Center);
            RectTransform sheet = Card(panel, "SentenceCard", new Vector2(160f, 250f), new Vector2(1600f, 310f), 40f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI sentence = TextAt(sheet, "Sentence", "Pagod ako, kaya umupo ako sa ___.", 68f, Ink, FontStyles.Bold, new Vector2(60f, 30f), new Vector2(1480f, 180f), TextAlignmentOptions.Center);
            sentence.enableAutoSizing = true;
            sentence.fontSizeMin = 40f;
            sentence.fontSizeMax = 68f;
            TextMeshProUGUI hint = TextAt(sheet, "Hint", "I was tired, so I sat on the ___.", 36f, SoftInk, FontStyles.Italic, new Vector2(60f, 220f), new Vector2(1480f, 60f), TextAlignmentOptions.Center);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 24f;
            hint.fontSizeMax = 36f;

            TextMeshProUGUI status = TextAt(panel, "Status", "Subukan muli!", 40f, Ink, FontStyles.Bold, new Vector2(160f, 580f), new Vector2(1600f, 60f), TextAlignmentOptions.Center);

            var choices = new SentenceScreen.Choice[3];
            for (int i = 0; i < choices.Length; i++)
            {
                RectTransform card = Card(panel, "Choice_" + (i + 1), new Vector2(230f + i * 500f, 670f), new Vector2(460f, 150f), 36f, Color.white, LineGray, sprites.rounded);
                TextMeshProUGUI label = TextAt(card, "Word", "upuan", 52f, Ink, FontStyles.Bold, new Vector2(20f, 20f), new Vector2(420f, 110f), TextAlignmentOptions.Center);
                label.enableAutoSizing = true;
                label.fontSizeMin = 30f;
                label.fontSizeMax = 52f;
                var button = card.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = card.Find("Fill").GetComponent<Image>();
                choices[i] = new SentenceScreen.Choice { button = button, fill = card.Find("Fill").GetComponent<Image>(), border = card.Find("Border").GetComponent<Image>(), label = label };
            }

            Button hintButton = MenuButton(panel, "HintButton", new Vector2(160f, 910f), new Vector2(320f, 105f), "Pahiwatig", null, Color.white, false, Ink, Color.white, LineGray, sprites, 40f);
            Button next = AddPillButton(panel, "NextButton", new Vector2(340f, 105f), Orange, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(740f, -425f));
            AddText(next.transform, "Label", "Magpatuloy", 40f, Color.white, FontStyles.Bold, new Vector2(320f, 70f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);

            var so = new SerializedObject(screen);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("progress").objectReferenceValue = progress;
            so.FindProperty("sentenceText").objectReferenceValue = sentence;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.FindProperty("status").objectReferenceValue = status;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("hintButton").objectReferenceValue = hintButton;
            so.FindProperty("nextButton").objectReferenceValue = next;
            SerializedProperty list = so.FindProperty("choices");
            list.arraySize = choices.Length;
            for (int i = 0; i < choices.Length; i++)
            {
                SerializedProperty c = list.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("button").objectReferenceValue = choices[i].button;
                c.FindPropertyRelative("fill").objectReferenceValue = choices[i].fill;
                c.FindPropertyRelative("border").objectReferenceValue = choices[i].border;
                c.FindPropertyRelative("label").objectReferenceValue = choices[i].label;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        // ------------------------------------------------------------------ quiz (wireframe 04)

        static QuizScreen BuildQuiz(RectTransform panel, Sprites sprites, GameIcons icons)
        {
            var quiz = panel.gameObject.AddComponent<QuizScreen>();

            // A plain sheet that also stops taps from reaching the world behind it.
            var background = NewStretched("Background", panel).gameObject.AddComponent<Image>();
            background.color = PaperColor;

            // Top bar: back, "1 sa 5" with dots, sound.
            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);
            Image soundIcon;
            Button soundButton = AddRoundButton(panel, "SoundButton", new Vector2(1f, 1f), new Vector2(-40f, -48f), sprites.rounded, sprites.soundOn, out soundIcon);
            ((RectTransform)soundButton.transform).pivot = new Vector2(1f, 1f);

            RectTransform progressRow = NewRect("Progress", panel);
            Place(progressRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(500f, 60f));
            SetupRow(progressRow, 20f);
            var dots = new Image[5];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = AddRounded(progressRow, "Dot_" + (i + 1), Vector2.one * 28f, LineGray, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
                FixedSize(dots[i].gameObject, 28f, 28f);
            }
            TextMeshProUGUI progressLabel = AddText(progressRow, "Label", "1 sa 5", 26f, SoftInk, FontStyles.Normal, new Vector2(120f, 40f),
                                                    new Vector2(0.5f, 0.5f), Vector2.zero, 0f, TextAlignmentOptions.MidlineLeft);
            FixedSize(progressLabel.gameObject, 120f, 40f);

            // "Ano ang tawag dito?" with its speaker.
            RectTransform bar = Card(panel, "QuestionBar", new Vector2(340f, 192f), new Vector2(1240f, 130f), 36f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI questionText = TextAt(bar, "Text", "Ano ang tawag dito?", 46f, Ink, FontStyles.Bold, Vector2.zero, new Vector2(1240f, 130f), TextAlignmentOptions.Center);
            Image speakCircle = RoundedAt(bar, "SpeakButton", new Vector2(1124f, 25f), new Vector2(80f, 80f), new Color(Orange.r, Orange.g, Orange.b, 0.15f), sprites.rounded);
            IconAt(speakCircle.transform, "Icon", Vector2.zero, Vector2.one * 46f, icons.speaker, Orange, true).raycastTarget = false;
            var speakButton = speakCircle.gameObject.AddComponent<Button>();
            speakButton.transition = Selectable.Transition.None;
            speakButton.targetGraphic = speakCircle;

            // The picture of the object.
            RectTransform box = Card(panel, "PictureBox", new Vector2(680f, 350f), new Vector2(560f, 400f), 36f, PictureGray, LineGray, sprites.rounded);
            TextMeshProUGUI placeholder = TextAt(box, "Placeholder", "LARAWAN", 26f, SoftInk, FontStyles.Normal, new Vector2(0f, 170f), new Vector2(560f, 60f), TextAlignmentOptions.Center, 3f);
            Image picture = IconAt(box, "Picture", new Vector2(40f, 30f), new Vector2(480f, 340f), null, Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            picture.gameObject.SetActive(false);

            // Three answer cards.
            var cards = new QuizScreen.ChoiceCard[3];
            for (int i = 0; i < cards.Length; i++) cards[i] = BuildChoiceCard(panel, i, sprites);

            // After the right answer: stars (only after the last word) and "Magpatuloy".
            RectTransform starsRoot = NewRect("Stars", panel);
            Place(starsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(590f, 90f), new Vector2(420f, 160f));
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = AddIcon(starsRoot, "Star_" + (i + 1), Vector2.one * 80f, icons.star, LineGray, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 96f, 30f));
                stars[i].raycastTarget = false;
            }
            TextMeshProUGUI starsLabel = AddText(starsRoot, "Label", "Nakuha mo ang 3 bituin!", 30f, Ink, FontStyles.Bold, new Vector2(420f, 44f),
                                                 new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), 0f, TextAlignmentOptions.Center);
            starsRoot.gameObject.SetActive(false);

            Button nextButton = AddPillButton(panel, "NextButton", new Vector2(340f, 100f), Orange, sprites.rounded, new Vector2(0.5f, 0.5f), new Vector2(590f, -60f));
            AddText(nextButton.transform, "Label", "Magpatuloy", 40f, Color.white, FontStyles.Bold, new Vector2(320f, 70f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);
            nextButton.gameObject.SetActive(false);

            var so = new SerializedObject(quiz);
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("soundButton").objectReferenceValue = soundButton;
            so.FindProperty("soundIcon").objectReferenceValue = soundIcon;
            so.FindProperty("soundOnSprite").objectReferenceValue = sprites.soundOn;
            so.FindProperty("soundOffSprite").objectReferenceValue = sprites.soundOff;
            so.FindProperty("questionText").objectReferenceValue = questionText;
            so.FindProperty("speakButton").objectReferenceValue = speakButton;
            so.FindProperty("pictureImage").objectReferenceValue = picture;
            so.FindProperty("picturePlaceholder").objectReferenceValue = placeholder.gameObject;
            so.FindProperty("checkSprite").objectReferenceValue = icons.check;
            so.FindProperty("crossSprite").objectReferenceValue = icons.cross;
            so.FindProperty("nextButton").objectReferenceValue = nextButton;
            so.FindProperty("starsRoot").objectReferenceValue = starsRoot.gameObject;
            so.FindProperty("starsLabel").objectReferenceValue = starsLabel;

            SerializedProperty dotsProperty = so.FindProperty("dots");
            dotsProperty.arraySize = dots.Length;
            for (int i = 0; i < dots.Length; i++) dotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            SerializedProperty starsProperty = so.FindProperty("stars");
            starsProperty.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++) starsProperty.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];

            SerializedProperty cardsProperty = so.FindProperty("cards");
            cardsProperty.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                SerializedProperty c = cardsProperty.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("button").objectReferenceValue = cards[i].button;
                c.FindPropertyRelative("group").objectReferenceValue = cards[i].group;
                c.FindPropertyRelative("border").objectReferenceValue = cards[i].border;
                c.FindPropertyRelative("fill").objectReferenceValue = cards[i].fill;
                c.FindPropertyRelative("picture").objectReferenceValue = cards[i].picture;
                c.FindPropertyRelative("word").objectReferenceValue = cards[i].word;
                c.FindPropertyRelative("badge").objectReferenceValue = cards[i].badge;
                c.FindPropertyRelative("badgeCircle").objectReferenceValue = cards[i].badgeCircle;
                c.FindPropertyRelative("badgeIcon").objectReferenceValue = cards[i].badgeIcon;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return quiz;
        }

        /// <summary>One answer card: a small picture on the left, the word, and a check / cross badge in the corner.</summary>
        static QuizScreen.ChoiceCard BuildChoiceCard(Transform parent, int index, Sprites sprites)
        {
            RectTransform card = Card(parent, "Choice_" + (index + 1), new Vector2(320f + index * 440f, 800f), new Vector2(400f, 230f), 36f, Color.white, LineGray, sprites.rounded);
            var group = card.gameObject.AddComponent<CanvasGroup>();

            Card(card, "PictureSlot", new Vector2(26f, 50f), new Vector2(130f, 130f), 26f, PictureGray, LineGray, sprites.rounded);
            Image picture = IconAt(card, "Picture", new Vector2(36f, 60f), new Vector2(110f, 110f), null, Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            picture.gameObject.SetActive(false);

            TextMeshProUGUI word = TextAt(card, "Word", "", 34f, Ink, FontStyles.Bold, new Vector2(170f, 87f), new Vector2(210f, 56f), TextAlignmentOptions.Center);
            word.enableAutoSizing = true;
            word.fontSizeMin = 20f;
            word.fontSizeMax = 34f;

            RectTransform badge = NewRect("Badge", card);
            Place(badge, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -40f), Vector2.one * 56f);
            var badgeCircle = badge.gameObject.AddComponent<Image>();
            badgeCircle.sprite = sprites.rounded;
            badgeCircle.type = Image.Type.Sliced;
            badgeCircle.color = RightGreen;
            badgeCircle.pixelsPerUnitMultiplier = 128f / 56f;
            badgeCircle.raycastTarget = false;
            Image badgeIcon = AddIcon(badge, "Icon", Vector2.one * 34f, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
            badgeIcon.raycastTarget = false;
            badge.gameObject.SetActive(false);

            var button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = card.Find("Fill").GetComponent<Image>();

            return new QuizScreen.ChoiceCard
            {
                button = button, group = group,
                border = card.Find("Border").GetComponent<Image>(), fill = card.Find("Fill").GetComponent<Image>(),
                picture = picture, word = word,
                badge = badge.gameObject, badgeCircle = badgeCircle, badgeIcon = badgeIcon
            };
        }

        // ------------------------------------------------------------------ spelling (wireframe 05)

        static SpellingScreen BuildSpelling(RectTransform panel, Sprites sprites, GameIcons icons)
        {
            var spelling = panel.gameObject.AddComponent<SpellingScreen>();
            var background = NewStretched("Background", panel).gameObject.AddComponent<Image>();
            background.color = PaperColor;

            // The dashed outlines are drawn once at their exact size.
            Sprite dashedGray = MakeDashed("DashedBoxGray.png", 104, 110, 22f, new Color(0.74f, 0.74f, 0.72f, 1f), 12f, 9f);
            Sprite dashedOrange = MakeDashed("DashedBoxOrange.png", 104, 110, 22f, Orange, 12f, 9f);
            Sprite dashedHint = MakeDashed("DashedHint.png", 370, 105, 30f, new Color(0.74f, 0.74f, 0.72f, 1f), 18f, 12f);

            // ---------------------------------------------------------------- top
            Image backIcon;
            Button backButton = AddRoundButton(panel, "BackButton", new Vector2(0f, 1f), new Vector2(40f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);
            TextAt(panel, "Title", "Ispel ang Salita", 56f, Ink, FontStyles.Bold, new Vector2(162f, 34f), new Vector2(800f, 72f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI subtitle = TextAt(panel, "Subtitle", "Paaralan", 26f, SoftInk, FontStyles.Normal, new Vector2(162f, 104f), new Vector2(800f, 36f), TextAlignmentOptions.MidlineLeft);
            RectTransform progressPill = Card(panel, "ProgressPill", new Vector2(1580f, 48f), new Vector2(260f, 76f), 38f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI progress = TextAt(progressPill, "Label", "Salita 1 sa 5", 27f, Ink, FontStyles.Bold, new Vector2(0f, 14f), new Vector2(260f, 48f), TextAlignmentOptions.Center);

            // ---------------------------------------------------------------- picture and Pakinggan
            RectTransform box = Card(panel, "PictureBox", new Vector2(122f, 221f), new Vector2(375f, 384f), 36f, PictureGray, LineGray, sprites.rounded);
            TextMeshProUGUI placeholder = TextAt(box, "Placeholder", "LARAWAN", 26f, SoftInk, FontStyles.Normal, new Vector2(0f, 162f), new Vector2(375f, 60f), TextAlignmentOptions.Center, 3f);
            Image picture = IconAt(box, "Picture", new Vector2(30f, 30f), new Vector2(315f, 324f), null, Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            picture.gameObject.SetActive(false);
            Button listen = MenuButton(panel, "ListenButton", new Vector2(184f, 632f), new Vector2(250f, 70f), "Pakinggan", icons.speaker, Orange, false, Ink, Color.white, LineGray, sprites, 26f);

            // ---------------------------------------------------------------- question, letter boxes and tiles
            TextAt(panel, "Question", "Anong salita ang nasa larawan?", 46f, Ink, FontStyles.Bold, new Vector2(567f, 258f), new Vector2(1257f, 64f), TextAlignmentOptions.MidlineLeft);
            TextAt(panel, "Instruction", "I-tap ang mga titik sa tamang pagkakasunod-sunod", 26f, SoftInk, FontStyles.Normal, new Vector2(567f, 326f), new Vector2(1257f, 36f), TextAlignmentOptions.MidlineLeft);

            RectTransform boxesRow = NewRect("Boxes", panel);
            Place(boxesRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(567f, -380f), new Vector2(1313f, 116f));
            SetupRow(boxesRow, 22f, TextAnchor.MiddleLeft);
            var boxes = new SpellingScreen.LetterBox[14];   // room for words of up to 14 letters (the rows shrink to fit)
            for (int i = 0; i < boxes.Length; i++) boxes[i] = BuildLetterBox(boxesRow, "Box_" + (i + 1), sprites, dashedGray, dashedOrange);
            TextMeshProUGUI dash = AddText(boxesRow, "Dash", "-", 70f, Ink, FontStyles.Bold, new Vector2(40f, 110f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f, TextAlignmentOptions.Center);
            FixedSize(dash.gameObject, 40f, 110f);
            dash.gameObject.SetActive(false);

            TextAt(panel, "TilesLabel", "MGA TITIK", 22f, SoftInk, FontStyles.Normal, new Vector2(567f, 540f), new Vector2(400f, 30f), TextAlignmentOptions.MidlineLeft, 3f);
            RectTransform tilesRow = NewRect("Tiles", panel);
            Place(tilesRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(567f, -580f), new Vector2(1313f, 116f));
            SetupRow(tilesRow, 22f, TextAnchor.MiddleLeft);
            var tiles = new SpellingScreen.LetterTile[14];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = BuildLetterTile(tilesRow, "Tile_" + (i + 1), sprites);

            TextMeshProUGUI status = TextAt(panel, "Status", string.Empty, 34f, Orange, FontStyles.Bold, new Vector2(567f, 722f), new Vector2(1257f, 50f), TextAlignmentOptions.MidlineLeft);

            // ---------------------------------------------------------------- bottom row: speaker, Burahin, Suriin and the hint
            Image speakerIcon;
            Button speakButton = AddRoundButton(panel, "SpeakButton", new Vector2(0f, 1f), new Vector2(576f, -864f), sprites.rounded, sprites.soundOn, out speakerIcon);
            ((RectTransform)speakButton.transform).pivot = new Vector2(0f, 1f);
            Button erase = MenuButton(panel, "EraseButton", new Vector2(750f, 859f), new Vector2(305f, 105f), "Burahin", null, Color.white, false, Ink, Color.white, LineGray, sprites);
            Button submit = MenuButton(panel, "SubmitButton", new Vector2(1097f, 859f), new Vector2(310f, 105f), "Suriin", null, Color.white, false, Color.white, Orange, Orange, sprites);
            var submitFill = (Image)submit.targetGraphic;
            TextMeshProUGUI submitLabel = submit.transform.Find("Row/Label").GetComponent<TextMeshProUGUI>();

            RectTransform hintCard = NewRect("HintCard", panel);
            Place(hintCard, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1454f, -859f), new Vector2(370f, 105f));
            StretchedRounded(hintCard, "Fill", new Color(0.98f, 0.98f, 0.96f, 1f), sprites.rounded, 30f, 0f).raycastTarget = false;
            Image hintDashed = NewStretched("Dashed", hintCard).gameObject.AddComponent<Image>();
            hintDashed.sprite = dashedHint;
            hintDashed.raycastTarget = false;
            TextAt(hintCard, "Label", "Pahiwatig", 22f, SoftInk, FontStyles.Normal, new Vector2(26f, 14f), new Vector2(318f, 30f), TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI hintText = TextAt(hintCard, "Text", "Kagamitang inuupuan", 30f, Ink, FontStyles.Bold, new Vector2(26f, 48f), new Vector2(318f, 44f), TextAlignmentOptions.MidlineLeft);
            hintText.enableAutoSizing = true;
            hintText.fontSizeMin = 20f;
            hintText.fontSizeMax = 30f;

            // ---------------------------------------------------------------- connect
            var so = new SerializedObject(spelling);
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("progress").objectReferenceValue = progress;
            so.FindProperty("pictureImage").objectReferenceValue = picture;
            so.FindProperty("picturePlaceholder").objectReferenceValue = placeholder.gameObject;
            so.FindProperty("listenButton").objectReferenceValue = listen;
            so.FindProperty("hintText").objectReferenceValue = hintText;
            so.FindProperty("boxesRow").objectReferenceValue = boxesRow;
            so.FindProperty("dash").objectReferenceValue = dash.rectTransform;
            so.FindProperty("status").objectReferenceValue = status;
            so.FindProperty("speakButton").objectReferenceValue = speakButton;
            so.FindProperty("eraseButton").objectReferenceValue = erase;
            so.FindProperty("submitButton").objectReferenceValue = submit;
            so.FindProperty("submitFill").objectReferenceValue = submitFill;
            so.FindProperty("submitLabel").objectReferenceValue = submitLabel;

            SerializedProperty boxesProperty = so.FindProperty("boxes");
            boxesProperty.arraySize = boxes.Length;
            for (int i = 0; i < boxes.Length; i++)
            {
                SerializedProperty b = boxesProperty.GetArrayElementAtIndex(i);
                b.FindPropertyRelative("button").objectReferenceValue = boxes[i].button;
                b.FindPropertyRelative("border").objectReferenceValue = boxes[i].border;
                b.FindPropertyRelative("fill").objectReferenceValue = boxes[i].fill;
                b.FindPropertyRelative("dashedGray").objectReferenceValue = boxes[i].dashedGray;
                b.FindPropertyRelative("dashedOrange").objectReferenceValue = boxes[i].dashedOrange;
                b.FindPropertyRelative("letter").objectReferenceValue = boxes[i].letter;
            }
            SerializedProperty tilesProperty = so.FindProperty("tiles");
            tilesProperty.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                SerializedProperty t = tilesProperty.GetArrayElementAtIndex(i);
                t.FindPropertyRelative("button").objectReferenceValue = tiles[i].button;
                t.FindPropertyRelative("letter").objectReferenceValue = tiles[i].letter;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return spelling;
        }

        /// <summary>One letter box: dashed while empty (orange for the next one), solid with its letter once filled.</summary>
        static SpellingScreen.LetterBox BuildLetterBox(Transform parent, string name, Sprites sprites, Sprite dashedGray, Sprite dashedOrange)
        {
            RectTransform root = NewRect(name, parent);
            FixedSize(root.gameObject, 104f, 110f);
            Image border = StretchedRounded(root, "Border", LineGray, sprites.rounded, 22f, 3f);
            Image fill = StretchedRounded(root, "Fill", new Color(0.97f, 0.97f, 0.95f, 1f), sprites.rounded, 22f, 0f);
            Image gray = NewStretched("DashedGray", root).gameObject.AddComponent<Image>();
            gray.sprite = dashedGray;
            gray.raycastTarget = false;
            Image orange = NewStretched("DashedOrange", root).gameObject.AddComponent<Image>();
            orange.sprite = dashedOrange;
            orange.raycastTarget = false;
            TextMeshProUGUI letter = TextAt(root, "Letter", string.Empty, 76f, Ink, FontStyles.Bold, Vector2.zero, new Vector2(104f, 110f), TextAlignmentOptions.Center);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return new SpellingScreen.LetterBox { button = button, border = border, fill = fill, dashedGray = gray, dashedOrange = orange, letter = letter };
        }

        /// <summary>One letter tile of the bank: a white square with a big letter.</summary>
        static SpellingScreen.LetterTile BuildLetterTile(Transform parent, string name, Sprites sprites)
        {
            RectTransform root = NewRect(name, parent);
            FixedSize(root.gameObject, 104f, 110f);
            StretchedRounded(root, "Border", LineGray, sprites.rounded, 22f, 3f);
            Image fill = StretchedRounded(root, "Fill", Color.white, sprites.rounded, 22f, 0f);
            TextMeshProUGUI letter = TextAt(root, "Letter", "A", 76f, Ink, FontStyles.Bold, Vector2.zero, new Vector2(104f, 110f), TextAlignmentOptions.Center);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return new SpellingScreen.LetterTile { button = button, letter = letter };
        }

        /// <summary>A rounded outline made of dashes, exactly the size of the object it goes on (so it is never stretched).</summary>
        static Sprite MakeDashed(string file, int width, int height, float radius, Color color, float dash, float gap)
        {
            float w = width, h = height;
            Shape outer = RoundRect(0f, 0f, w, h, radius);
            Shape inner = RoundRect(3f, 3f, w - 3f, h - 3f, radius - 3f);
            Shape dashes = (x, y) =>
            {
                if (!outer(x, y) || inner(x, y)) return false;
                float toLeft = x, toRight = w - x, toBottom = y, toTop = h - y;
                float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
                float along;   // how far along the outline we are, so the dashes run around the shape
                if (nearest == toTop) along = x;
                else if (nearest == toRight) along = w + (h - y);
                else if (nearest == toBottom) along = w + h + (w - x);
                else along = 2f * w + h + y;
                return Mathf.Repeat(along, dash + gap) < dash;
            };
            var painter = new Painter(width, height);
            painter.Fill(dashes, color);
            return Save(IconFolder, file, painter);
        }

        // ------------------------------------------------------------------ pause (wireframe 06)

        static PauseScreen BuildPause(RectTransform panel, Sprites sprites, GameIcons icons)
        {
            var pause = panel.gameObject.AddComponent<PauseScreen>();

            // The dimmed game and the white card.
            RectTransform menu = NewStretched("Menu", panel);
            var dim = NewStretched("Dim", menu).gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform card = Card(menu, "Card", new Vector2(567f, 193f), new Vector2(786f, 694f), 50f, Color.white, LineGray, sprites.rounded);
            TextAt(card, "Title", "Naka-pause", 72f, Ink, FontStyles.Bold, new Vector2(0f, 34f), new Vector2(786f, 100f), TextAlignmentOptions.Center);
            TextMeshProUGUI subtitle = TextAt(card, "Subtitle", "Pahinga muna tayo!", 30f, SoftInk, FontStyles.Normal, new Vector2(0f, 130f), new Vector2(786f, 44f), TextAlignmentOptions.Center);

            RectTransform sound = Card(card, "SoundButton", new Vector2(678f, 28f), new Vector2(76f, 76f), 38f, Color.white, LineGray, sprites.rounded);
            Image soundIcon = IconAt(sound, "Icon", Vector2.zero, Vector2.one * 42f, sprites.soundOn, Color.white, true);
            soundIcon.raycastTarget = false;
            var soundButton = sound.gameObject.AddComponent<Button>();
            soundButton.transition = Selectable.Transition.None;
            soundButton.targetGraphic = sound.Find("Fill").GetComponent<Image>();

            Vector2 buttonSize = new Vector2(640f, 97f);
            Button resume = MenuButton(card, "ResumeButton", new Vector2(73f, 200f), buttonSize, "Ituloy", sprites.play, Color.white, false, Color.white, Orange, Orange, sprites);
            Button restart = MenuButton(card, "RestartButton", new Vector2(73f, 316f), buttonSize, "Uliting ang antas", icons.restart, Ink, false, Ink, Color.white, LineGray, sprites);
            Button settingsButton = MenuButton(card, "SettingsButton", new Vector2(73f, 432f), buttonSize, "Mga Setting", sprites.settings, Color.white, false, Ink, Color.white, LineGray, sprites);
            Button map = MenuButton(card, "MapButton", new Vector2(73f, 548f), buttonSize, "Bumalik sa Mapa", icons.cross, SlashRed, false, SlashRed, Color.white, LineGray, sprites);

            // The settings screen of the title screen again, opened by "Mga Setting".
            RectTransform settingsScreen = NewStretched("SettingsScreen", panel);
            var settingsBackground = settingsScreen.gameObject.AddComponent<Image>();
            settingsBackground.color = PaperColor;
            RectTransform settingsPanel;
            SettingsController settings = HirayaSettingsSetup.BuildPanel(settingsScreen, sprites, out settingsPanel);
            settingsScreen.gameObject.SetActive(false);

            var so = new SerializedObject(pause);
            so.FindProperty("menu").objectReferenceValue = menu.gameObject;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("restartButton").objectReferenceValue = restart;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("mapButton").objectReferenceValue = map;
            so.FindProperty("soundButton").objectReferenceValue = soundButton;
            so.FindProperty("soundIcon").objectReferenceValue = soundIcon;
            so.FindProperty("soundOnSprite").objectReferenceValue = sprites.soundOn;
            so.FindProperty("soundOffSprite").objectReferenceValue = sprites.soundOff;
            so.FindProperty("settingsScreen").objectReferenceValue = settingsScreen.gameObject;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pause;
        }

        /// <summary>A pill button: a picture and a text in one centred row. The picture comes after the text when iconAfter is true.</summary>
        internal static Button MenuButton(Transform parent, string name, Vector2 topLeft, Vector2 size, string label, Sprite icon, Color iconColor, bool iconAfter,
                                 Color textColor, Color fill, Color border, Sprites sprites, float fontSize = 38f)
        {
            RectTransform root = Card(parent, name, topLeft, size, size.y * 0.5f, fill, border, sprites.rounded);
            RectTransform row = NewStretched("Row", root);
            SetupRow(row, 18f);

            Image iconImage = null;
            if (icon != null)
            {
                iconImage = AddIcon(row, "Icon", Vector2.one * 44f, icon, iconColor, new Vector2(0.5f, 0.5f), Vector2.zero);
                iconImage.raycastTarget = false;
                FixedSize(iconImage.gameObject, 44f, 44f);
            }
            TextMeshProUGUI text = AddText(row, "Label", label, fontSize, textColor, FontStyles.Bold, new Vector2(300f, 56f), new Vector2(0.5f, 0.5f), Vector2.zero, 0f);
            if (iconImage != null && iconAfter) iconImage.transform.SetAsLastSibling();

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = root.Find("Fill").GetComponent<Image>();
            return button;
        }

        // ------------------------------------------------------------------ results (wireframe 07)

        static ResultsScreen BuildResults(RectTransform panel, Sprites sprites, GameIcons icons, Sprite chevron)
        {
            var results = panel.gameObject.AddComponent<ResultsScreen>();

            // A pale sheet with a few little decorations.
            var background = NewStretched("Background", panel).gameObject.AddComponent<Image>();
            background.color = new Color(0.945f, 0.945f, 0.925f, 1f);
            Decoration(panel, "Dot_1", new Vector2(205f, 245f), 20f, new Color(0.97f, 0.72f, 0.72f, 1f), sprites);
            Decoration(panel, "Dot_2", new Vector2(1745f, 330f), 14f, new Color(0.78f, 0.78f, 0.76f, 1f), sprites);
            Decoration(panel, "Dot_3", new Vector2(120f, 760f), 14f, new Color(0.95f, 0.78f, 0.60f, 1f), sprites);
            Decoration(panel, "Dot_4", new Vector2(1815f, 880f), 24f, new Color(0.97f, 0.72f, 0.72f, 1f), sprites);
            Decoration(panel, "Dot_5", new Vector2(1690f, 120f), 12f, new Color(0.80f, 0.80f, 0.78f, 1f), sprites);
            Decoration(panel, "Dot_6", new Vector2(250f, 975f), 16f, new Color(0.78f, 0.78f, 0.76f, 1f), sprites);

            RectTransform card = Card(panel, "Card", new Vector2(342f, 90f), new Vector2(1236f, 900f), 48f, Color.white, LineGray, sprites.rounded);
            TextMeshProUGUI caption = TextAt(card, "Caption", "BAITANG 1 PATAAS · PAARALAN", 24f, SoftInk, FontStyles.Normal, new Vector2(0f, 46f), new Vector2(1236f, 34f), TextAlignmentOptions.Center, 3f);
            TextMeshProUGUI heading = TextAt(card, "Heading", "Magaling, Fin!", 80f, Ink, FontStyles.Bold, new Vector2(0f, 84f), new Vector2(1236f, 100f), TextAlignmentOptions.Center);
            TextMeshProUGUI subtitle = TextAt(card, "Subtitle", "Natapos mo ang aralin sa Paaralan", 32f, SoftInk, FontStyles.Normal, new Vector2(0f, 188f), new Vector2(1236f, 44f), TextAlignmentOptions.Center);

            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = IconAt(card, "Star_" + (i + 1), new Vector2(408f + i * 150f, 250f), Vector2.one * 120f, icons.star, LineGray);
                stars[i].raycastTarget = false;
            }
            TextMeshProUGUI starsLabel = TextAt(card, "StarsLabel", "0 sa 3 bituin", 30f, SoftInk, FontStyles.Normal, new Vector2(0f, 382f), new Vector2(1236f, 40f), TextAlignmentOptions.Center);

            TextMeshProUGUI correctValue = StatBox(card, "Stat_Correct", "TAMANG SAGOT", new Vector2(78f, 450f), sprites);
            TextMeshProUGUI newWordsValue = StatBox(card, "Stat_NewWords", "BAGONG SALITA", new Vector2(448f, 450f), sprites);
            TextMeshProUGUI attemptsValue = StatBox(card, "Stat_Attempts", "PAGSUBOK", new Vector2(818f, 450f), sprites);

            TextAt(card, "WordsLabel", "MGA SALITANG NATUTUNAN", 22f, SoftInk, FontStyles.Normal, new Vector2(78f, 604f), new Vector2(700f, 30f), TextAlignmentOptions.MidlineLeft, 3f);
            RectTransform chipRow = NewRect("Chips", card);
            Place(chipRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -644f), new Vector2(1080f, 70f));
            SetupRow(chipRow, 16f, TextAnchor.MiddleLeft);
            var chips = new ResultsScreen.WordChip[3];
            for (int i = 0; i < chips.Length; i++)
            {
                Button chipButton;
                TextMeshProUGUI chipLabel;
                RectTransform chip = BuildChip(chipRow, "Chip_" + (i + 1), "mansanas", icons.speaker, false, sprites, out chipButton, out chipLabel);
                chips[i] = new ResultsScreen.WordChip { root = chip.gameObject, button = chipButton, label = chipLabel };
            }
            Button moreButton;
            TextMeshProUGUI moreLabel;
            RectTransform more = BuildChip(chipRow, "MoreChip", "+ 2 pang salita sa Salita Book", null, true, sprites, out moreButton, out moreLabel);
            moreButton.interactable = false;

            Button replay = MenuButton(card, "ReplayButton", new Vector2(78f, 752f), new Vector2(384f, 100f), "Uliting", icons.restart, Ink, false, Ink, Color.white, LineGray, sprites);
            Button next = MenuButton(card, "NextButton", new Vector2(774f, 752f), new Vector2(384f, 100f), "Susunod", chevron, Color.white, true, Color.white, Orange, Orange, sprites);

            var so = new SerializedObject(results);
            so.FindProperty("caption").objectReferenceValue = caption;
            so.FindProperty("heading").objectReferenceValue = heading;
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("starsLabel").objectReferenceValue = starsLabel;
            so.FindProperty("correctValue").objectReferenceValue = correctValue;
            so.FindProperty("newWordsValue").objectReferenceValue = newWordsValue;
            so.FindProperty("attemptsValue").objectReferenceValue = attemptsValue;
            so.FindProperty("chipRow").objectReferenceValue = chipRow;
            so.FindProperty("moreChip").objectReferenceValue = more.gameObject;
            so.FindProperty("moreLabel").objectReferenceValue = moreLabel;
            so.FindProperty("replayButton").objectReferenceValue = replay;
            so.FindProperty("nextButton").objectReferenceValue = next;
            SerializedProperty starsProperty = so.FindProperty("stars");
            starsProperty.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++) starsProperty.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            SerializedProperty chipsProperty = so.FindProperty("chips");
            chipsProperty.arraySize = chips.Length;
            for (int i = 0; i < chips.Length; i++)
            {
                SerializedProperty c = chipsProperty.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("root").objectReferenceValue = chips[i].root;
                c.FindPropertyRelative("button").objectReferenceValue = chips[i].button;
                c.FindPropertyRelative("label").objectReferenceValue = chips[i].label;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return results;
        }

        static void Decoration(Transform parent, string name, Vector2 center, float size, Color color, Sprites sprites)
        {
            Image dot = AddRounded(parent, name, Vector2.one * size, color, sprites.rounded, new Vector2(0f, 1f), new Vector2(center.x, -center.y));
            dot.raycastTarget = false;
        }

        /// <summary>A grey box with a small caption and a big number. Returns the number text.</summary>
        static TextMeshProUGUI StatBox(Transform parent, string name, string caption, Vector2 topLeft, Sprites sprites)
        {
            Color fill = new Color(0.95f, 0.95f, 0.93f, 1f);
            RectTransform box = Card(parent, name, topLeft, new Vector2(340f, 130f), 28f, fill, fill, sprites.rounded);
            TextAt(box, "Caption", caption, 22f, SoftInk, FontStyles.Normal, new Vector2(0f, 20f), new Vector2(340f, 34f), TextAlignmentOptions.Center, 2f);
            return TextAt(box, "Value", "0", 50f, Ink, FontStyles.Bold, new Vector2(0f, 56f), new Vector2(340f, 66f), TextAlignmentOptions.Center);
        }

        /// <summary>
        /// A word chip that is as wide as its text: a small speaker and the word (or only grey text for the "more" chip).
        /// The border and fill ignore the row layout and just stretch behind the text.
        /// </summary>
        static RectTransform BuildChip(Transform parent, string name, string text, Sprite icon, bool plain, Sprites sprites, out Button button, out TextMeshProUGUI label)
        {
            RectTransform root = NewRect(name, parent);
            Image border = StretchedRounded(root, "Border", LineGray, sprites.rounded, 31f, 3f);
            Image fill = StretchedRounded(root, "Fill", plain ? new Color(0.98f, 0.98f, 0.96f, 1f) : Color.white, sprites.rounded, 31f, 0f);
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(plain ? 28 : 20, 28, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (icon != null)
            {
                Image speaker = AddIcon(root, "Icon", Vector2.one * 32f, icon, Orange, new Vector2(0.5f, 0.5f), Vector2.zero);
                speaker.raycastTarget = false;
                FixedSize(speaker.gameObject, 32f, 32f);
            }
            label = AddText(root, "Label", text, plain ? 26f : 30f, plain ? SoftInk : Ink, plain ? FontStyles.Normal : FontStyles.Bold, new Vector2(200f, 42f),
                            new Vector2(0.5f, 0.5f), Vector2.zero, 0f, TextAlignmentOptions.MidlineLeft);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 42f;

            button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return root;
        }

        // ------------------------------------------------------------------ the small pictures

        /// <summary>A row that keeps its children in the middle (or to the left), so a different number of dots or words still looks balanced.</summary>
        internal static void SetupRow(RectTransform row, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = alignment;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        internal static void FixedSize(GameObject go, float width, float height)
        {
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.minHeight = element.preferredHeight = height;
        }

        static Sprite Save(string folder, string file, Painter painter) => SaveSprite(folder + "/" + file, painter, Vector4.zero);

        static GameIcons MakeIcons()
        {
            var icons = new GameIcons();

            // Icons are drawn white and tinted on the objects, so one picture works in any colour.
            var speaker = new Painter(128, 128);
            speaker.Fill(Union(Rect(20f, 50f, 44f, 78f), Polygon(new Vector2(44f, 50f), new Vector2(76f, 24f), new Vector2(76f, 104f), new Vector2(44f, 78f)),
                               Arc(76f, 64f, 20f, 28f, 0f, 48f), Arc(76f, 64f, 36f, 44f, 0f, 48f)), Color.white);
            icons.speaker = Save(IconFolder, "SpeakerIcon.png", speaker);

            var pause = new Painter(128, 128);
            pause.Fill(Union(RoundRect(34f, 24f, 58f, 104f, 6f), RoundRect(70f, 24f, 94f, 104f, 6f)), Color.white);
            icons.pause = Save(IconFolder, "PauseIcon.png", pause);

            var check = new Painter(128, 128);
            check.Fill(Union(Segment(new Vector2(28f, 66f), new Vector2(54f, 40f), 16f), Segment(new Vector2(54f, 40f), new Vector2(102f, 94f), 16f)), Color.white);
            icons.check = Save(IconFolder, "CheckIcon.png", check);

            var cross = new Painter(128, 128);
            cross.Fill(Union(Segment(new Vector2(34f, 34f), new Vector2(94f, 94f), 16f), Segment(new Vector2(34f, 94f), new Vector2(94f, 34f), 16f)), Color.white);
            icons.cross = Save(IconFolder, "CrossIcon.png", cross);

            var star = new Painter(128, 128);
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? 58f : 24f;
                points[i] = new Vector2(64f + Mathf.Cos(angle) * radius, 62f + Mathf.Sin(angle) * radius);
            }
            star.Fill(Polygon(points), Color.white);
            icons.star = Save(IconFolder, "StarWhiteIcon.png", star);

            var arrow = new Painter(128, 128);
            arrow.Fill(Polygon(new Vector2(64f, 112f), new Vector2(104f, 62f), new Vector2(78f, 62f), new Vector2(78f, 18f),
                               new Vector2(50f, 18f), new Vector2(50f, 62f), new Vector2(24f, 62f)), Color.white);
            icons.arrow = Save(IconFolder, "ArrowIcon.png", arrow);

            // "Uliting" (a circle with an arrow head).
            var restart = new Painter(128, 128);
            restart.Fill(Union(Arc(64f, 60f, 28f, 40f, 205f, 145f), Polygon(new Vector2(82f, 60f), new Vector2(116f, 60f), new Vector2(99f, 90f))), Color.white);
            icons.restart = Save(IconFolder, "RestartIcon.png", restart);

            // The dashed ring around the object to find (orange).
            var ring = new Painter(256, 256);
            var dashes = new Shape[24];
            for (int i = 0; i < dashes.Length; i++) dashes[i] = Arc(128f, 128f, 108f, 123f, i * 15f, 5.5f);
            ring.Fill(Union(dashes), Orange);
            icons.ring = Save(IconFolder, "TargetRing.png", ring);

            // Pictures of the three words (simple flat drawings; replace them with your own art on the objects).
            Color wood = new Color(0.76f, 0.52f, 0.28f, 1f);
            Color woodDark = new Color(0.56f, 0.36f, 0.18f, 1f);

            var chair = new Painter(256, 256);
            chair.Fill(Union(RoundRect(66f, 26f, 92f, 112f, 5f), RoundRect(164f, 26f, 190f, 112f, 5f)), woodDark);
            chair.Fill(RoundRect(46f, 102f, 210f, 138f, 10f), wood);
            chair.Fill(Union(RoundRect(64f, 136f, 90f, 232f, 8f), RoundRect(166f, 136f, 192f, 232f, 8f)), wood);
            chair.Fill(RoundRect(64f, 196f, 192f, 236f, 10f), wood);
            chair.Fill(RoundRect(90f, 156f, 166f, 178f, 6f), woodDark);
            icons.upuan = Save(WordFolder, "Silya.png", chair);

            var table = new Painter(256, 256);
            table.Fill(Union(RoundRect(50f, 30f, 78f, 150f, 5f), RoundRect(178f, 30f, 206f, 150f, 5f)), woodDark);
            table.Fill(RoundRect(78f, 118f, 178f, 144f, 3f), woodDark);
            table.Fill(RoundRect(22f, 142f, 234f, 184f, 12f), wood);
            icons.mesa = Save(WordFolder, "Mesa.png", table);

            var bed = new Painter(256, 256);
            bed.Fill(Union(RoundRect(34f, 38f, 56f, 76f, 4f), RoundRect(200f, 38f, 222f, 76f, 4f)), woodDark);
            bed.Fill(RoundRect(24f, 70f, 60f, 206f, 12f), woodDark);
            bed.Fill(RoundRect(196f, 70f, 232f, 134f, 10f), woodDark);
            bed.Fill(RoundRect(24f, 70f, 232f, 106f, 8f), wood);
            bed.Fill(RoundRect(52f, 104f, 200f, 146f, 12f), new Color(0.97f, 0.94f, 0.86f, 1f));
            bed.Fill(RoundRect(104f, 104f, 200f, 152f, 12f), new Color(0.36f, 0.62f, 0.74f, 1f));
            bed.Fill(RoundRect(58f, 142f, 112f, 176f, 14f), new Color(0.99f, 0.99f, 0.99f, 1f));
            icons.kama = Save(WordFolder, "Kama.png", bed);

            return icons;
        }
    }
}
