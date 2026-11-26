using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). Builds the title screen from wireframe 01 (and the age step that follows
    /// it) as a normal UI prefab (Assets/Prefabs/UI/TitleScreen.prefab) and puts one copy of it in the Main Menu scene.
    /// Every part is an ordinary UI object you can select and move; the little icons are drawn into Assets/UI/Title so you
    /// can swap in your own art.
    ///   TitleScreen > TitlePanel (HIRAYA, Boy / Girl, MAGLARO, Parent / Teacher, sound, settings)
    ///               > AgePanel   (tap your age, Susunod, back)
    /// </summary>
    public static class HirayaTitleSetup
    {
        public const string IconFolder = "Assets/UI/Title";
        public const string PrefabFolder = "Assets/Prefabs/UI";
        public const string TitlePrefabPath = PrefabFolder + "/TitleScreen.prefab";

        // Colours of the wireframe.
        internal static readonly Color PaperColor = new Color(0.918f, 0.918f, 0.898f, 1f);
        internal static readonly Color Orange = new Color(0.91f, 0.34f, 0.18f, 1f);
        internal static readonly Color Ink = new Color(0.10f, 0.10f, 0.10f, 1f);
        internal static readonly Color SoftInk = new Color(0.43f, 0.43f, 0.43f, 1f);
        internal static readonly Color LineGray = new Color(0.85f, 0.85f, 0.83f, 1f);
        internal static readonly Color IconInk = new Color(0.25f, 0.25f, 0.27f, 1f);
        internal static readonly Color SlashRed = new Color(0.89f, 0.25f, 0.15f, 1f);

        const float AvatarSize = 150f;
        const float AgeSize = 130f;
        static readonly int[] Ages = { 3, 4, 5, 6, 7, 8, 9 };

        internal class Sprites
        {
            public Sprite rounded, play, lockIcon, soundOn, soundOff, settings, back;
        }

        // ------------------------------------------------------------------ menu entry points

        public static void BuildTitleScreen()
        {
            BuildPrefab();

            var scene = EditorSceneManager.OpenScene(HirayaSceneSetup.MainMenuScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("TitleScreen");
            while (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                existing = GameObject.Find("TitleScreen");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefabPath);
            PrefabUtility.InstantiatePrefab(prefab);

            // The title screen is the first thing the player sees. The old placeholder menu (Game Title / Play / Settings / Quit)
            // and the old age screen are hidden: Boy / Girl and the age are now chosen on the title screen's own two steps.
            GameObject canvasGo = GameObject.Find("Main Canvas");
            if (canvasGo != null)
            {
                Transform oldMenu = canvasGo.transform.Find("Mainmenu");
                if (oldMenu != null) oldMenu.gameObject.SetActive(false);
                Transform ageSelect = canvasGo.transform.Find("Age Select");
                if (ageSelect != null) ageSelect.gameObject.SetActive(false);

                var controller = canvasGo.GetComponent<MainMenuController>();
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    so.FindProperty("mainMenuPanel").objectReferenceValue = null;
                    so.FindProperty("playButton").objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ the prefab

        public static GameObject BuildPrefab()
        {
            HirayaCharacterSetup.EnsureFolder(IconFolder);
            HirayaCharacterSetup.EnsureFolder(PrefabFolder);
            Sprites sprites = MakeSprites();

            var fin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(HirayaCharacterSetup.CharactersFolder + "/Fin.asset");
            var fiona = AssetDatabase.LoadAssetAtPath<CharacterSkin>(HirayaCharacterSetup.CharactersFolder + "/Fiona.asset");

            var rootGo = new GameObject("TitleScreen", typeof(RectTransform));
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            rootGo.AddComponent<GraphicRaycaster>();
            var controller = rootGo.AddComponent<TitleScreenController>();
            Transform root = rootGo.transform;

            // Background: a plain sheet for now. Put the 2.5D background art here (the wireframe's placeholder).
            RectTransform background = NewStretched("BackgroundArt", root);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = PaperColor;
            backgroundImage.raycastTarget = false;

            RectTransform titlePanel = NewStretched("TitlePanel", root);
            RectTransform agePanel = NewStretched("AgePanel", root);

            // ---------------------------------------------------------------- step 1: title
            AddText(titlePanel, "Title", "HIRAYA", 150f, Ink, FontStyles.Bold, new Vector2(1400f, 200f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -229f), 0f);
            AddText(titlePanel, "Subtitle", "Isang Paglalakbay sa Wikang Filipino", 36f, SoftInk, FontStyles.Normal, new Vector2(1400f, 56f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -337f), 0f);
            Image underline = AddRounded(titlePanel, "SubtitleUnderline", new Vector2(217f, 7f), Orange, sprites.rounded,
                                         new Vector2(0.5f, 1f), new Vector2(0f, -381f));
            underline.raycastTarget = false;

            // MAGLARO (play) button.
            Button playButton = AddPillButton(titlePanel, "PlayButton", new Vector2(502f, 150f), Orange, sprites.rounded,
                                              new Vector2(0.5f, 0.5f), Vector2.zero);
            Image playIcon = AddIcon(playButton.transform, "PlayIcon", new Vector2(64f, 64f), sprites.play, Color.white,
                                     new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f));
            playIcon.raycastTarget = false;
            AddText(playButton.transform, "Label", "MAGLARO", 46f, Color.white, FontStyles.Bold, new Vector2(320f, 80f),
                    new Vector2(0.5f, 0.5f), new Vector2(35f, 0f), 1f);

            // Boy or girl.
            AddText(titlePanel, "ChildPickerLabel", "PUMILI NG BATA", 24f, SoftInk, FontStyles.Normal, new Vector2(700f, 40f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 389f), 6f);

            var avatarSlots = new TitleScreenController.AvatarSlot[2];
            string[] ids = { "Boy", "Girl" };
            Sprite[] portraits = { fin.front, fiona.front };
            for (int i = 0; i < 2; i++)
            {
                bool selected = i == 0;
                RectTransform item = NewRect("Child_" + ids[i], titlePanel);
                Place(item, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((i - 0.5f) * 210f, 275f), new Vector2(AvatarSize, AvatarSize));

                float ringWidth = selected ? 8f : 3f;
                Image ring = AddRounded(item, "Ring", Vector2.one * (AvatarSize + 2f * ringWidth), selected ? Orange : LineGray, sprites.rounded,
                                        new Vector2(0.5f, 0.5f), Vector2.zero);
                Image fill = AddRounded(item, "Fill", Vector2.one * AvatarSize, Color.white, sprites.rounded,
                                        new Vector2(0.5f, 0.5f), Vector2.zero);

                // The portrait is the character's front picture (Fin for the boy, Fiona for the girl).
                Image portraitImage = AddIcon(item, "Portrait", Vector2.one * 215f, portraits[i], Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;

                TextMeshProUGUI nameLabel = AddText(item, "NameLabel", ids[i], 30f, selected ? Ink : SoftInk,
                                                    selected ? FontStyles.Bold : FontStyles.Normal, new Vector2(260f, 46f),
                                                    new Vector2(0.5f, 0.5f), new Vector2(0f, -102f), 0f);

                var button = item.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = fill;

                avatarSlots[i] = new TitleScreenController.AvatarSlot { button = button, ring = ring, nameLabel = nameLabel };
            }

            // Parent / Teacher (bottom left).
            RectTransform parent = NewRect("ParentButton", titlePanel);
            Place(parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 46f), new Vector2(337f, 92f));
            AddRounded(parent, "Ring", new Vector2(343f, 98f), LineGray, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            Image parentFill = AddRounded(parent, "Fill", new Vector2(337f, 92f), Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            AddIcon(parent, "LockIcon", new Vector2(52f, 52f), sprites.lockIcon, Color.white, new Vector2(0.5f, 0.5f), new Vector2(-112f, 0f)).raycastTarget = false;
            AddText(parent, "Label", "Parent / Teacher", 30f, new Color(0.2f, 0.2f, 0.2f, 1f), FontStyles.Normal, new Vector2(240f, 60f),
                    new Vector2(0.5f, 0.5f), new Vector2(32f, 0f), 0f);
            var parentButton = parent.gameObject.AddComponent<Button>();
            parentButton.transition = Selectable.Transition.None;
            parentButton.targetGraphic = parentFill;

            // Sound and settings (bottom right).
            Image soundIcon;
            var soundButton = AddRoundButton(titlePanel, "SoundButton", new Vector2(1f, 0f), new Vector2(-181f, 46f), sprites.rounded, sprites.soundOn, out soundIcon);
            Image settingsIcon;
            var settingsButton = AddRoundButton(titlePanel, "SettingsButton", new Vector2(1f, 0f), new Vector2(-48f, 46f), sprites.rounded, sprites.settings, out settingsIcon);

            // ---------------------------------------------------------------- step 2: age
            Image backIcon;
            var backButton = AddRoundButton(agePanel, "BackButton", new Vector2(0f, 1f), new Vector2(48f, -48f), sprites.rounded, sprites.back, out backIcon);
            ((RectTransform)backButton.transform).pivot = new Vector2(0f, 1f);

            AddText(agePanel, "AgeTitle", "Ilang taon ka na?", 110f, Ink, FontStyles.Bold, new Vector2(1500f, 150f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -250f), 0f);
            AddText(agePanel, "AgeSubtitle", "Piliin ang iyong edad", 38f, SoftInk, FontStyles.Normal, new Vector2(1000f, 56f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -360f), 0f);

            var ageSlots = new TitleScreenController.AgeSlot[Ages.Length];
            for (int i = 0; i < Ages.Length; i++)
            {
                RectTransform item = NewRect("Age_" + Ages[i], agePanel);
                Place(item, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (Ages.Length - 1) * 0.5f) * 170f, -10f), new Vector2(AgeSize, AgeSize));

                Image ring = AddRounded(item, "Ring", Vector2.one * (AgeSize + 6f), LineGray, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
                Image fill = AddRounded(item, "Fill", Vector2.one * AgeSize, Color.white, sprites.rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
                TextMeshProUGUI label = AddText(item, "Number", Ages[i].ToString(), 72f, Ink, FontStyles.Bold, new Vector2(AgeSize, AgeSize),
                                                new Vector2(0.5f, 0.5f), Vector2.zero, 0f);

                var button = item.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = fill;

                ageSlots[i] = new TitleScreenController.AgeSlot { button = button, fill = fill, ring = ring, label = label };
            }

            Button nextButton = AddPillButton(agePanel, "NextButton", new Vector2(460f, 140f), Orange, sprites.rounded,
                                              new Vector2(0.5f, 0f), new Vector2(0f, 210f));
            var nextColors = nextButton.colors;
            nextColors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.40f);
            nextButton.colors = nextColors;
            AddText(nextButton.transform, "Label", "Susunod", 46f, Color.white, FontStyles.Bold, new Vector2(360f, 80f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, 1f);

            agePanel.gameObject.SetActive(false);

            // ---------------------------------------------------------------- step 3: world map
            RectTransform worldPanel;
            WorldMapController worldMap = HirayaWorldMapSetup.BuildPanel(root, sprites, fin.front, fiona.front, out worldPanel);
            worldPanel.gameObject.SetActive(false);

            // ---------------------------------------------------------------- settings
            RectTransform settingsPanel;
            SettingsController settings = HirayaSettingsSetup.BuildPanel(root, sprites, out settingsPanel);
            settingsPanel.gameObject.SetActive(false);

            // ---------------------------------------------------------------- Salita Book
            RectTransform wordBookPanel;
            SalitaBookController wordBook = HirayaSalitaBookSetup.BuildPanel(root, sprites, out wordBookPanel);
            wordBookPanel.gameObject.SetActive(false);

            // ---------------------------------------------------------------- Parent / Teacher: the gate and the dashboard
            RectTransform parentGatePanel;
            ParentGateController parentGate = HirayaParentSetup.BuildGate(root, sprites, out parentGatePanel);
            parentGatePanel.gameObject.SetActive(false);
            RectTransform dashboardPanel;
            ParentDashboardController dashboard = HirayaParentSetup.BuildDashboard(root, sprites, fin.front, fiona.front, out dashboardPanel);
            dashboardPanel.gameObject.SetActive(false);

            // ---------------------------------------------------------------- connect everything to the controller
            var so = new SerializedObject(controller);
            so.FindProperty("titlePanel").objectReferenceValue = titlePanel.gameObject;
            so.FindProperty("agePanel").objectReferenceValue = agePanel.gameObject;
            so.FindProperty("worldMapPanel").objectReferenceValue = worldPanel.gameObject;
            so.FindProperty("worldMap").objectReferenceValue = worldMap;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.gameObject;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("wordBookPanel").objectReferenceValue = wordBookPanel.gameObject;
            so.FindProperty("wordBook").objectReferenceValue = wordBook;
            so.FindProperty("parentGatePanel").objectReferenceValue = parentGatePanel.gameObject;
            so.FindProperty("parentGate").objectReferenceValue = parentGate;
            so.FindProperty("dashboardPanel").objectReferenceValue = dashboardPanel.gameObject;
            so.FindProperty("dashboard").objectReferenceValue = dashboard;

            SerializedProperty slots = so.FindProperty("avatars");
            slots.arraySize = avatarSlots.Length;
            for (int i = 0; i < avatarSlots.Length; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("button").objectReferenceValue = avatarSlots[i].button;
                slot.FindPropertyRelative("ring").objectReferenceValue = avatarSlots[i].ring;
                slot.FindPropertyRelative("nameLabel").objectReferenceValue = avatarSlots[i].nameLabel;
            }

            SerializedProperty ageProperty = so.FindProperty("ageSlots");
            ageProperty.arraySize = ageSlots.Length;
            for (int i = 0; i < ageSlots.Length; i++)
            {
                SerializedProperty slot = ageProperty.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("button").objectReferenceValue = ageSlots[i].button;
                slot.FindPropertyRelative("fill").objectReferenceValue = ageSlots[i].fill;
                slot.FindPropertyRelative("ring").objectReferenceValue = ageSlots[i].ring;
                slot.FindPropertyRelative("label").objectReferenceValue = ageSlots[i].label;
            }
            SerializedProperty values = so.FindProperty("ageValues");
            values.arraySize = Ages.Length;
            for (int i = 0; i < Ages.Length; i++) values.GetArrayElementAtIndex(i).intValue = Ages[i];

            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("parentButton").objectReferenceValue = parentButton;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("soundButton").objectReferenceValue = soundButton;
            so.FindProperty("nextButton").objectReferenceValue = nextButton;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.FindProperty("soundIcon").objectReferenceValue = soundIcon;
            so.FindProperty("soundOnSprite").objectReferenceValue = sprites.soundOn;
            so.FindProperty("soundOffSprite").objectReferenceValue = sprites.soundOff;
            so.ApplyModifiedPropertiesWithoutUndo();

            var mapData = new SerializedObject(worldMap);
            mapData.FindProperty("catalog").objectReferenceValue = HirayaSalitaBookSetup.EnsureCatalog();
            mapData.ApplyModifiedPropertiesWithoutUndo();
            HirayaAudioSetup.AddClickSounds(rootGo);   // every button and switch plays the click
            HirayaLanguage.Apply(rootGo);               // English, with the Filipino as the other language
            HirayaSkin.ApplyToTitle(rootGo);            // your button art and title background
            HirayaLanguage.AddLargeText(rootGo);        // the Large text switch
            HirayaLayout.FitAllScreens(rootGo, null);   // nothing cut off on long phones or on tablets
            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, TitlePrefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo);
            return prefab;
        }

        internal static Button AddPillButton(Transform parent, string name, Vector2 size, Color color, Sprite rounded, Vector2 anchor, Vector2 position)
        {
            Image image = AddRounded(parent, name, size, color, rounded, anchor, position);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            return button;
        }

        internal static Button AddRoundButton(Transform parent, string name, Vector2 anchor, Vector2 position, Sprite rounded, Sprite icon, out Image iconImage)
        {
            RectTransform item = NewRect(name, parent);
            Place(item, anchor, anchor, position, new Vector2(96f, 96f));
            AddRounded(item, "Ring", new Vector2(102f, 102f), LineGray, rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            Image fill = AddRounded(item, "Fill", new Vector2(96f, 96f), Color.white, rounded, new Vector2(0.5f, 0.5f), Vector2.zero);
            iconImage = AddIcon(item, "Icon", new Vector2(58f, 58f), icon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
            iconImage.raycastTarget = false;
            var button = item.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            return button;
        }

        // ------------------------------------------------------------------ UI helpers

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static RectTransform NewStretched(string name, Transform parent)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>A pill or circle: one 9-sliced sprite whose corners are scaled to half of the smaller side.</summary>
        internal static Image AddRounded(Transform parent, string name, Vector2 size, Color color, Sprite rounded, Vector2 anchor, Vector2 position)
        {
            RectTransform rect = NewRect(name, parent);
            Place(rect, anchor, new Vector2(0.5f, 0.5f), position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.pixelsPerUnitMultiplier = 128f / Mathf.Min(size.x, size.y);
            return image;
        }

        internal static Image AddIcon(Transform parent, string name, Vector2 size, Sprite sprite, Color color, Vector2 anchor, Vector2 position)
        {
            RectTransform rect = NewRect(name, parent);
            Place(rect, anchor, new Vector2(0.5f, 0.5f), position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        internal static TextMeshProUGUI AddText(Transform parent, string name, string text, float fontSize, Color color, FontStyles style,
                                       Vector2 size, Vector2 anchor, Vector2 position, float characterSpacing,
                                                TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = NewRect(name, parent);
            Place(rect, anchor, new Vector2(0.5f, 0.5f), position, size);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.characterSpacing = characterSpacing;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ------------------------------------------------------------------ drawing the small pictures

        internal delegate bool Shape(float x, float y);

        internal class Painter
        {
            readonly int width;
            readonly int height;
            readonly Color32[] pixels;

            public Painter(int width, int height)
            {
                this.width = width;
                this.height = height;
                pixels = new Color32[width * height];
            }

            /// <summary>Fills a shape with anti-aliasing (4 x 4 samples per pixel). y = 0 is the bottom.</summary>
            public void Fill(Shape inside, Color color)
            {
                const int samples = 4;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < samples; sy++)
                            for (int sx = 0; sx < samples; sx++)
                                if (inside(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples)) hits++;
                        if (hits == 0) continue;

                        float a = hits / (float)(samples * samples) * color.a;
                        Color32 dst = pixels[y * width + x];
                        float da = dst.a / 255f;
                        float oa = a + da * (1f - a);
                        float r = (color.r * a + dst.r / 255f * da * (1f - a)) / oa;
                        float g = (color.g * a + dst.g / 255f * da * (1f - a)) / oa;
                        float b = (color.b * a + dst.b / 255f * da * (1f - a)) / oa;
                        pixels[y * width + x] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(g * 255f),
                                                            (byte)Mathf.RoundToInt(b * 255f), (byte)Mathf.RoundToInt(oa * 255f));
                    }
                }
            }

            /// <summary>Makes a shape see-through again (a door cut out of a house).</summary>
            public void Clear(Shape inside)
            {
                const int samples = 4;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < samples; sy++)
                            for (int sx = 0; sx < samples; sx++)
                                if (inside(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples)) hits++;
                        if (hits == 0) continue;
                        Color32 c = pixels[y * width + x];
                        c.a = (byte)Mathf.RoundToInt(c.a * (1f - hits / (float)(samples * samples)));
                        pixels[y * width + x] = c;
                    }
                }
            }

            public byte[] ToPng()
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels32(pixels);
                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                return bytes;
            }
        }

        internal static Shape Circle(float cx, float cy, float r) => (x, y) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        internal static Shape Ring(float cx, float cy, float r0, float r1) => (x, y) =>
        {
            float d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
            return d >= r0 * r0 && d <= r1 * r1;
        };

        internal static Shape Rect(float x0, float y0, float x1, float y1) => (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

        internal static Shape RoundRect(float x0, float y0, float x1, float y1, float r) => (x, y) =>
        {
            if (x < x0 || x > x1 || y < y0 || y > y1) return false;
            float cx = Mathf.Clamp(x, x0 + r, x1 - r);
            float cy = Mathf.Clamp(y, y0 + r, y1 - r);
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        };

        internal static Shape Polygon(params Vector2[] points) => (x, y) =>
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                bool crosses = (points[i].y > y) != (points[j].y > y) &&
                               x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x;
                if (crosses) inside = !inside;
            }
            return inside;
        };

        internal static Shape Segment(Vector2 a, Vector2 b, float width) => (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude <= width * 0.5f;
        };

        /// <summary>Part of a ring: only the points within halfSpan degrees of the given direction.</summary>
        internal static Shape Arc(float cx, float cy, float r0, float r1, float directionDegrees, float halfSpanDegrees) => (x, y) =>
        {
            float dx = x - cx, dy = y - cy;
            float d = dx * dx + dy * dy;
            if (d < r0 * r0 || d > r1 * r1) return false;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(angle, directionDegrees)) <= halfSpanDegrees;
        };

        internal static Shape Union(params Shape[] shapes) => (x, y) =>
        {
            foreach (Shape s in shapes) if (s(x, y)) return true;
            return false;
        };

        internal static Shape Intersect(Shape a, Shape b) => (x, y) => a(x, y) && b(x, y);

        internal static Sprite SaveSprite(string path, Painter painter, Vector4 border)
        {
            File.WriteAllBytes(path, painter.ToPng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprites MakeSprites()
        {
            var s = new Sprites();

            // Rounded square with 64 px corners, cut in nine parts: becomes any pill or circle.
            var rounded = new Painter(136, 136);
            rounded.Fill(RoundRect(0f, 0f, 136f, 136f, 64f), Color.white);
            s.rounded = SaveSprite(IconFolder + "/RoundedShape.png", rounded, new Vector4(64f, 64f, 64f, 64f));

            var play = new Painter(128, 128);
            play.Fill(Polygon(new Vector2(40f, 22f), new Vector2(40f, 106f), new Vector2(108f, 64f)), Color.white);
            s.play = SaveSprite(IconFolder + "/PlayIcon.png", play, Vector4.zero);

            var padlock = new Painter(128, 128);
            Shape shackle = Union(Intersect(Ring(64f, 68f, 17f, 28f), (x, y) => y >= 68f),
                                  Rect(36f, 58f, 47f, 68f), Rect(81f, 58f, 92f, 68f));
            padlock.Fill(shackle, IconInk);
            padlock.Fill(RoundRect(26f, 18f, 102f, 70f, 9f), IconInk);
            s.lockIcon = SaveSprite(IconFolder + "/LockIcon.png", padlock, Vector4.zero);

            Shape speaker = Union(Rect(20f, 50f, 44f, 78f), Polygon(new Vector2(44f, 50f), new Vector2(76f, 24f), new Vector2(76f, 104f), new Vector2(44f, 78f)));

            var soundOn = new Painter(128, 128);
            soundOn.Fill(speaker, IconInk);
            soundOn.Fill(Arc(76f, 64f, 20f, 28f, 0f, 48f), IconInk);
            soundOn.Fill(Arc(76f, 64f, 36f, 44f, 0f, 48f), IconInk);
            s.soundOn = SaveSprite(IconFolder + "/SoundOnIcon.png", soundOn, Vector4.zero);

            var soundOff = new Painter(128, 128);
            soundOff.Fill(speaker, IconInk);
            soundOff.Fill(Segment(new Vector2(30f, 106f), new Vector2(108f, 22f), 10f), SlashRed);
            s.soundOff = SaveSprite(IconFolder + "/SoundOffIcon.png", soundOff, Vector4.zero);

            var settings = new Painter(128, 128);
            settings.Fill(Union(Ring(64f, 64f, 26f, 35f), Circle(64f, 64f, 9f),
                                Rect(60f, 92f, 68f, 114f), Rect(60f, 14f, 68f, 36f), Rect(14f, 60f, 36f, 68f), Rect(92f, 60f, 114f, 68f)), IconInk);
            s.settings = SaveSprite(IconFolder + "/SettingsIcon.png", settings, Vector4.zero);

            var back = new Painter(128, 128);
            back.Fill(Union(Segment(new Vector2(82f, 24f), new Vector2(42f, 64f), 13f), Segment(new Vector2(42f, 64f), new Vector2(82f, 104f), 13f)), IconInk);
            s.back = SaveSprite(IconFolder + "/BackIcon.png", back, Vector4.zero);

            return s;
        }
    }
}
